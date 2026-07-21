using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Core;

namespace UI
{
    /// <summary>
    /// 热更新加载界面。
    /// 游戏启动时显示进度条，等待 Addressables 热更下载完成（至少 2 秒），
    /// 完成后隐藏 Slider 和背景图，启用开始按钮。
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
        [SerializeField] private float minDisplayTime = 2f;

        private void Start()
        {
            // 初始状态：显示进度条和背景，禁用开始按钮
            SetVisible(true);
            if (startButton != null)
                startButton.interactable = false;
            if (progressBar != null)
                progressBar.value = 0f;

            StartCoroutine(WaitForUpdateComplete());
        }

        private IEnumerator WaitForUpdateComplete()
        {
            float startTime = Time.time;

            // 等待热更完成（devMode 下瞬间完成）
            while (!AddressablesUpdater.IsUpdateComplete)
            {
                float progress = AddressablesUpdater.DownloadProgress;
                UpdateProgressUI(progress);
                yield return null;
            }

            // 热更已完成，确保进度条显示 100%
            UpdateProgressUI(1f);

            // 保证最小显示时长（至少 2 秒）
            float elapsed = Time.time - startTime;
            if (elapsed < minDisplayTime)
            {
                yield return new WaitForSeconds(minDisplayTime - elapsed);
            }

            // 隐藏进度条和背景图，启用开始按钮
            SetVisible(false);
            if (startButton != null)
                startButton.interactable = true;
        }

        private void UpdateProgressUI(float progress)
        {
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(progress);

            if (progressText != null)
            {
                if (AddressablesUpdater.State == AddressablesUpdater.UpdateState.Downloading)
                {
                    var mb = AddressablesUpdater.TotalDownloadBytes / 1024f / 1024f;
                    progressText.text = $"下载资源中 {mb:F1}MB  ({progress * 100:F0}%)";
                }
                else if (AddressablesUpdater.State == AddressablesUpdater.UpdateState.Complete)
                {
                    progressText.text = "加载完成";
                }
                else
                {
                    progressText.text = "加载中...";
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
    }
}
