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
    public class TextTweenManager : MonoBehaviour, IDisposable
    {
        [Range(0, 1f)]
        public float Progress;

        [SerializeField]
        internal List<TMP_Text> Texts = new();

        [SerializeField]
        internal List<CharModifier> Modifiers = new();

        [SerializeField]
        internal List<MeshData> MeshData = new();

        [Header("Advanced")]
        [Tooltip(
            "The sum total number of vertices used for buffers across all Text instances being managed.\n\n"
                + "Runtime buffer size will be the max of this value and ComputedBufferSize.\n\n"
                + "<color=yellow>Should only be set if you know your text is going to grow to some size in the future"
                + "</color>"
        )]
        public int ExplicitBufferSize = -1;

        [Tooltip(
            "Auto-configured by TextTween internals, changes to this value will be overwritten."
        )]
        [FormerlySerializedAs("BufferSize")]
        [SerializeField]
        internal int ComputedBufferSize;

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
            int bufferSize = Math.Max(ExplicitBufferSize, ComputedBufferSize);
            bufferSize = Math.Max(0, bufferSize);
            Original = new MeshArray(bufferSize, Allocator.Persistent);
            Modified = new MeshArray(bufferSize, Allocator.Persistent);

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(_onTextChange);
        }

        internal void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
            Dispose();
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

        public void Remove(TMP_Text text)
        {
            if (!MeshData.TryGetValue(text, out MeshData meshData))
            {
                return;
            }

            meshData.Apply(Original);
            MeshData.Remove(meshData);

            int length = Original.Length - meshData.Trail;
            if (length <= 0)
            {
                return;
            }

            Move(meshData.Trail, meshData.Offset, length).Complete();

            TryUpdateComputedBufferSize();
        }

        internal void Change(UnityEngine.Object obj)
        {
            if (Texts == null)
                return;
            TMP_Text tmp = (TMP_Text)obj;

            int index = MeshData.GetIndex(tmp);
            if (index < 0)
            {
                return;
            }

            Allocate();

            int delta = tmp.GetVertexCount() - MeshData[index].Length;
            if (delta != 0 && index < MeshData.Count - 1)
            {
                int from = MeshData[index + 1].Offset;
                int to = from + delta;
                Move(from, to, MeshData[^1].Trail - from).Complete();
            }
            MeshData[index].Update(Original, MeshData[index].Offset);

            Apply();
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
            Original.EnsureCapacity(capacity);
            Modified.EnsureCapacity(capacity);
            TryUpdateComputedBufferSize(capacity);
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

            return vertexCount;
        }

        internal void TryUpdateComputedBufferSize(int? capacity = null)
        {
#if UNITY_EDITOR
            // Update LKG of buffer size if we're in a place where serialization is ok (game not playing)
            if (!Application.isPlaying)
            {
                int oldBufferSize = ComputedBufferSize;
                int newBufferSize = capacity ?? CalculateCapacity();
                if (oldBufferSize != newBufferSize)
                {
                    ComputedBufferSize = newBufferSize;
                    EditorUtility.SetDirty(this);
                }
            }
#endif
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

        public void Dispose()
        {
            Original.Dispose();
            Modified.Dispose();
        }
    }
}
