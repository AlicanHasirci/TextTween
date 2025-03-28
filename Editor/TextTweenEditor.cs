using UnityEditor;

namespace TextTween.Editor {
    [CustomEditor(typeof(TweenManager))]
    public class TextTweenEditor : UnityEditor.Editor {
        private TweenManager tweenManager;

        private void OnEnable() {
            tweenManager = (TweenManager) target;
        }

        public override void OnInspectorGUI() {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (!EditorGUI.EndChangeCheck()) return;
            tweenManager.CreateCharDataArray();
            tweenManager.ForceUpdate();
        }
    }
}