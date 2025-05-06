namespace TextTween
{
    using System;
    using TMPro;
    using Utilities;

    [Serializable]
    public class MeshData
    {
        public static readonly MeshData Empty = new(null);

        public TMP_Text Text;
        public int Offset;
        public int Length;
        public int Trail => Length + Offset;

        public MeshData(TMP_Text text)
        {
            Text = text;
        }

        public void Apply(MeshArray array)
        {
            if (Text == null || Text.mesh == null || string.IsNullOrEmpty(Text.text))
            {
                return;
            }

            array.CopyTo(Text, Offset, Length);
        }

        public void Update(MeshArray meshArray, int offset, bool copyFrom = true)
        {
            int length = Text.GetVertexCount();
            if (copyFrom)
            {
                meshArray.CopyFrom(Text, offset, length);
            }

            Offset = offset;
            Length = length;
        }
    }
}
