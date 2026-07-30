using UI.Framework.Base;
using UI.Framework.Manager;
using UnityEngine;
using UI.Lobby;

namespace UI.Framework.Panel
{
    /// <summary>
    /// 音量调节面板：包含 AudioSettingsWidget，打开时同步当前音量到滑块。
    /// </summary>
    public class AudioSettingsPanel : UIScreen
    {
        [SerializeField] private AudioSettingsWidget audioSettingsWidget;

        protected override void Awake()
        {
            screenId = "AudioSettingsPanel";
            base.Awake();
            UIManager.Instance.RegisterScreen(this);
        }

        protected override void OnBeforeOpen()
        {
            base.OnBeforeOpen();
            if (audioSettingsWidget != null)
                audioSettingsWidget.RefreshFromAudioManager();
        }
    }
}
