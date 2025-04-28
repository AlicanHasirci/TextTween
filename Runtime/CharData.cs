namespace TextTween
{
    using Extensions;
    using Unity.Mathematics;

    public struct CharData
    {
        public int2 CharIndex { get; }
        public float2 Interval { get; set; }
        public MinMaxAABB CharBounds { get; }
        public MinMaxAABB TextBounds { get; }

        public CharData(
            int2 charIndex,
            float2 interval,
            MinMaxAABB charBounds,
            MinMaxAABB textBounds
        )
        {
            CharIndex = charIndex;
            Interval = interval;
            CharBounds = charBounds;
            TextBounds = textBounds;
        }

        public bool IsValid()
        {
            return !CharBounds.IsNaN() && !TextBounds.IsNaN() && CharIndex.x < CharIndex.y;
        }
    }
}
