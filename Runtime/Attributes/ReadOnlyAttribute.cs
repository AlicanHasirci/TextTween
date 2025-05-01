namespace TextTween.Attributes
{
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    /// <summary>
    ///     TextTween internal, simple, read-only attribute. Does not support complex types.
    /// </summary>
    internal sealed class ReadOnlyAttribute : PropertyAttribute { }
}
