namespace TextTween.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using TMPro;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(TweenManager))]
    public class TweenManagerEditor : Editor
    {
        private float _progress;
        private float _offset;
        private readonly List<TMP_Text> _texts = new();
        private readonly List<CharModifier> _modifiers = new();

        private static readonly Lazy<FieldInfo> TextField = new(
            () =>
                typeof(TweenManager).GetField(
                    "_texts",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
        );

        private static readonly Lazy<FieldInfo> ModifiersField = new(
            () =>
                typeof(TweenManager).GetField(
                    "_modifiers",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
        );

        private void OnEnable()
        {
            CheckForChanges(target as TweenManager, out _, out _, out _);

            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        private void OnDestroy()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            TweenManager tweenManager = (TweenManager)target;
            CheckForChanges(
                tweenManager,
                out bool createArrays,
                out bool applyChanges,
                out IReadOnlyList<TMP_Text> oldTexts
            );

            if (createArrays)
            {
                tweenManager.Dispose(oldTexts);
                tweenManager.CreateNativeArrays();
            }

            if (applyChanges)
            {
                tweenManager.ForceUpdate();
            }
        }

        private void CheckForChanges(
            TweenManager manager,
            out bool createArrays,
            out bool applyChanges,
            out IReadOnlyList<TMP_Text> oldTexts
        )
        {
            createArrays = false;
            applyChanges = false;
            oldTexts = _texts;

            if (manager.Offset != _offset)
            {
                createArrays = true;
                applyChanges = true;
                _offset = manager.Offset;
            }

            if (manager.Progress != _progress)
            {
                createArrays = true;
                applyChanges = true;
                _progress = manager.Progress;
            }

            IReadOnlyList<TMP_Text> texts =
                TextField.Value.GetValue(manager) as IReadOnlyList<TMP_Text>
                ?? Array.Empty<TMP_Text>();
            if (!_texts.SequenceEqual(texts))
            {
                createArrays = true;
                applyChanges = true;
                oldTexts = _texts.ToArray();
                _texts.Clear();
                _texts.AddRange(texts);
            }

            IReadOnlyList<CharModifier> modifiers =
                ModifiersField.Value.GetValue(manager) as IReadOnlyList<CharModifier>
                ?? Array.Empty<CharModifier>();
            if (!_modifiers.SequenceEqual(modifiers))
            {
                applyChanges = true;
                _modifiers.Clear();
                _modifiers.AddRange(modifiers);
            }
        }

        private void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.serializedObject.targetObject.GetType() != typeof(TweenManager))
            {
                return;
            }

            if (property.name == "_texts")
            {
                menu.AddItem(
                    new GUIContent("Find All Texts"),
                    false,
                    () => FindMissingComponents<TMP_Text>(property)
                );
            }

            if (property.name == "_modifiers")
            {
                menu.AddItem(
                    new GUIContent("Find All Modifiers"),
                    false,
                    () => FindMissingComponents<CharModifier>(property)
                );
            }
        }

        private void FindMissingComponents<T>(SerializedProperty serializedProperty)
            where T : UnityEngine.Object
        {
            TweenManager tweenManager = (TweenManager)target;
            T[] current = new T[serializedProperty.arraySize];

            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                current[i] = serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue as T;
            }

            for (int i = current.Length - 1; i >= 0; i--)
            {
                if (current[i] != null)
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

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
