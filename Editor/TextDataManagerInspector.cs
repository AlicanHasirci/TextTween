namespace TextTween.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using Utilities;

    [CustomEditor(typeof(TextTweenManager))]
    public class TextDataManagerInspector : Editor
    {
        private TextTweenManager _manager;
        private SerializedProperty _textsProperty;
        private SerializedProperty _modifiersProperty;

        private readonly List<TMP_Text> _previousTexts = new();
        private readonly List<CharModifier> _previousModifiers = new();

        private readonly List<TMP_Text> _textsBuffer = new();
        private readonly List<CharModifier> _modifiersBuffer = new();

        private readonly HashSet<TMP_Text> _currentTextDuplicateBuffer = new();
        private readonly HashSet<TMP_Text> _changeTextDuplicateBuffer = new();

        private readonly HashSet<CharModifier> _currentModifiersDuplicateBuffer = new();
        private readonly HashSet<CharModifier> _changeModifiersDuplicateBuffer = new();

        private GUIStyle _impactButtonStyle;

        private void OnEnable()
        {
            _manager = (TextTweenManager)target;
            _textsProperty = serializedObject.FindProperty(nameof(TextTweenManager.Texts));
            _modifiersProperty = serializedObject.FindProperty(nameof(TextTweenManager.Modifiers));
            HydrateCurrentState();
        }

        public override void OnInspectorGUI()
        {
            _impactButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold,
            };

            TextTweenManager tweenManager = ((TextTweenManager)target);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, nameof(TextTweenManager.MeshData));
            RenderInvalidButtons(tweenManager);
            RenderSyncButtons(tweenManager);
            if (
                serializedObject.ApplyModifiedProperties()
                || HasChanged(_previousTexts, _textsProperty)
                || HasChanged(_previousModifiers, _modifiersProperty)
            )
            {
                List<TMP_Text> current = GetCurrentArrayValues<TMP_Text>(_textsProperty).ToList();

                HashSet<TMP_Text> add = current.ToHashSet();
                HashSet<TMP_Text> remove = _previousTexts.ToHashSet();

                add.ExceptWith(_previousTexts);
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
                tweenManager.Apply();
                HydrateCurrentState();
            }
        }

        private void RenderInvalidButtons(TextTweenManager tweenManager)
        {
            using (new HorizontalLayoutGroup(this))
            {
                CheckAndRemoveNulls(tweenManager.Texts, "Remove Null Texts");
                CheckAndRemoveNulls(tweenManager.Modifiers, "Remove Null Modifiers");
            }
            using (new HorizontalLayoutGroup(this))
            {
                CheckAndRemoveDuplicates(
                    tweenManager.Texts,
                    _currentTextDuplicateBuffer,
                    "Remove Duplicate Texts"
                );
                CheckAndRemoveDuplicates(
                    tweenManager.Modifiers,
                    _currentModifiersDuplicateBuffer,
                    "Remove Duplicate Modifiers"
                );
            }
        }

        private void RenderSyncButtons(TextTweenManager tweenManager)
        {
            using (new HorizontalLayoutGroup(this))
            {
                CheckAndSync(
                    tweenManager,
                    _textsBuffer,
                    tweenManager.Texts,
                    _currentTextDuplicateBuffer,
                    _changeTextDuplicateBuffer,
                    "Sync Texts"
                );
                CheckAndSync(
                    tweenManager,
                    _modifiersBuffer,
                    tweenManager.Modifiers,
                    _currentModifiersDuplicateBuffer,
                    _changeModifiersDuplicateBuffer,
                    "Sync Modifiers"
                );
            }
        }

        private void CheckAndRemoveDuplicates<T>(
            List<T> list,
            HashSet<T> duplicateBuffer,
            string buttonText
        )
            where T : Object
        {
            duplicateBuffer.Clear();
            foreach (T element in list)
            {
                duplicateBuffer.Add(element);
            }
            if (duplicateBuffer.Count == list.Count)
            {
                return;
            }

            if (GUILayout.Button(buttonText, _impactButtonStyle))
            {
                Dictionary<T, int> elementCounts = new();
                foreach (T element in list)
                {
                    int count = elementCounts.GetValueOrDefault(element, 0);
                    elementCounts[element] = count + 1;
                }

                // Remove items from the end
                for (int i = list.Count - 1; 0 <= i; --i)
                {
                    T element = list[i];
                    int count = elementCounts.GetValueOrDefault(element, 0);
                    if (1 < count)
                    {
                        list.RemoveAt(i);
                        --count;
                        elementCounts[element] = count;
                    }
                }
            }
        }

        private void CheckAndRemoveNulls<T>(List<T> list, string buttonText)
            where T : Object
        {
            if (!list.Exists(e => e == null))
            {
                return;
            }

            if (GUILayout.Button(buttonText, _impactButtonStyle))
            {
                list.RemoveAll(element => element == null);
            }
        }

        private static void CheckAndSync<T>(
            TextTweenManager tweenManager,
            List<T> buffer,
            List<T> list,
            HashSet<T> currentDuplicateBuffer,
            HashSet<T> changeDuplicateBuffer,
            string buttonText
        )
            where T : Object
        {
            currentDuplicateBuffer.Clear();
            foreach (T element in list)
            {
                currentDuplicateBuffer.Add(element);
            }

            buffer.Clear();
            tweenManager.GetComponentsInChildren(true, buffer);
            changeDuplicateBuffer.Clear();
            foreach (T element in buffer)
            {
                changeDuplicateBuffer.Add(element);
            }

            if (HasChanged(currentDuplicateBuffer, changeDuplicateBuffer))
            {
                if (GUILayout.Button(buttonText, EditorStyles.miniButton))
                {
                    list.Clear();
                    list.AddRange(buffer);
                }
            }
        }

        private void HydrateCurrentState()
        {
            _previousTexts.Clear();
            _previousTexts.AddRange(GetCurrentArrayValues<TMP_Text>(_textsProperty));
            _previousModifiers.Clear();
            _previousModifiers.AddRange(GetCurrentArrayValues<CharModifier>(_modifiersProperty));
        }

        private static IEnumerable<T> GetCurrentArrayValues<T>(SerializedProperty property)
            where T : Object
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                yield return property.GetArrayElementAtIndex(i).objectReferenceValue as T;
            }
        }

        /*
            For some reason, Editor ChangeCheck does not properly identify when users drag elements into a list.
            So we have to resort to something like this.
            
            Preference is to have non-garbage generating code (like this) in the OnGUI checks for maximum performance.
         */
        private static bool HasChanged<T>(List<T> previous, SerializedProperty property)
            where T : Object
        {
            if (property.arraySize != previous.Count)
            {
                return true;
            }

            for (int i = 0; i < property.arraySize; i++)
            {
                if (previous[i] != property.GetArrayElementAtIndex(i).objectReferenceValue)
                {
                    return true;
                }
            }

            return false;
        }

        // No-alloc, specialized version of LINQ's SetEquals
        private static bool HasChanged<T>(HashSet<T> previous, HashSet<T> change)
        {
            if (previous.Count != change.Count)
            {
                return true;
            }

            foreach (T item in change)
            {
                if (!previous.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
