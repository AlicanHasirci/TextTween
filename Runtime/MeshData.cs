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
            if (Text == null || Text.mesh == null || Text.text.Length == 0)
            {
                return;
            }

            array.CopyTo(Text, Offset, Length);
        }

        public void Update(MeshArray meshArray, int offset)
        {
            int length = Text.GetVertexCount();
            meshArray.CopyFrom(Text, length, offset);
            Offset = offset;
            Length = length;
        }
    }
}
