namespace Presentation.UI.Framework.Utils
{
    public static class UIConstants
    {
        public const string CanvasName = "UICanvas";
        public const string UIManagerName = "UIManager";
        public const string EventSystemName = "EventSystem";

        public const string ResourcesPath = "UI";
        public const string PrefabsPath = "Prefabs/UI";
        public const string AnimationsPath = "Animations/UI";

        public const string MaskPrefix = "Mask_";
        public const string PanelPrefix = "Panel_";
        public const string WidgetPrefix = "Widget_";
        public const string ViewPrefix = "View_";

        public const string ButtonClickSound = "ButtonClick";
        public const string PanelOpenSound = "PanelOpen";
        public const string PanelCloseSound = "PanelClose";

        public const float DefaultAnimationDuration = 0.3f;
        public const float DefaultFadeDuration = 0.2f;
        public const float DefaultSlideDuration = 0.3f;
        public const float DefaultScaleDuration = 0.2f;

        public const float MaskAlpha = 0.5f;
        public const int LayerSortingOrderOffset = 100;

        public static class ScreenIds
        {
            public const string Loading = "Loading";
            public const string MainMenu = "MainMenu";
            public const string Lobby = "Lobby";
            public const string LobbyCharacter = "LobbyCharacter";
            public const string LobbyRune = "LobbyRune";
            public const string CombatHUD = "CombatHUD";
            public const string BossHP = "BossHP";
            public const string MagicUpgrade = "MagicUpgrade";
            public const string Pause = "Pause";
            public const string Tooltip = "Tooltip";
            public const string Settings = "Settings";
            public const string Inventory = "Inventory";
            public const string MatchResult = "MatchResult";
        }

        public static class AnimationNames
        {
            public const string FadeIn = "FadeIn";
            public const string FadeOut = "FadeOut";
            public const string SlideInLeft = "SlideInLeft";
            public const string SlideInRight = "SlideInRight";
            public const string SlideInUp = "SlideInUp";
            public const string SlideInDown = "SlideInDown";
            public const string ScaleIn = "ScaleIn";
            public const string ScaleOut = "ScaleOut";
        }
    }
}