namespace TextTween
{
    using Attributes;
    using TMPro;
    using UnityEngine;

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class TextTweenAgent : MonoBehaviour
    {
        [SerializeField, ReadOnly] 
        private TextTweenManager _owner;

        private TMP_Text _text;
        private bool _marked;
        
        private void OnEnable()
        {
            hideFlags = HideFlags.HideInInspector;
            _text = GetComponent<TMP_Text>();
        }

        internal void SetOwner(TextTweenManager owner)
        {
            if (_owner != null)
            {
                _owner.Remove(_text, true);
            }
            _owner = owner;
        }

        internal void Remove()
        {
            if (_marked)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        private void OnDestroy()
        {
            if (_owner != null && !_marked)
            {
                _marked = true;
                _owner.Remove(_text);
            }
        }
    }
}