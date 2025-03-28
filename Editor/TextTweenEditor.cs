using UnityEditor;

namespace TextTween.Editor {
    [CustomEditor(typeof(TweenManager))]
    public class TextTweenEditor : UnityEditor.Editor {
        private TweenManager _tweenManager;
        
        private SerializedProperty _progressProperty;
        private SerializedProperty _offsetProperty;
        private SerializedProperty _textsProperty;
        private SerializedProperty _modifiersProperty;
        
        private void OnEnable() {
            _tweenManager = (TweenManager) target;
            
            _progressProperty = serializedObject.FindProperty("Progress");
            _offsetProperty = serializedObject.FindProperty("Offset");
            _textsProperty = serializedObject.FindProperty("_texts");
            _modifiersProperty = serializedObject.FindProperty("_modifiers");
        }

        public override void OnInspectorGUI() {
            var createArrays = false;
            var applyChanges = SliderChanged(_progressProperty, "Progress");

            if (SliderChanged(_offsetProperty, "Offset")) {
                createArrays = true;
                applyChanges = true;
            }

            if (PropertyChanged(_textsProperty)) {
                createArrays = true;
                applyChanges = true;
            }

            if (PropertyChanged(_modifiersProperty)) {
                applyChanges = true;
            }
            
            if (createArrays) _tweenManager.Dispose();
            if (applyChanges) serializedObject.ApplyModifiedProperties();
            if (createArrays) _tweenManager.CreateNativeArrays();
            if (applyChanges) _tweenManager.ForceUpdate();
        }

        private bool SliderChanged(SerializedProperty property, string text) {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(property, 0, 1, text);
            return EditorGUI.EndChangeCheck();
        }

        private bool PropertyChanged(SerializedProperty property) {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property);
            return EditorGUI.EndChangeCheck();
        }
    }
}