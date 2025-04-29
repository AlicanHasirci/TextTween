namespace TextTween
{
    using System;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Utilities;

    [Serializable]
    public class MeshData
    {
        public static readonly MeshData Empty = new(null);

        public TMP_Text Text;

        public int Offset
        {
            get => Text == null ? 0 : _offset;
            private set => _offset = value;
        }

        public int Length
        {
            get => Text == null ? 0 : _length;
            private set => _length = value;
        }

        public int Trail => Text == null ? 0 : _length + _offset;

        [SerializeField]
        [FormerlySerializedAs("Offset")]
        internal int _offset;

        [SerializeField]
        [FormerlySerializedAs("Length")]
        private int _length;

        public MeshData(TMP_Text text)
        {
            Text = text;
        }

        public void Apply(MeshArray array)
        {
            if (Text == null || Text.mesh == null || Text.text.Length == 0)
            {
                return;
            }

            array.CopyTo(Text, Offset, Length);
        }

        public void Update(MeshArray meshArray, int offset)
        {
            int length = Text.GetVertexCount();
            if (length != 0)
            {
                meshArray.CopyFrom(Text, length, offset);
            }

            Offset = offset;
            Length = length;
        }
    }
}
