namespace TextTween.Editor.Utilities
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;

    // Simple auto-closing Horizontal layout group
    internal readonly struct HorizontalLayoutGroup : IDisposable
    {
        // Can't have parameter-less struct constructors, so introduce a useless parameter
        public HorizontalLayoutGroup(object uselessParameter)
        {
            EditorGUILayout.BeginHorizontal();
        }

        public void Dispose()
        {
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
