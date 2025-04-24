namespace TextTween.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(CharModifier), true)]
    public class ModifierEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                ((CharModifier)target).Dispose();
            }
        }
    }
}
