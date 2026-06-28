using UnityEngine;
using UnityEngine.UI;

namespace Presentation.UI.Framework.Utils
{
    public static class UIComponentFinder
    {
        public static T FindComponent<T>(Transform parent, string path) where T : Component
        {
            if (parent == null || string.IsNullOrEmpty(path))
                return null;

            Transform target = parent.Find(path);
            if (target == null)
            {
                Debug.LogWarning($"UIComponentFinder: 未找到路径 {path}");
                return null;
            }

            return target.GetComponent<T>();
        }

        public static T FindComponentInChildren<T>(Transform parent, string name = "") where T : Component
        {
            if (parent == null)
                return null;

            if (string.IsNullOrEmpty(name))
                return parent.GetComponentInChildren<T>(true);

            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child.GetComponent<T>();
                }

                T found = FindComponentInChildren<T>(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static Text FindText(Transform parent, string path)
        {
            return FindComponent<Text>(parent, path);
        }

        public static Image FindImage(Transform parent, string path)
        {
            return FindComponent<Image>(parent, path);
        }

        public static Slider FindSlider(Transform parent, string path)
        {
            return FindComponent<Slider>(parent, path);
        }

        public static Button FindButton(Transform parent, string path)
        {
            return FindComponent<Button>(parent, path);
        }

        public static Toggle FindToggle(Transform parent, string path)
        {
            return FindComponent<Toggle>(parent, path);
        }

        public static InputField FindInputField(Transform parent, string path)
        {
            return FindComponent<InputField>(parent, path);
        }

        public static ScrollRect FindScrollRect(Transform parent, string path)
        {
            return FindComponent<ScrollRect>(parent, path);
        }

        public static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            return component;
        }

        public static RectTransform GetRectTransform(Component component)
        {
            return component.GetComponent<RectTransform>();
        }

        public static RectTransform GetRectTransform(GameObject obj)
        {
            return obj.GetComponent<RectTransform>();
        }

        public static Canvas GetCanvas(Component component)
        {
            return component.GetComponentInParent<Canvas>();
        }

        public static CanvasGroup GetCanvasGroup(Component component)
        {
            return component.GetComponent<CanvasGroup>();
        }

        public static CanvasGroup GetOrAddCanvasGroup(GameObject obj)
        {
            return GetOrAddComponent<CanvasGroup>(obj);
        }
    }
}