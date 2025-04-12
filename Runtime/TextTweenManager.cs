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
    using Unity.Mathematics;
    using UnityEngine;
    using Utilities;

    [Serializable, ExecuteInEditMode]
    public class TextTweenManager : MonoBehaviour, IDisposable
    {
        [Range(0, 1f)]
        public float Progress;

        [Range(0, 1f)]
        public float Overlap;

        [SerializeField]
        internal List<TMP_Text> Texts;

        [SerializeField]
        internal List<CharModifier> Modifiers;

        [SerializeField]
        private List<MeshData> _meshData = new();
        private readonly Action<UnityEngine.Object> _onTextChange;

        internal NativeArray<CharData> Chars;
        internal NativeArray<float4> Colors;
        internal NativeArray<float3> Vertices;

        private float _progress;

        public TextTweenManager()
        {
            _onTextChange = Change;
        }

        private void OnEnable()
        {
            NativeArrayUtility.EnsureCapacity(ref Chars, 0);
            NativeArrayUtility.EnsureCapacity(ref Colors, 0);
            NativeArrayUtility.EnsureCapacity(ref Vertices, 0);

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(_onTextChange);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(_onTextChange);
        }

        private void OnDisable()
        {
            Dispose();
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
            newData.Update(ref Vertices, ref Colors, ref Chars, last.Trail, Overlap);
            _meshData.Add(newData);

            Apply();
        }

        public void Remove(TMP_Text text)
        {
            if (!_meshData.TryGetValue(text, out MeshData meshData))
            {
                return;
            }

            meshData.Apply(Vertices, Colors);
            _meshData.Remove(meshData);

            int length = Vertices.Length - meshData.Trail;
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
            _meshData[index]
                .Update(ref Vertices, ref Colors, ref Chars, _meshData[index].Offset, Overlap);

            Apply();
        }

        public void Apply()
        {
            using Snapshot ss = new(this);
            ss.Schedule(Progress, Modifiers).Complete();
            ss.Apply(_meshData);
        }

        public void Allocate()
        {
            int vertexCount = 0;
            foreach (TMP_Text text in Texts)
            {
                vertexCount += text.GetVertexCount();
            }

            NativeArrayUtility.EnsureCapacity(ref Chars, vertexCount);
            NativeArrayUtility.EnsureCapacity(ref Colors, vertexCount);
            NativeArrayUtility.EnsureCapacity(ref Vertices, vertexCount);
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

            return JobHandle.CombineDependencies(
                NativeArrayUtility.Move(ref Vertices, from, to, length, dependsOn),
                NativeArrayUtility.Move(ref Colors, from, to, length, dependsOn),
                NativeArrayUtility.Move(ref Chars, from, to, length, dependsOn)
            );
        }

        public void Dispose()
        {
            if (Chars.IsCreated)
            {
                Chars.Dispose();
            }
            if (Vertices.IsCreated)
            {
                Vertices.Dispose();
            }
            if (Colors.IsCreated)
            {
                Colors.Dispose();
            }
        }
    }
}
