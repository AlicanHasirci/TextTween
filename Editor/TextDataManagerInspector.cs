using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditorInternal;
using UnityEngine;

namespace TextTween.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(TextTweenManager))]
    public class TextDataManagerInspector : Editor
    {
        private SerializedProperty _textsProperty;
        private ReorderableList _reorderableList;

        private readonly Lazy<MethodInfo> _add = new(
            () =>
                typeof(TextTweenManager).GetMethod(
                    "Add",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
        );

        private readonly Lazy<MethodInfo> _remove = new(
            () =>
                typeof(TextTweenManager).GetMethod(
                    "Remove",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
        );

        private List<TMP_Text> _previous = new();

        private void OnEnable()
        {
            _textsProperty ??= serializedObject.FindProperty("Texts");
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
                    _remove.Value.Invoke(target, new object[] { o });
                }
                foreach (TMP_Text o in add)
                {
                    if (o == null)
                    {
                        continue;
                    }
                    _add.Value.Invoke(target, new object[] { o });
                }
                _previous = current;
                ((TextTweenManager)target).Apply();
            }
        }
    }
}
