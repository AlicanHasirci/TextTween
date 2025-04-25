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
    using Utilities;

    [Serializable, ExecuteInEditMode]
    public class TextTweenManager : MonoBehaviour, IDisposable
    {
        [SerializeField]
        internal int BufferSize;

        [Range(0, 1f)]
        public float Progress;

        [SerializeField]
        internal List<TMP_Text> Texts;

        [SerializeField]
        internal List<CharModifier> Modifiers;

        [SerializeField]
        private List<MeshData> _meshData = new();
        private readonly Action<UnityEngine.Object> _onTextChange;

        private MeshArray _original;
        private MeshArray _modified;

        private float _progress;

        public TextTweenManager()
        {
            _onTextChange = Change;
        }

        private void OnEnable()
        {
            _original = new MeshArray(BufferSize, Allocator.Persistent);
            _modified = new MeshArray(BufferSize, Allocator.Persistent);

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(_onTextChange);
        }

        private void OnDisable()
        {
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
            newData.Update(_original, last.Trail);
            _meshData.Add(newData);

            Apply();
        }

        public void Remove(TMP_Text text)
        {
            if (!_meshData.TryGetValue(text, out MeshData meshData))
            {
                return;
            }

            meshData.Apply(_original);
            _meshData.Remove(meshData);

            int length = _original.Length - meshData.Trail;
            if (length <= 0)
            {
                return;
            }

            Move(meshData.Trail, meshData.Offset, length).Complete();
        }

        internal void Change(UnityEngine.Object obj)
        {
            if (Texts == null)
                return;
            TMP_Text tmp = (TMP_Text)obj;

            int index = _meshData.GetIndex(tmp);
            if (index < 0)
            {
                return;
            }

            Allocate();

            int delta = tmp.GetVertexCount() - _meshData[index].Length;
            if (delta != 0 && index < _meshData.Count - 1)
            {
                int from = _meshData[index + 1].Offset;
                int to = from + delta;
                Move(from, to, _meshData[^1].Trail - from).Complete();
            }
            _meshData[index].Update(_original, _meshData[index].Offset);

            Apply();
        }

        public void Apply()
        {
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

            _original.EnsureCapacity(vertexCount);
            _modified.EnsureCapacity(vertexCount);
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
            _original.Dispose();
            _modified.Dispose();
        }
    }
}
