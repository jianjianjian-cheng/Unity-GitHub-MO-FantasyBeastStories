using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class EventMarkerTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("自定义提示文本（可选，如果设置则覆盖代码设置的文本）")]
        public string customTooltipText = "";

        private string tooltipText;

        public void SetTooltip(string text)
        {
            tooltipText = text;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            string displayText = string.IsNullOrEmpty(customTooltipText)
                ? tooltipText
                : customTooltipText;
            if (!string.IsNullOrEmpty(displayText))
            {
                TooltipManager.Instance?.ShowTooltip(displayText);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipManager.Instance?.HideTooltip();
        }
    }
}
