using UnityEngine;
using UnityEngine.UI;
using Core;

namespace UI
{
    /// <summary>
    /// 热更新加载界面。
    /// 挂在 StartMenuCanvas 上，显示下载进度条和背景图。
    /// 仅在检测到更新时才显示，至少保持 3 秒，Slider 平滑过渡。
    /// </summary>
    public class HotfixLoadingUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text progressText;
        [SerializeField] private GameObject backgroundImage;

        [Header("开始按钮（下载完成前禁用）")]
        [SerializeField] private Button startButton;

        [Header("最小显示时长（秒）")]
        [SerializeField] private float minDisplayTime = 3f;

        // 平滑过渡的当前进度值
        private float _displayedProgress;
        private bool _shown;
        private bool _completed;

        private void Start()
        {
            // 初始隐藏，等检测到更新再显示
            SetVisible(false);
            _displayedProgress = 0f;

            if (startButton != null)
                startButton.interactable = false;
        }

        private void Update()
        {
            if (!_shown)
            {
                // 检测到需要更新时才开始显示
                if (AddressablesUpdater.HasRemoteUpdate ||
                    AddressablesUpdater.State == AddressablesUpdater.UpdateState.Checking ||
                    AddressablesUpdater.State == AddressablesUpdater.UpdateState.Downloading)
                {
                    _shown = true;
                    SetVisible(true);
                }
                return;
            }

            if (_completed) return;

            // 平滑过渡 Slider：从当前值 lerp 到实际进度
            float target = AddressablesUpdater.DownloadProgress;
            _displayedProgress = Mathf.Lerp(_displayedProgress, target, Time.deltaTime * 5f);
            // 确保不会倒退，且至少显示一点进度
            if (_displayedProgress < target && target - _displayedProgress < 0.001f)
                _displayedProgress = target;

            if (progressBar != null)
                progressBar.value = _displayedProgress;

            // 更新文字
            if (progressText != null)
            {
                switch (AddressablesUpdater.State)
                {
                    case AddressablesUpdater.UpdateState.Checking:
                        progressText.text = "检查更新中...";
                        break;
                    case AddressablesUpdater.UpdateState.Downloading:
                        var mb = AddressablesUpdater.TotalDownloadBytes / 1024f / 1024f;
                        progressText.text = $"下载资源中 {mb:F1}MB  ({_displayedProgress * 100:F0}%)";
                        break;
                    default:
                        progressText.text = "加载中...";
                        break;
                }
            }

            // 下载完成 → 等待最小显示时长
            if (AddressablesUpdater.IsUpdateComplete)
            {
                _displayedProgress = 1f;
                if (progressBar != null)
                    progressBar.value = 1f;
                if (progressText != null)
                    progressText.text = "加载完成";

                float elapsed = Time.time - AddressablesUpdater.DownloadStartTime;
                if (elapsed >= minDisplayTime)
                {
                    _completed = true;
                    Hide();
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (progressBar != null)
                progressBar.gameObject.SetActive(visible);
            if (backgroundImage != null)
                backgroundImage.SetActive(visible);
        }

        private void Hide()
        {
            SetVisible(false);
            if (startButton != null)
                startButton.interactable = true;
        }
    }
}