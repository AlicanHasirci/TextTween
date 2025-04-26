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
    using UnityEditor;
    using UnityEngine;
    using Utilities;

    [Serializable, ExecuteInEditMode]
    public class TextTweenManager : MonoBehaviour, IDisposable
    {
        [SerializeField]
        [HideInInspector]
        internal int BufferSize;

        [Range(0, 1f)]
        public float Progress;

        [SerializeField]
        internal List<TMP_Text> Texts = new();

        [SerializeField]
        internal List<CharModifier> Modifiers = new();

        [SerializeField]
        [HideInInspector]
        private List<MeshData> _meshData = new();

        private readonly Action<UnityEngine.Object> _onTextChange;

        private MeshArray _original;
        private MeshArray _modified;

        private float _progress;
        private bool _textChangeAttached;

        internal bool _needsHydration;

        public TextTweenManager()
        {
            _onTextChange = Change;
        }

        private void OnEnable()
        {
            foreach (TMP_Text text in Texts)
            {
                if (text != null)
                {
                    text.ForceMeshUpdate(true);
                }
            }

            if (!_textChangeAttached)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Add(_onTextChange);
                _textChangeAttached = true;
            }
            Hydrate();
        }

        private void OnDisable()
        {
            if (_textChangeAttached)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
                _textChangeAttached = false;
            }
            Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Update()
        {
            bool shouldUpdate = false;

            if (_needsHydration)
            {
                shouldUpdate = true;
                _needsHydration = false;
                Hydrate();
                if (_needsHydration)
                {
                    return;
                }
            }

            if (!shouldUpdate || Mathf.Approximately(_progress, Progress))
            {
                return;
            }

            Apply();
        }

        public void Add(TMP_Text tmp)
        {
            if (tmp == null || _meshData.Contains(tmp))
            {
                return;
            }

            Allocate();
            MeshData last = MeshData.Empty;
            foreach (MeshData data in _meshData)
            {
                if (data.Trail > last.Trail)
                {
                    last = data;
                }
            }
            MeshData newData = new(tmp);
            bool success = newData.Update(_original, last.Trail);
            _meshData.Add(newData);
            if (success)
            {
                Apply();
            }
            else
            {
                _needsHydration = true;
            }
        }

        public void Remove(TMP_Text text)
        {
            if (!_meshData.TryGetValue(text, out MeshData meshData))
            {
                return;
            }

            if (!_needsHydration)
            {
                meshData.Apply(_original);
            }

            _meshData.Remove(meshData);

            int length = _original.Length - meshData.Trail;
            if (length <= 0)
            {
                return;
            }

            Move(meshData.Trail, meshData.Offset, length).Complete();
            Allocate();
        }

        internal void Change(UnityEngine.Object obj)
        {
            if (Texts is not { Count: > 0 })
            {
                return;
            }

            TMP_Text tmp = obj as TMP_Text;
            int index = _meshData.GetIndex(tmp);
            if (index < 0)
            {
                return;
            }

            int originalLength = _meshData[index].Length;
            Allocate();

            int delta = tmp.GetVertexCount() - originalLength;
            if (delta != 0 && index < _meshData.Count - 1)
            {
                int from = _meshData[index + 1].Offset;
                int to = from + delta;
                Move(from, to, _meshData[^1].Trail - from).Complete();
            }
            bool success = _meshData[index].Update(_original, _meshData[index].Offset);
            if (success)
            {
                Apply();
            }
            else
            {
                _needsHydration = true;
            }
        }

        internal void Hydrate()
        {
            _original ??= new MeshArray(BufferSize, Allocator.Persistent);
            _modified ??= new MeshArray(BufferSize, Allocator.Persistent);
            Allocate();
            int offset = 0;
            foreach (MeshData textData in _meshData)
            {
                bool success = textData.Update(_original, offset);
                if (!success)
                {
                    _needsHydration = true;
                    return;
                }

                offset += textData.Length;
            }
            _needsHydration = false;
            Apply();
        }

        public void Apply()
        {
            if (_needsHydration)
            {
                return;
            }

            _modified.CopyFrom(_original);
            _modified.Schedule(Progress, Modifiers).Complete();
            foreach (MeshData textData in _meshData)
            {
                textData.Apply(_modified);
            }
            _progress = Progress;
        }

        public void Allocate()
        {
            int vertexCount = 0;
            foreach (TMP_Text text in Texts)
            {
                vertexCount += text.GetVertexCount();
            }

            _original.EnsureAndApplyLength(vertexCount);
            _modified.EnsureAndApplyLength(vertexCount);
            BufferSize = vertexCount;

            if (Application.isEditor && !Application.isPlaying)
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        private JobHandle Move(int from, int to, int length, JobHandle dependsOn = default)
        {
            int delta = to - from;
            foreach (MeshData data in _meshData)
            {
                if (data.Offset < from)
                {
                    continue;
                }

                data.Offset += delta;
            }
            return _original.Move(from, to, length, dependsOn);
        }

        public void Dispose()
        {
            if (_original != null && !_needsHydration)
            {
                foreach (MeshData textData in _meshData)
                {
                    textData.Apply(_original);
                }
            }

            _original?.Dispose();
            _original = null;
            _modified?.Dispose();
            _modified = null;
        }
    }
}
