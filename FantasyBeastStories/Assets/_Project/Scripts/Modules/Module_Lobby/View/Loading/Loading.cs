using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Core;
using Controllers.Battle;
using Controllers.Game;

namespace UI.Framework.Panel
{
    /// <summary>
    /// Loading 面板 — 完全独立的 UI 单例，不继承 UIScreen。
    /// 遵循项目标准单例模式（与 UIManager / PlayerManager 相同）。
    /// </summary>
    public class Loading : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private Coroutine _bounceRoutine;
        private Tween _iconPopTween;
        private Sequence _iconRotateSeq;

        [Header("子对象引用")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI loadingText;

        // TMP 逐字弹跳用
        private Vector3[][] _charBaseVertices;
        private int[] _charMeshIndex;
        private int[] _charVertexIndex;
        private int _charCount;

        #region 单例模式

        private static Loading _instance;

        private bool _isDuplicate;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                _isDuplicate = true;
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ServiceLocator.Register(this);
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        #endregion


        private void Initialize()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            // 添加 CanvasScaler 支持手机分辨率适配
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            // icon 恢复为初始状态
            if (icon != null)
            {
                icon.transform.localScale = Vector3.zero;
                icon.transform.localEulerAngles = Vector3.zero;
            }
        }

        private void OnEnable()
        {
            if (_isDuplicate) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            if (_isDuplicate) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<Loading>();
            }
            KillIconTweens();
            StopBounceAnimation();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #region Icon 开场动画

        /// <summary>
        /// icon 蹦出来 + 分段旋转（4 周期 × 90°，每周期间隔 0.5s）
        /// </summary>
        private void PlayIconOpen()
        {
            if (icon == null) return;

            KillIconTweens();

            // 重置：从完全透明、零尺寸、无旋转开始
            icon.transform.localScale = Vector3.zero;
            icon.transform.localEulerAngles = Vector3.zero;
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0f);

            // ★★★ 震撼开场：急速淡入 + 大 overshoot 弹出 ★★★
            //     scale 冲到 1.25 再弹回 1.0，配合 OutBack overshoot:4 制造"炸出"感
            Sequence openSeq = DOTween.Sequence();
            openSeq
                .Join(icon.DOFade(1f, 0.08f))                                    // 瞬间显形
                .Join(icon.transform.DOScale(1.25f, 0.2f).SetEase(Ease.OutBack, overshoot: 4f)) // 炸出
                .Append(icon.transform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad));               // 回落

            _iconPopTween = openSeq;

            // 开场结束后才开始循环旋转
            openSeq.OnComplete(() =>
            {
                if (this == null) return;
                _iconRotateSeq = DOTween.Sequence();
                for (int i = 0; i < 4; i++)
                {
                    _iconRotateSeq
                        .AppendInterval(0.5f)
                        .Append(
                            icon.transform
                                .DOBlendableRotateBy(new Vector3(0f, 0f, 90f), 0.25f)
                                .SetEase(Ease.OutQuad)
                        );
                }
                _iconRotateSeq.SetLoops(-1, LoopType.Restart);
                _iconRotateSeq.Play();
            });
        }

        private void KillIconTweens()
        {
            if (_iconPopTween != null && _iconPopTween.IsActive())
                _iconPopTween.Kill();
            _iconPopTween = null;

            if (_iconRotateSeq != null && _iconRotateSeq.IsActive())
                _iconRotateSeq.Kill();
            _iconRotateSeq = null;
        }

        #endregion

        #region "加载中"逐字弹跳

        private void CacheCharVertices()
        {
            if (loadingText == null) return;

            // ★★★ 必须强制刷新 Canvas 布局，textInfo 才会正确生成 ★★★
            Canvas.ForceUpdateCanvases();
            loadingText.ForceMeshUpdate();

            var info = loadingText.textInfo;
            if (info == null) return;

            _charCount = Mathf.Min(3, info.characterCount);
            if (_charCount == 0) return;

            _charBaseVertices = new Vector3[_charCount][];
            _charMeshIndex = new int[_charCount];
            _charVertexIndex = new int[_charCount];

            for (int i = 0; i < _charCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                _charMeshIndex[i] = ch.materialReferenceIndex;
                _charVertexIndex[i] = ch.vertexIndex;

                var verts = info.meshInfo[_charMeshIndex[i]].vertices;
                _charBaseVertices[i] = new Vector3[4];
                System.Array.Copy(verts, _charVertexIndex[i], _charBaseVertices[i], 0, 4);
            }
        }

        private void StartBounceAnimation()
        {
            StopBounceAnimation();
            CacheCharVertices();
            if (_charCount == 0) return;

            _bounceRoutine = StartCoroutine(BounceRoutine());
        }

        private void StopBounceAnimation()
        {
            if (_bounceRoutine != null)
            {
                StopCoroutine(_bounceRoutine);
                _bounceRoutine = null;
            }

            if (loadingText != null && _charBaseVertices != null)
            {
                loadingText.ForceMeshUpdate();
                var info = loadingText.textInfo;
                for (int i = 0; i < _charCount; i++)
                {
                    if (_charBaseVertices[i] == null) continue;
                    int m = _charMeshIndex[i];
                    int vi = _charVertexIndex[i];
                    for (int j = 0; j < 4; j++)
                        info.meshInfo[m].vertices[vi + j] = _charBaseVertices[i][j];
                }
                loadingText.UpdateVertexData();
            }
        }

        private IEnumerator BounceRoutine()
        {
            float bounceHeight = 12f;
            float cycleTime = 0.5f;
            float stagger = 0.18f;

            while (true)
            {
                float elapsed = Time.time;

                for (int i = 0; i < _charCount; i++)
                {
                    if (_charBaseVertices[i] == null) continue;

                    float t = Mathf.Repeat(elapsed / cycleTime + i * (stagger / cycleTime), 1f);

                    float offset;
                    if (t < 0.35f)
                    {
                        float p = t / 0.35f;
                        offset = bounceHeight * (1f - (1f - p) * (1f - p));
                    }
                    else
                    {
                        float p = (t - 0.35f) / 0.65f;
                        offset = bounceHeight * (1f - p) * (1f - p);
                    }

                    var info = loadingText.textInfo;
                    int mi = _charMeshIndex[i];
                    int vi = _charVertexIndex[i];
                    for (int j = 0; j < 4; j++)
                    {
                        info.meshInfo[mi].vertices[vi + j].y = _charBaseVertices[i][j].y + offset;
                    }
                }

                loadingText.UpdateVertexData();
                yield return null;
            }
        }

        #endregion

        #region 场景切换回调

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[Loading] OnSceneLoaded 触发，场景: {scene.name} (buildIndex={scene.buildIndex})");
            // 只有返回大厅时才执行结算，进入战斗场景不结算
            bool isLobby = scene.buildIndex == 1;
            StartCoroutine(HideWithMinDisplay(isLobby));
        }

        private IEnumerator HideWithMinDisplay(bool finalizeMatch)
        {
            // 延迟 1s，场景已切换成功，但 loading 动画继续播放
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(Hide(finalizeMatch));
        }

        #endregion

        #region 公开接口

        public IEnumerator Show()
        {
            _canvasGroup.blocksRaycasts = true;

            // ★★★ icon 蹦出 + 旋转 ★★★
            PlayIconOpen();

            // ★★★ "加载中"逐字弹跳 ★★★
            StartBounceAnimation();

            // 协程淡入
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = elapsed / duration;
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(1f);
        }

        public IEnumerator Hide(bool finalizeMatch = false)
        {
            _canvasGroup.alpha = 0.5f;
            _canvasGroup.blocksRaycasts = true;

            // 停止所有持续动画
            KillIconTweens();
            StopBounceAnimation();

            // ★★★ 关闭动画：icon 转圈缩小 → 文字缩小 → 整体淡出 ★★★
            float duration = 0.4f;

            // 1) Icon: 高速自转 360° + 缩小到 0 + 渐隐
            if (icon != null)
            {
                icon.transform
                    .DOBlendableRotateBy(new Vector3(0f, 0f, 360f), duration * 0.55f)
                    .SetEase(Ease.InCubic)
                    .Play();
                icon.transform
                    .DOScale(0f, duration)
                    .SetEase(Ease.InBack)
                    .Play();
                icon
                    .DOFade(0f, duration * 0.6f)
                    .Play();
            }

            // 2) 文字: 延迟 0.15s 后缩小到 0
            if (loadingText != null)
            {
                loadingText.transform
                    .DOScale(0f, duration * 0.45f)
                    .SetDelay(0.15f)
                    .SetEase(Ease.InBack)
                    .Play();
            }

            // 3) Canvas 淡出
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            // 收尾：确保所有元素复位到隐藏状态
            if (icon != null)
            {
                icon.transform.localScale = Vector3.zero;
                icon.transform.localEulerAngles = Vector3.zero;
                icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0f);
            }
            if (loadingText != null)
                loadingText.transform.localScale = Vector3.one;

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;

            yield return new WaitForSeconds(1f);

            // 1) 对局结算（仅返回大厅时执行，有实际数据时才发放金币并弹结算面板）
            if (finalizeMatch && ServiceLocator.TryGet<MatchStatisticsManager>(out var matchStats))
            {
                Debug.Log($"[Loading.Hide] FinalizeMatch called: kills={matchStats.GetTotalKillsInMatch()}, damage={matchStats.Model.TotalDamageInMatch}, exp={matchStats.Model.TotalExpInMatch}");
                matchStats.FinalizeMatch();
            }

            // 2) 广播当前金币数，初始化金币 UI 显示
            if (ServiceLocator.TryGet<CoinManager>(out var coinManager))
            {
                coinManager.BroadcastCurrentCoins();
            }
        }

        #endregion
    }
}