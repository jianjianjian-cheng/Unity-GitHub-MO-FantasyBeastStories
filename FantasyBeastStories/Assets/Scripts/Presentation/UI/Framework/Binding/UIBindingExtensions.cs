using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI.Framework.Binding
{
    public static class UIBindingExtensions
    {
        public static void BindText(this Text text, PropertyBinding<string> binding)
        {
            binding.OnValueChanged += value => text.text = value;
        }

        public static void BindImage(this Image image, PropertyBinding<Sprite> binding)
        {
            binding.OnValueChanged += value => image.sprite = value;
        }

        public static void BindSlider(this Slider slider, PropertyBinding<float> binding)
        {
            binding.OnValueChanged += value => slider.value = value;
        }

        public static void BindToggle(this Toggle toggle, PropertyBinding<bool> binding)
        {
            binding.OnValueChanged += value => toggle.isOn = value;
        }

        public static void BindColor(this Graphic graphic, PropertyBinding<Color> binding)
        {
            binding.OnValueChanged += value => graphic.color = value;
        }

        public static void BindActive(this GameObject obj, PropertyBinding<bool> binding)
        {
            binding.OnValueChanged += value => obj.SetActive(value);
        }

        public static PropertyBinding<string> ToBinding(this string value)
        {
            var binding = new PropertyBinding<string>();
            binding.Value = value;
            return binding;
        }

        public static PropertyBinding<float> ToBinding(this float value)
        {
            var binding = new PropertyBinding<float>();
            binding.Value = value;
            return binding;
        }

        public static PropertyBinding<bool> ToBinding(this bool value)
        {
            var binding = new PropertyBinding<bool>();
            binding.Value = value;
            return binding;
        }

        public static PropertyBinding<Sprite> ToBinding(this Sprite value)
        {
            var binding = new PropertyBinding<Sprite>();
            binding.Value = value;
            return binding;
        }

        public static PropertyBinding<Color> ToBinding(this Color value)
        {
            var binding = new PropertyBinding<Color>();
            binding.Value = value;
            return binding;
        }
    }
}