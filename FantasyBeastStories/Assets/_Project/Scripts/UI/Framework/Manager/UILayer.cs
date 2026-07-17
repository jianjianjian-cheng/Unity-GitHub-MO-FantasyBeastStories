using UnityEngine;

namespace UI.Framework
{
    public enum UILayer
    {
        Background = 0,
        Normal = 1,
        HUD = 2,
        Popup = 3,
        Top = 4,
        Loading = 5
    }

    public static class UILayerExtensions
    {
        public static int ToSortingOrder(this UILayer layer)
        {
            return (int)layer * 100;
        }

        public static string ToLayerName(this UILayer layer)
        {
            switch (layer)
            {
                case UILayer.Background: return "Background";
                case UILayer.Normal: return "Normal";
                case UILayer.HUD: return "HUD";
                case UILayer.Popup: return "Popup";
                case UILayer.Top: return "Top";
                case UILayer.Loading: return "Loading";
                default: return "Normal";
            }
        }
    }
}