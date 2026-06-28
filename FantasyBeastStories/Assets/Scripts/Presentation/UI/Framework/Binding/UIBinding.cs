using System;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI.Framework.Binding
{
    public abstract class UIBinding : MonoBehaviour
    {
        [Header("数据绑定设置")]
        [SerializeField] protected bool autoBindOnStart = true;

        protected virtual void Start()
        {
            if (autoBindOnStart)
                Bind();
        }

        public abstract void Bind();
        public abstract void Unbind();

        protected virtual void OnDestroy()
        {
            Unbind();
        }
    }

    public class PropertyBinding<T> : UIBinding
    {
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(value);
            }
        }

        private T _value;
        public event Action<T> OnValueChanged;

        public override void Bind() { }

        public override void Unbind()
        {
            OnValueChanged = null;
        }

        public void SetValueSilent(T value)
        {
            _value = value;
        }
    }

    [Serializable]
    public class TextBinding : PropertyBinding<string>
    {
        [SerializeField] private Text targetText;

        public override void Bind()
        {
            OnValueChanged += UpdateText;
        }

        public override void Unbind()
        {
            OnValueChanged -= UpdateText;
            base.Unbind();
        }

        private void UpdateText(string value)
        {
            if (targetText != null)
                targetText.text = value;
        }
    }

    [Serializable]
    public class ImageBinding : PropertyBinding<Sprite>
    {
        [SerializeField] private Image targetImage;

        public override void Bind()
        {
            OnValueChanged += UpdateImage;
        }

        public override void Unbind()
        {
            OnValueChanged -= UpdateImage;
            base.Unbind();
        }

        private void UpdateImage(Sprite value)
        {
            if (targetImage != null)
                targetImage.sprite = value;
        }
    }

    [Serializable]
    public class SliderBinding : PropertyBinding<float>
    {
        [SerializeField] private Slider targetSlider;

        public override void Bind()
        {
            OnValueChanged += UpdateSlider;
        }

        public override void Unbind()
        {
            OnValueChanged -= UpdateSlider;
            base.Unbind();
        }

        private void UpdateSlider(float value)
        {
            if (targetSlider != null)
                targetSlider.value = value;
        }
    }

    [Serializable]
    public class ToggleBinding : PropertyBinding<bool>
    {
        [SerializeField] private Toggle targetToggle;

        public override void Bind()
        {
            OnValueChanged += UpdateToggle;
        }

        public override void Unbind()
        {
            OnValueChanged -= UpdateToggle;
            base.Unbind();
        }

        private void UpdateToggle(bool value)
        {
            if (targetToggle != null)
                targetToggle.isOn = value;
        }
    }
}