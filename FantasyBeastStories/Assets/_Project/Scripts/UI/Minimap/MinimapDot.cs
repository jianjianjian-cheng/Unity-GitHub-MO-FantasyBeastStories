using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 小地图红点：由 MinimapWidget 外部驱动位置与显隐。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MinimapDot : MonoBehaviour
    {
        private Image _image;
        private RectTransform _rectTransform;

        void Awake()
        {
            _image = GetComponentInChildren<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            _rectTransform.anchoredPosition = anchoredPosition;
        }

        public void SetColor(Color color)
        {
            if (_image != null)
                _image.color = color;
        }

        public void SetSize(float size)
        {
            _rectTransform.sizeDelta = new Vector2(size, size);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
