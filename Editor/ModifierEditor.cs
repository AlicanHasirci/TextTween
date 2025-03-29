using UnityEditor;

namespace TextTween.Editor
{
    [CustomEditor(typeof(CharModifier), true)]
    public class ModifierEditor : UnityEditor.Editor
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
