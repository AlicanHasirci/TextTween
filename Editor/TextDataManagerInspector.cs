namespace TextTween.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEditor;

    [CustomEditor(typeof(TextTweenManager))]
    public class TextDataManagerInspector : Editor
    {
        private TextTweenManager _manager;
        private SerializedProperty _textsProperty;
        private SerializedProperty _modifiersProperty;

        private readonly List<TMP_Text> _previousTexts = new();
        private readonly List<CharModifier> _previousModifiers = new();

        private void OnEnable()
        {
            _manager = (TextTweenManager)target;
            _textsProperty = serializedObject.FindProperty(nameof(TextTweenManager.Texts));
            _modifiersProperty = serializedObject.FindProperty(nameof(TextTweenManager.Modifiers));
            HydrateCurrentState();
        }

        public override void OnInspectorGUI()
        {
            TextTweenManager tweenManager = ((TextTweenManager)target);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, nameof(TextTweenManager.MeshData));
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

        private void HydrateCurrentState()
        {
            _previousTexts.Clear();
            _previousTexts.AddRange(GetCurrentArrayValues<TMP_Text>(_textsProperty));
            _previousModifiers.Clear();
            _previousModifiers.AddRange(GetCurrentArrayValues<CharModifier>(_modifiersProperty));
        }

        private static IEnumerable<T> GetCurrentArrayValues<T>(SerializedProperty property)
            where T : UnityEngine.Object
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
            where T : UnityEngine.Object
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
    }
}
