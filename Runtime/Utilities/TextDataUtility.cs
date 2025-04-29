namespace TextTween.Utilities
{
    using System.Collections.Generic;
    using TMPro;

    public static class TextDataUtility
    {
        public static int GetVertexCount(this TMP_Text text)
        {
            if (text == null || text.mesh == null)
            {
                return 0;
            }
            return text.mesh.vertexCount;
        }

        public static bool Contains(this IReadOnlyList<MeshData> collection, TMP_Text text)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                MeshData data = collection[i];
                if (data.Text == text)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetValue(
            this IReadOnlyList<MeshData> collection,
            TMP_Text text,
            out MeshData meshData
        )
        {
            for (int i = 0; i < collection.Count; i++)
            {
                MeshData data = collection[i];
                if (data.Text == text)
                {
                    meshData = data;
                    return true;
                }
            }

            meshData = default;
            return false;
        }

        public static int GetIndex(this IReadOnlyList<MeshData> collection, TMP_Text text)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i].Text == text)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
