namespace TextTween.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [CustomEditor(typeof(TextTweenManager))]
    public class TextDataManagerInspector : Editor
    {
        private TextTweenManager _manager;
        private SerializedProperty _textsProperty;
        private SerializedProperty _modifiersProperty;
        private ReorderableList _reorderableList;

        private readonly List<TMP_Text> _previousTexts = new();

        private void OnEnable()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
            EditorApplication.update += TryHydrate;
            _manager = (TextTweenManager)target;
            _textsProperty = serializedObject.FindProperty(nameof(TextTweenManager.Texts));
            _previousTexts.Clear();
            CheckForAllChanges();
        }

        private void OnDisable()
        {
            EditorApplication.update -= TryHydrate;
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                CheckForAllChanges();
                if (_manager.isActiveAndEnabled)
                {
                    _manager.Apply();
                }
            }
        }

        private void TryHydrate()
        {
            TextTweenManager manager = target as TextTweenManager;
            if (manager != null && manager.isActiveAndEnabled && manager._needsHydration)
            {
                manager.Hydrate();
            }
        }

        private void CheckForAllChanges()
        {
            CheckForTextChanges();
        }

        private void CheckForTextChanges()
        {
            CheckForGenericChanges(_previousTexts, _textsProperty, _manager.Remove, _manager.Add);
        }

        private static void CheckForGenericChanges<T>(
            List<T> previous,
            SerializedProperty property,
            Action<T> removal,
            Action<T> addition
        )
            where T : Object
        {
            List<T> current = new(property.arraySize);
            for (int i = 0; i < property.arraySize; i++)
            {
                current.Add((T)property.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            HashSet<T> add = current.ToHashSet();
            HashSet<T> remove = previous.ToHashSet();

            add.ExceptWith(previous);
            remove.ExceptWith(current);

            foreach (T o in remove)
            {
                removal(o);
            }
            foreach (T o in add.Where(o => o != null))
            {
                addition(o);
            }

            previous.Clear();
            previous.AddRange(current);
        }

        private void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.serializedObject.targetObject.GetType() != typeof(TextTweenManager))
            {
                return;
            }

            if (property.name == nameof(TextTweenManager.Texts))
            {
                menu.AddItem(
                    new GUIContent("Find All Texts"),
                    false,
                    () => FindMissingComponents<TMP_Text>(property)
                );
            }

            if (property.name == nameof(TextTweenManager.Modifiers))
            {
                menu.AddItem(
                    new GUIContent("Find All Modifiers"),
                    false,
                    () => FindMissingComponents<CharModifier>(property)
                );
            }
        }

        private void FindMissingComponents<T>(SerializedProperty serializedProperty)
            where T : Object
        {
            TextTweenManager tweenManager = target as TextTweenManager;
            if (tweenManager == null)
            {
                SerializedObject propertyObject = serializedProperty?.serializedObject;
                if (propertyObject == null)
                {
                    return;
                }

                Object targetObject = propertyObject.targetObject;
                tweenManager = targetObject switch
                {
                    GameObject go => go.GetComponentInChildren<TextTweenManager>(true),
                    Component component => component.GetComponentInChildren<TextTweenManager>(true),
                    _ => tweenManager,
                };
                // Whoops! We're somewhere really unexpected! Be safe and exit.
                if (tweenManager == null)
                {
                    return;
                }
            }

            HashSet<T> uniqueElements = new();

            T[] current = new T[serializedProperty.arraySize];

            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                current[i] = serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue as T;
            }

            for (int i = current.Length - 1; i >= 0; i--)
            {
                T currentElement = current[i];
                if (currentElement != null && uniqueElements.Add(currentElement))
                {
                    continue;
                }
                serializedProperty.DeleteArrayElementAtIndex(i);
            }

            T[] found = tweenManager.GetComponentsInChildren<T>().Except(current).ToArray();
            if (found.Length > 0)
            {
                foreach (T component in found)
                {
                    int index = serializedProperty.arraySize;
                    serializedProperty.InsertArrayElementAtIndex(index);
                    serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue =
                        component;
                }
            }

            (
                serializedObject == null ? serializedProperty.serializedObject : serializedObject
            ).ApplyModifiedProperties();
            CheckForAllChanges();
            tweenManager.Apply();
        }
    }
}
