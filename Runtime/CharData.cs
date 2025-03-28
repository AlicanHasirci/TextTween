using Unity.Mathematics;

namespace TextTween {
    public struct CharData {
        public float2 Interval { get; }
        public int VertexIndex { get; }
        public int VertexCount { get; }

        public CharData(float2 interval, int vertexIndex, int vertexCount) {
            Interval = interval;
            VertexIndex = vertexIndex;
            VertexCount = vertexCount;
        }

    }
}