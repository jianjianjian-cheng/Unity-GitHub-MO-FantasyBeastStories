using Application;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Presentation.UI.Framework
{
    /// <summary>
    /// UI 音效组件 — 拖拽到任何 UI 元素上，自动播放悬浮/点击音效
    /// 无需修改按钮原有代码，即插即用
    /// </summary>
    public class UISoundEffect : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("音效 ID 配置（在 SoundLibrary 中定义）")]
        [Tooltip("鼠标悬浮时播放的音效 ID")]
        public string hoverSoundId = "sfx_btn_hover";

        [Tooltip("点击时播放的音效 ID")]
        public string clickSoundId = "sfx_button_click";

        [Header("选项")]
        [Tooltip("悬浮音效的冷却时间（秒），防止快速移动时频繁触发")]
        public float hoverCooldown = 0.1f;

        private float _lastHoverTime;

        /// <summary>鼠标进入（悬浮）时播放音效</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Time.unscaledTime - _lastHoverTime < hoverCooldown)
                return;

            _lastHoverTime = Time.unscaledTime;

            if (!string.IsNullOrEmpty(hoverSoundId))
                AudioManager.Instance.PlayUI(hoverSoundId);
        }

        /// <summary>点击时播放音效</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(clickSoundId))
                AudioManager.Instance.PlayUI(clickSoundId);
        }
    }
}