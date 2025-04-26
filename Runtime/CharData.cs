namespace TextTween
{
    using Extensions;
    using Unity.Mathematics;

    public struct CharData
    {
        public int2 CharIndex { get; }
        public float2 Interval { get; set; }
        public TextTweenMinMaxAABB CharBounds { get; }
        public TextTweenMinMaxAABB TextBounds { get; }

        public CharData(
            int2 charIndex,
            float2 interval,
            TextTweenMinMaxAABB charBounds,
            TextTweenMinMaxAABB textBounds
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
