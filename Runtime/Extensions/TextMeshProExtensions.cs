namespace TextTween.Extensions
{
    using TMPro;
    using UnityEngine;
    using Utilities;

    public static class TextMeshProExtensions
    {
        public static void EnsureArrayIntegrity(this TMP_Text tmp, bool forceMeshUpdate = true)
        {
            if (tmp == null)
            {
                return;
            }

            if (forceMeshUpdate)
            {
                tmp.ForceMeshUpdate(true);
            }

            if (tmp.mesh == null)
            {
                return;
            }

            if (tmp.textInfo.meshInfo is not { Length: > 0 })
            {
                return;
            }

            Mesh mesh = tmp.mesh;
            if (mesh.vertices is not { Length: > 0 })
            {
                return;
            }

            bool wasModified = false;
            int vertexCount = tmp.GetVertexCount();
            mesh.colors = PopulateArrayIfNeeded(mesh.colors, tmp.color);
            mesh.colors32 = PopulateArrayIfNeeded(mesh.colors32, tmp.color);
            mesh.uv = PopulateArrayIfNeeded(mesh.uv, Vector2.zero);
            mesh.uv2 = PopulateArrayIfNeeded(mesh.uv2, Vector2.zero);
            if (!wasModified)
            {
                return;
            }

            if (tmp is TextMeshProUGUI textMeshProUGUI && textMeshProUGUI.canvasRenderer != null)
            {
                textMeshProUGUI.canvasRenderer.SetMesh(tmp.mesh);
            }

            return;

            T[] PopulateArrayIfNeeded<T>(T[] array, T value)
            {
                if (array?.Length == vertexCount)
                {
                    return array;
                }

                array = new T[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    array[i] = value;
                }

                wasModified = true;
                return array;
            }
        }
    }
}
