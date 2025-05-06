namespace TextTween.Extensions
{
    using System.Linq;
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

            TMP_MeshInfo meshInfo = tmp.textInfo.meshInfo[0];

            int vertexCount = tmp.GetVertexCount();
            bool wasModified = false;

            Color[] colors = mesh.colors;
            PopulateArrayIfNeeded(ref colors, tmp.color);
            mesh.colors = colors;

            Color32[] colors32 = mesh.colors32;
            PopulateArrayIfNeeded(ref colors32, tmp.color);
            mesh.colors32 = colors32;

            Vector2[] uv = mesh.uv;
            PopulateArrayIfNeeded(ref uv, Vector2.zero);
            mesh.uv = uv;

            Vector2[] uv2 = mesh.uv2;
            PopulateArrayIfNeeded(ref uv2, Vector2.zero);
            mesh.uv2 = uv2;

            if (!wasModified)
            {
                return;
            }

            meshInfo.colors32 = colors32.ToArray();
            meshInfo.vertices = mesh.vertices.ToArray();
            meshInfo.uvs0 = uv.ToArray();
            meshInfo.uvs2 = uv2.ToArray();
            tmp.textInfo.meshInfo[0] = meshInfo;
            tmp.UpdateGeometry(mesh, 0);
            if (tmp is TextMeshProUGUI textMeshProUGUI && textMeshProUGUI.canvasRenderer != null)
            {
                textMeshProUGUI.canvasRenderer.SetMesh(tmp.mesh);
            }

            return;

            void PopulateArrayIfNeeded<T>(ref T[] array, T value)
            {
                if (array?.Length == vertexCount)
                {
                    return;
                }

                array = new T[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    array[i] = value;
                }

                wasModified = true;
            }
        }
    }
}
