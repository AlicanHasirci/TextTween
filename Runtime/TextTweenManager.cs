using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TextTween.Tests")]
[assembly: InternalsVisibleTo("TextTween.Editor")]

namespace TextTween
{
    using System;
    using System.Collections.Generic;
    using TMPro;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Utilities;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [Serializable, ExecuteInEditMode]
    [AddComponentMenu("TextTween/Text Tween Manager")]
    public class TextTweenManager : MonoBehaviour, IDisposable
    {
        [Header("Tween Config")]
        [Range(0, 1f)]
        public float Progress;

        [SerializeField]
        internal List<TMP_Text> Texts = new();

        [SerializeField]
        internal List<CharModifier> Modifiers = new();

        [Header("Advanced")]
        [Tooltip(
            "<color=yellow>Should only be set if you know your text is going to grow to some size in the future."
                + "</color>\n\n"
                + "Explicit Buffer Size is the sum total number of vertices used for buffers across all Text instances "
                + "being managed.\n\nRuntime buffer size will be the max of this value and ComputedBufferSize."
        )]
        public int ExplicitBufferSize = -1;

        [Tooltip(
            "Auto-configured by TextTween internals, changes to this value will be overwritten."
        )]
        [FormerlySerializedAs("BufferSize")]
        [Attributes.ReadOnly]
        [SerializeField]
        internal int ComputedBufferSize;

        [SerializeField]
        internal List<MeshData> MeshData = new();

        internal MeshArray Original;
        internal MeshArray Modified;

        private float _progress;
        private readonly Action<UnityEngine.Object> _onTextChange;

        public TextTweenManager()
        {
            _onTextChange = Change;
        }

        internal void OnEnable()
        {
            /*
                It's very likely that we'll be parent components of the TMP texts that we're managing. In some cases, that
                means we'll be enabled before they are, and their state will be dirty. I've run into several cases in
                the editor where, without having the text's mesh updated, our internals will try to interact with TMP
                and TMP internals will be non-initialized and throw null reference exceptions.
                
                To avoid this, force-update the meshes to ensure we're in a known-good state.
             */
            foreach (MeshData meshData in MeshData)
            {
                if (meshData.Text != null)
                {
                    meshData.Text.ForceMeshUpdate(ignoreActiveState: true);
                }
            }

            Allocate();

            CheckForMeshChanges();

            Apply();

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(_onTextChange);
        }

        internal void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
            Dispose();
        }

        internal void OnDestroy()
        {
            foreach (TMP_Text text in Texts)
            {
                if (text != null)
                {
                    text.ForceMeshUpdate(true);
                }
                RemoveAgent(text);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || Mathf.Approximately(_progress, Progress))
            {
                return;
            }

            Apply();
        }

        public void Add(TMP_Text tmp)
        {
            if (tmp == null || MeshData.Contains(tmp))
            {
                return;
            }
            
            AddAgent(tmp);

            Allocate();

            MeshData last = TextTween.MeshData.Empty;
            foreach (MeshData data in MeshData)
            {
                if (data.Trail > last.Trail)
                {
                    last = data;
                }
            }
            MeshData newData = new(tmp);
            newData.Update(Original, last.Trail);
            MeshData.Add(newData);

            Apply();
        }

        public void Remove(TMP_Text tmp, bool retainAgent = false)
        {
            if (!MeshData.TryGetValue(tmp, out MeshData meshData))
            {
                return;
            }

            if (!retainAgent)
            {
                RemoveAgent(tmp);
            }
            
            Texts.Remove(tmp);
            meshData.Apply(Original);
            MeshData.Remove(meshData);

            int length = Original.Length - meshData.Trail;
            if (length <= 0)
            {
                return;
            }

            Move(meshData.Trail, meshData.Offset, length).Complete();
        }

        internal void Change(UnityEngine.Object obj)
        {
            if (Texts == null)
            {
                return;
            }

            Allocate();

            CheckForMeshChanges();

            Apply();
        }

        internal void CheckForMeshChanges()
        {
            for (int i = 0; i < MeshData.Count; i++)
            {
                MeshData meshData = MeshData[i];
                if (meshData.Text == null)
                {
                    continue;
                }
                int delta = meshData.Text.GetVertexCount() - meshData.Length;
                if (delta != 0 && i < MeshData.Count - 1)
                {
                    int from = MeshData[i + 1].Offset;
                    int to = from + delta;
                    Move(from, to, MeshData[^1].Trail - from).Complete();
                }
                meshData.Update(Original, meshData.Offset);
            }
        }

        public void Apply()
        {
            Modified.CopyFrom(Original);
            Modified.Schedule(Progress, Modifiers).Complete();
            foreach (MeshData textData in MeshData)
            {
                textData.Apply(Modified);
            }
            _progress = Progress;
        }

        public void Allocate()
        {
            int capacity = CalculateCapacity();
            EnsureCapacity(ref Original, capacity);
            EnsureCapacity(ref Modified, capacity);
        }

        private static void EnsureCapacity(ref MeshArray meshArray, int capacity)
        {
            if (meshArray == null)
            {
                meshArray = new MeshArray(capacity, Allocator.Persistent);
            }
            else
            {
                meshArray.EnsureCapacity(capacity);
            }
        }

        private int CalculateCapacity()
        {
            int vertexCount = 0;
            foreach (TMP_Text text in Texts)
            {
                if (text != null)
                {
                    vertexCount += text.GetVertexCount();
                }
            }
            if (ComputedBufferSize != vertexCount)
            {
                ComputedBufferSize = vertexCount;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
            return Math.Max(ComputedBufferSize, ExplicitBufferSize);
        }

        private JobHandle Move(int from, int to, int length, JobHandle dependsOn = default)
        {
            int delta = to - from;
            foreach (MeshData data in MeshData)
            {
                if (data.Offset < from)
                {
                    continue;
                }

                data.Offset += delta;
            }

            return Original.Move(from, to, length, dependsOn);
        }
        
        private void AddAgent(TMP_Text tmp)
        {
            if (!tmp.TryGetComponent(out TextTweenAgent agent))
            {
                agent = tmp.gameObject.AddComponent<TextTweenAgent>();
            }
            agent.SetOwner(this);
        }

        private static void RemoveAgent(TMP_Text tmp)
        {
            if (tmp.TryGetComponent(out TextTweenAgent agent))
            {
                agent.Remove();
            }
        }

        public void Dispose()
        {
            Original.Dispose();
            Modified.Dispose();
        }
    }
}
