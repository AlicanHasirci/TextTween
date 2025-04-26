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
            if (Text == null || Text.mesh == null)
            {
                return;
            }
            array.CopyTo(Text, Offset, Length);
        }

        public bool Update(MeshArray meshArray, int offset)
        {
            int length = Text.GetVertexCount();
            bool success = meshArray.CopyFrom(Text, length, offset);
            if (!success)
            {
                return false;
            }
            Offset = offset;
            Length = length;
            return true;
        }
    }
}
