using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Domain.Event;
using Domain.Event.Channels.General;
using Domain.Services;
using Presentation.UI;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
using Presentation.UI.Framework.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LobbyCanvas : UIScreen
{
    private const string CharactorPanelId = "CharactorPanel";
    private const string RunePanelId = "RunePanel";

    [Header("Lobby References")]
    [SerializeField] private Volume postProcessVolume;

    // 顶部导航按钮
    [SerializeField]
    private Button lobbyNavButton;
    [SerializeField]
    private Button characterNavButton;
    [SerializeField]
    private Button runeNavButton;
    [SerializeField]
    private Button missionNavButton;

    // 功能按钮
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private Button exitRoomButton;

    // UI 状态图片资源
    private Sprite selectedButtonImage;
    private Sprite defaultButtonImage;

    // 符文插槽按钮（位于大厅 UI，用于选中装备目标插槽）
    [SerializeField] private Button runeSlot1Button;
    [SerializeField] private Button runeSlot2Button;
    private Transform runeSlot1Icon;
    private Transform runeSlot2Icon;
    private GameObject selectedRuneIcon;

    // 状态
    private bool isReady = false;
    private bool isInitialized = false;

    // ──────────────────────────────────────────────
    //  UIScreen 生命周期
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
        // 不调用 base.Awake()，因为 UIScreen.Awake() 会 deactivate 自身
        // LobbyCanvas 是场景根 Canvas，需要保持 active
        _canvas = GetComponent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 手动收集子 Widget
        CollectChildWidgets();
    }

    protected override void Start()
    {
        base.Start();

        // 注册到 UIManager
        UIManager.Instance.RegisterScreen(this);

        // 查找 UI 引用并初始化
        Initialize();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SubscribeEvents();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeEvents();
    }

    protected override void SubscribeEvents()
    {
        EventChannelLocator.MainContainer.roomJoinedChannel.RegisterListener(OnRoomJoined);
    }

    protected override void UnsubscribeEvents()
    {
        EventChannelLocator.MainContainer.roomJoinedChannel.UnregisterListener(OnRoomJoined);
    }

    private void OnRoomJoined(Domain.Event.RoomJoinedEventData data)
    {
        // 当加入房间后重新初始化（场景重载等情况）
        Initialize();
        if (!IsOpen)
            Open();
    }

    protected override void Update()
    {
        // 不调用 base.Update() 以避免 closeOnEsc 关闭自身
        // LobbyCanvas 作为根屏幕不应被 Escape 关闭

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
        }

        if (Input.GetMouseButtonDown(0)
            && selectedRuneIcon != null
            && CanDeselectRuneIcons()
            && !IsPointerOverRuneSlot())
        {
            DeselectRuneIcons();
        }
    }

    // ──────────────────────────────────────────────
    //  初始化
    // ──────────────────────────────────────────────

    private void Initialize()
    {
        if (isInitialized)
            return;

        Debug.Log("[LobbyCanvas] Initialize()");

        // 加载资源
        selectedButtonImage = Resources.Load<Sprite>("UI/SelectedButton");
        defaultButtonImage = Resources.Load<Sprite>("UI/DefaultButton");

        // 查找导航按钮
        lobbyNavButton = GameObject.Find("LobbyButton")?.GetComponent<Button>();
        characterNavButton = GameObject.Find("CharactorButton")?.GetComponent<Button>();
        runeNavButton = GameObject.Find("RuneButton")?.GetComponent<Button>();
        missionNavButton = GameObject.Find("MassionButton")?.GetComponent<Button>();

        // 查找功能按钮
        startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        exitRoomButton = GameObject.Find("ExitRoomButton")?.GetComponent<Button>();

        // 查找 PostProcessVolume（如果未在 Inspector 中赋值）
        if (postProcessVolume == null)
            postProcessVolume = GameObject.Find("PostProcessVolume")?.GetComponent<Volume>();

        // 查找符文插槽图标引用（runeSlot1Button/runeSlot2Button 通过 [SerializeField] 从 Inspector 绑定）
        if (runeSlot1Button != null)
            runeSlot1Icon = runeSlot1Button.transform.Find("Icon");
        if (runeSlot2Button != null)
            runeSlot2Icon = runeSlot2Button.transform.Find("Icon");

        // 验证关键引用
        if (startButton == null)
            Debug.LogError("[LobbyCanvas] 找不到 StartButton！");

        // 绑定按钮事件
        BindButtonListeners();

        // 设置默认选中大厅
        SetButtonSelected(lobbyNavButton?.gameObject);

        // 添加符文插槽悬停动画
        if (runeSlot1Button != null)
            AddRuneSlotHoverAnimation(runeSlot1Button.gameObject);
        if (runeSlot2Button != null)
            AddRuneSlotHoverAnimation(runeSlot2Button.gameObject);

        // 订阅 RunePanel 事件
        var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
        if (runePanel != null)
        {
            runePanel.OnRuneEquipped += OnRuneEquippedFromPanel;
        }

        isInitialized = true;
    }

    private void BindButtonListeners()
    {
        if (exitRoomButton != null)
            exitRoomButton.onClick.AddListener(OnExitRoomClicked);

        if (lobbyNavButton != null)
            lobbyNavButton.onClick.AddListener(OnLobbyNavClicked);

        if (characterNavButton != null)
            characterNavButton.onClick.AddListener(OnCharacterNavClicked);

        if (runeNavButton != null)
            runeNavButton.onClick.AddListener(OnRuneNavClicked);

        if (runeSlot1Button != null)
            runeSlot1Button.onClick.AddListener(OnRuneSlot1Clicked);

        if (runeSlot2Button != null)
            runeSlot2Button.onClick.AddListener(OnRuneSlot2Clicked);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
    }

    // ──────────────────────────────────────────────
    //  UI 打开 / 关闭管理（替代旧版 Lobby.GameManager.isOpenUI）
    // ──────────────────────────────────────────────

    private bool IsAnyPanelOpen()
    {
        var charactorPanel = UIManager.Instance.GetScreen(CharactorPanelId);
        bool charactorOpen = charactorPanel != null && (charactorPanel.IsOpen || charactorPanel.IsAnimating);

        var runePanel = UIManager.Instance.GetScreen(RunePanelId);
        bool runeOpen = runePanel != null && (runePanel.IsOpen || runePanel.IsAnimating);

        return charactorOpen || runeOpen;
    }

    /// <summary>直接设置模糊效果及相机旋转状态（不查询面板状态，用于主动打开/关闭时）</summary>
    private void SetBlurAndRotation(bool anyOpen)
    {
        if (postProcessVolume != null)
            postProcessVolume.weight = anyOpen ? 1f : 0f;

        EventChannelLocator.MainContainer.changeCanRotateChannel.Raise(anyOpen);
    }

    /// <summary>根据当前面板状态刷新 UI（用于 ESC 等需要查询实际状态的场景）</summary>
    private void RefreshUIState()
    {
        SetBlurAndRotation(IsAnyPanelOpen());
    }

    // ──────────────────────────────────────────────
    //  导航按钮事件
    // ──────────────────────────────────────────────

    private void OnLobbyNavClicked()
    {
        SetButtonSelected(lobbyNavButton?.gameObject);
        CloseAllPanels();
    }

    private void OnCharacterNavClicked()
    {
        CloseRunePanel();
        SetButtonSelected(characterNavButton?.gameObject);
        OpenCharactorPanel();
    }

    private void OnRuneNavClicked()
    {
        SetButtonSelected(runeNavButton?.gameObject);
        CloseCharactorPanel();
        OpenRunePanel();
    }

    // ──────────────────────────────────────────────
    //  角色面板
    // ──────────────────────────────────────────────

    private void OpenCharactorPanel()
    {
        UIScreen panel = UIManager.Instance.GetScreen(CharactorPanelId);
        if (panel == null)
        {
            Debug.LogError($"[LobbyCanvas] GetScreen(\"{CharactorPanelId}\") 返回 null！请确认 CharactorPanel 已注册");
            return;
        }
        panel.Open();
        // panel.Open() 是异步的，IsOpen 会延迟更新，所以直接主动设置状态
        SetBlurAndRotation(true);
    }

    private void CloseCharactorPanel()
    {
        var panel = UIManager.Instance.GetScreen(CharactorPanelId);
        if (panel == null)
        {
            Debug.LogWarning($"[LobbyCanvas] CloseCharactorPanel: 未找到面板 {CharactorPanelId}");
            return;
        }
        panel.Close();
        // Close() 同样是异步的，IsOpen 还未变为 false，直接主动关闭状态
        SetBlurAndRotation(false);
    }

    // ──────────────────────────────────────────────
    //  符文面板
    // ──────────────────────────────────────────────

    private void OpenRunePanel()
    {
        var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
        if (runePanel == null)
        {
            Debug.LogError($"[LobbyCanvas] GetScreen(\"{RunePanelId}\") 返回 null！请确认 RunePanel 已注册");
            return;
        }

        if (runePanel.IsOpen)
            return;

        runePanel.Open();
        SetBlurAndRotation(true);
    }

    private void CloseRunePanel()
    {
        var runePanel = UIManager.Instance.GetScreen(RunePanelId);
        if (runePanel == null)
        {
            Debug.LogWarning($"[LobbyCanvas] CloseRunePanel: 未找到面板 {RunePanelId}");
            SetBlurAndRotation(false);
            return;
        }

        // 在关闭前先清除符文图标选中状态
        DeselectRuneIcons();

        runePanel.Close();
        // Close() 异步，直接主动关闭模糊
        SetBlurAndRotation(false);
    }

    private void OnRuneSlot1Clicked()
    {
        CloseCharactorPanel();

        SetRuneIconSelected(runeSlot1Button?.gameObject);

        OpenRunePanel();

        // 通知 RunePanel 当前选中的是插槽 0
        var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
        if (runePanel != null)
            runePanel.SetEquipTargetSlot(0);
    }

    private void OnRuneSlot2Clicked()
    {
        CloseCharactorPanel();

        SetRuneIconSelected(runeSlot2Button?.gameObject);

        OpenRunePanel();

        // 通知 RunePanel 当前选中的是插槽 1
        var runePanel = UIManager.Instance.GetScreen(RunePanelId) as RunePanel;
        if (runePanel != null)
            runePanel.SetEquipTargetSlot(1);
    }

    /// <summary>RunePanel 装备符文后的回调</summary>
    private void OnRuneEquippedFromPanel(int slotIndex, Sprite equippedSprite)
    {
        Debug.Log($"[LobbyCanvas] 符文装备回调: slotIndex={slotIndex}");

        // 更新对应插槽图标
        Transform targetIcon = slotIndex == 0 ? runeSlot1Icon : runeSlot2Icon;
        if (targetIcon != null)
        {
            var img = targetIcon.GetComponent<Image>();
            if (img != null)
                img.sprite = equippedSprite;
        }
    }

    // ──────────────────────────────────────────────
    //  符文插槽选中状态
    // ──────────────────────────────────────────────

    private void SetRuneIconSelected(GameObject icon)
    {
        selectedRuneIcon = icon;

        if (runeSlot1Button != null)
        {
            runeSlot1Button.GetComponent<Image>().sprite =
                icon == runeSlot1Button.gameObject ? selectedButtonImage : defaultButtonImage;
            runeSlot1Button.transform.DOScale(icon == runeSlot1Button.gameObject ? 1.1f : 1f, 0.2f);
        }

        if (runeSlot2Button != null)
        {
            runeSlot2Button.GetComponent<Image>().sprite =
                icon == runeSlot2Button.gameObject ? selectedButtonImage : defaultButtonImage;
            runeSlot2Button.transform.DOScale(icon == runeSlot2Button.gameObject ? 1.1f : 1f, 0.2f);
        }
    }

    private void DeselectRuneIcons()
    {
        if (selectedRuneIcon == null)
            return;

        selectedRuneIcon = null;

        if (runeSlot1Button != null)
        {
            runeSlot1Button.GetComponent<Image>().sprite = defaultButtonImage;
            runeSlot1Button.transform.DOScale(1f, 0.2f);
        }

        if (runeSlot2Button != null)
        {
            runeSlot2Button.GetComponent<Image>().sprite = defaultButtonImage;
            runeSlot2Button.transform.DOScale(1f, 0.2f);
        }
    }

    private bool CanDeselectRuneIcons()
    {
        var runePanel = UIManager.Instance.GetScreen(RunePanelId);
        return runePanel == null || (!runePanel.IsOpen && !runePanel.IsAnimating);
    }

    private bool IsPointerOverRuneSlot()
    {
        if (EventSystem.current == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition,
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            GameObject go = result.gameObject;
            if (go == runeSlot1Button?.gameObject
                || go == runeSlot2Button?.gameObject
                || (runeSlot1Button != null && go.transform.IsChildOf(runeSlot1Button.transform))
                || (runeSlot2Button != null && go.transform.IsChildOf(runeSlot2Button.transform)))
            {
                return true;
            }
        }
        return false;
    }

    // ──────────────────────────────────────────────
    //  符文插槽悬停动画
    // ──────────────────────────────────────────────

    private void AddRuneSlotHoverAnimation(GameObject slot)
    {
        var trigger = slot.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slot.AddComponent<EventTrigger>();

        // PointerEnter
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => slot.transform.DOScale(1.1f, 0.2f));
        trigger.triggers.Add(enter);

        // PointerExit（如果未选中则恢复）
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            if (selectedRuneIcon != slot)
                slot.transform.DOScale(1f, 0.2f);
        });
        trigger.triggers.Add(exit);
    }

    // ──────────────────────────────────────────────
    //  关闭所有面板
    // ──────────────────────────────────────────────

    private void CloseAllPanels()
    {
        CloseCharactorPanel();
        CloseRunePanel();
    }

    private void HandleEscape()
    {
        SetButtonSelected(lobbyNavButton?.gameObject);
        CloseAllPanels();
    }

    // ──────────────────────────────────────────────
    //  导航按钮选中状态
    // ──────────────────────────────────────────────

    private void SetButtonSelected(GameObject button)
    {
        if (button == null) return;

        EventSystem.current?.SetSelectedGameObject(button);

        SetNavButtonState(lobbyNavButton, button);
        SetNavButtonState(characterNavButton, button);
        SetNavButtonState(runeNavButton, button);
        SetNavButtonState(missionNavButton, button);
    }

    private void SetNavButtonState(Button btn, GameObject selected)
    {
        if (btn == null) return;
        bool isSelected = btn.gameObject == selected;
        btn.interactable = !isSelected;
        var img = btn.GetComponent<Image>();
        if (img != null)
            img.sprite = isSelected ? selectedButtonImage : defaultButtonImage;
    }

    // ──────────────────────────────────────────────
    //  Start 按钮（就绪）
    // ──────────────────────────────────────────────

    private void OnStartClicked()
    {
        Debug.Log($"[LobbyCanvas] 点击就绪按钮: IsStayLobby={EventChannelLocator.MainContainer.gameSettings.IsStayLobby}, isReady={isReady}");

        if (!EventChannelLocator.MainContainer.gameSettings.IsStayLobby || isReady)
        {
            Debug.LogWarning($"[LobbyCanvas] 阻止就绪: IsStayLobby={EventChannelLocator.MainContainer.gameSettings.IsStayLobby}, isReady={isReady}");
            return;
        }

        EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.SyncAllPlayers);
        isReady = true;

        if (startButton != null)
        {
            startButton.interactable = false;
            var btnText = startButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = "已就绪";
        }

        EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.SetLocalReady);
    }

    // ──────────────────────────────────────────────
    //  退出房间
    // ──────────────────────────────────────────────

    private void OnExitRoomClicked()
    {
        EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.QuitToMainMenu);
    }
}