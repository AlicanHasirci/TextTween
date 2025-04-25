namespace TextTween.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomEditor(typeof(TextTweenManager))]
    public class TextDataManagerInspector : Editor
    {
        private TextTweenManager _manager;
        private SerializedProperty _textsProperty;
        private ReorderableList _reorderableList;

        private List<TMP_Text> _previous = new();

        private void OnEnable()
        {
            _manager = (TextTweenManager)target;
            _textsProperty = serializedObject.FindProperty(nameof(TextTweenManager.Texts));
            for (int i = 0; i < _textsProperty.arraySize; i++)
            {
                _previous.Add(
                    (TMP_Text)_textsProperty.GetArrayElementAtIndex(i).objectReferenceValue
                );
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                List<TMP_Text> current = new(_textsProperty.arraySize);
                for (int i = 0; i < _textsProperty.arraySize; i++)
                {
                    current.Add(
                        (TMP_Text)_textsProperty.GetArrayElementAtIndex(i).objectReferenceValue
                    );
                }

                HashSet<TMP_Text> add = current.ToHashSet();
                HashSet<TMP_Text> remove = _previous.ToHashSet();

                add.ExceptWith(_previous);
                remove.ExceptWith(current);

                foreach (TMP_Text o in remove)
                {
                    if (o == null)
                    {
                        continue;
                    }
                    _manager.Remove(o);
                }
                foreach (TMP_Text o in add)
                {
                    if (o == null)
                    {
                        continue;
                    }
                    _manager.Add(o);
                }
                _previous = current;
                ((TextTweenManager)target).Apply();
            }
        }
    }
}
