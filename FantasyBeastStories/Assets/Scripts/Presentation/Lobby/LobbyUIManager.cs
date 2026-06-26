using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Domain.Event;
using Domain.Event.Channels.General;
using Presentation.UI;
using Domain.CardData;

namespace Presentation.Lobby
{
  public class LobbyUIManager : MonoBehaviour
  {
    //退出房间按钮
    private GameObject exitRoomButton;

    //符文部分
    private GameObject runeButton; //大厅打开符文面板
    private GameObject RunePanel; //整个符文面板对象
    private List<GameObject> runeList = new List<GameObject>(); //符文图标按钮数组
    private GameObject EquipButton; //符文装备按钮
    private int[] currentEquippedRuneIds = new int[2]; //当前装备的符文ID数组

    //角色选择面板
    private GameObject nextCharacterButton; //下一个角色按钮
    private GameObject previousCharacterButton; //上一个角色按钮
    private GameObject SwitchButton; //切换角色按钮
    private GameObject CharactorIll; //当前角色
    private GameObject CharactorShowPosition; //当前角色位置对象
    private int currentCharactorIndex = 0; //当前角色索引
    private GameObject currentCharactorInstance; //当前角色实例

    private GameObject MassionButton;
    private Text nameUIText;
    private GameObject startButton;
    private GameObject CharactorPanel;
    private Volume PostProcessVolume;
    public GameObject lobbyButton;
    private GameObject characterButton;
    private GameObject RuneIcon_1;
    private GameObject RuneIcon_2;
    private GameObject selectedRuneIcon;
    private GameObject selectedRuneListItem;
    private Sprite selectedButtonImage;
    private Sprite defaultButtonImage;

    private string eventName = CharacterCardType.WizardBoy;
    private bool isReady = false;

    void Start()
    {
      Debug.Log("[LobbyUIManager] Start() 被调用");
      Intilize();
    }

    void OnEnable()
    {
      Debug.Log("[LobbyUIManager] OnEnable() — 注册 roomJoinedChannel 监听");
      EventChannelLocator.MainContainer.roomJoinedChannel.RegisterListener(OnRoomJoined);
    }

    void OnDisable()
    {
      EventChannelLocator.MainContainer.roomJoinedChannel.UnregisterListener(OnRoomJoined);
    }

    private void OnRoomJoined(RoomJoinedEventData data)
    {
      Intilize();
    }

    private IEnumerator DelayedRuneInitialization()
    {
      // 等待一帧确保所有UI组件都已初始化
      yield return null;

      if (runeList.Count > 0)
      {
        SetRuneListSelected(runeList[0]);
      }
    }

    public void Intilize()
    {
      Debug.Log("[LobbyUIManager] Initilize() 开始执行");
      var launcher = FindObjectOfType<Infrastructure.Network.Launcher>();
      if (launcher == null)
      {
        Debug.LogError("[LobbyUIManager] Launcher 为 null，请检查场景中 Launcher 脚本的组件引用");
        return;
      }

      EquipButton = launcher.GetInactiveObjectByName("EquipButton");
      MassionButton = GameObject.Find("MassionButton");
      runeButton = GameObject.Find("RuneButton");
      RunePanel = launcher.GetInactiveObjectByName("RunePanel");
      nameUIText = GameObject.Find("NameUIText")?.GetComponent<Text>();
      PostProcessVolume = GameObject.Find("PostProcessVolume")?.GetComponent<Volume>();
      CharactorPanel = launcher.GetInactiveObjectByName("CharactorPanel");
      selectedButtonImage = Resources.Load<Sprite>("UI/SelectedButton");
      defaultButtonImage = Resources.Load<Sprite>("UI/DefaultButton");
      lobbyButton = GameObject.Find("LobbyButton");
      characterButton = GameObject.Find("CharactorButton");
      RuneIcon_1 = GameObject.Find("RuneIcon_1");
      RuneIcon_2 = GameObject.Find("RuneIcon_2");
      startButton = GameObject.Find("StartButton");
      if (startButton == null)
        Debug.LogError("[LobbyUIManager] 找不到 StartButton！请检查场景中是否存在名为 StartButton 的 GameObject");
      exitRoomButton = GameObject.Find("ExitRoomButton");

      //角色选择面板
      CharactorIll = launcher.GetInactiveObjectByName("CharactorIll");
      nextCharacterButton = launcher.GetInactiveObjectByName("nextCharacterButton");
      previousCharacterButton = launcher.GetInactiveObjectByName("previousCharacterButton");
      SwitchButton = launcher.GetInactiveObjectByName("SwitchButton");
      CharactorShowPosition = GameObject.Find("CharactorShowPosition");

      //翻转nextCharacterButton的图片
      Image previousCharacterButtonImage = previousCharacterButton.GetComponent<Image>();
      Vector3 scale = previousCharacterButtonImage.transform.localScale;
      scale.x *= -1;
      previousCharacterButtonImage.transform.localScale = scale;

      SwitchCharactor(0); //默认角色为第一个角色
                          //添加角色选择按钮的点击事件
      previousCharacterButton
          .GetComponent<Button>()
          .onClick.AddListener(() =>
          {
            int newIndex = currentCharactorIndex - 1;
            if (newIndex < 0)
              newIndex = 0;
            SwitchCharactor(newIndex);
          });

      nextCharacterButton
          .GetComponent<Button>()
          .onClick.AddListener(() =>
          {
            int newIndex = currentCharactorIndex + 1;
            if (newIndex >= 2)
              newIndex = 1;
            SwitchCharactor(newIndex);
          });
      //添加切换按钮的点击事件
      SwitchButton
          .GetComponent<Button>()
          .onClick.AddListener(() =>
          {
            SwitchCharactorButtonClicked();
          });

      //添加退出房间按钮的点击事件
      exitRoomButton
          .GetComponent<Button>()
          .onClick.AddListener(() =>
          {
            EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.QuitToMainMenu);
          });

      //寻找符文插槽
      FindRuneIcons();

      //设置默认选中大厅按钮
      SetButtonSelected(lobbyButton);
      lobbyButton.GetComponent<Button>().onClick.AddListener(LobbyButtonOnClick);
      characterButton.GetComponent<Button>().onClick.AddListener(CharacterButtonOnClick);
      RuneIcon_1.GetComponent<Button>().onClick.AddListener(Rune_1ButtonOnClick);
      RuneIcon_2.GetComponent<Button>().onClick.AddListener(Rune_2ButtonOnClick);
      startButton.GetComponent<Button>().onClick.AddListener(StartButtonOnClick);
      runeButton.GetComponent<Button>().onClick.AddListener(RuneButtonOnClick);
      EquipButton.GetComponent<Button>().onClick.AddListener(EquipButtonOnClick);

      for (int i = 0; i < runeList.Count; i++)
      {
        GameObject runeIcon = runeList[i];
        if (runeIcon == null)
          continue;

        int index = i;
        runeIcon
            .GetComponent<Button>()
            .onClick.AddListener(() => OnRuneListItemClicked(runeList[index]));
      }

      StartCoroutine(DelayedRuneInitialization());

      // 添加RuneIcon的鼠标悬停动画
      AddRuneIconHoverAnimation(RuneIcon_1);
      AddRuneIconHoverAnimation(RuneIcon_2);
    }

    void Update()
    {
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        SetButtonSelected(lobbyButton); // 调整后处理效果的权重，例如模糊效果
        HideCharactorPanel();
        HideRunePanel();
      }

      if (
          Input.GetMouseButtonDown(0)
          && selectedRuneIcon != null
          && CanDeselectRuneIcons()
          && !IsPointerOverRuneIcon()
      )
      {
        DeselectRuneIcons();
      }
    }

    private void FindRuneIcons()
    {
      runeList.Clear();
      var launcher = FindObjectOfType<Infrastructure.Network.Launcher>();
      for (int i = 1; i <= 2; i++)
      {
        GameObject runeIcon = launcher != null ? launcher.GetInactiveObjectByName($"RuneSlot_{i}") : null;
        if (runeIcon != null)
        {
          runeList.Add(runeIcon);
        }
        else
        {
          Debug.LogWarning($"未找到 RuneIcon_{i} 按钮");
        }
      }
    }

    private void EquipButtonOnClick()
    {
      if (selectedRuneListItem == null)
      {
        Debug.LogWarning("没有选中的符文图标");
        return;
      }

      RuneSlot runeSlot = selectedRuneListItem.GetComponent<RuneSlot>();
      if (runeSlot == null)
      {
        Debug.LogWarning("选中的符文图标没有 RuneSlot 组件");
        return;
      }
      Debug.Log($"装备符文: {runeSlot.RuneName}");
      if (
          RuneIcon_1.transform.Find("Icon").GetComponent<Image>().sprite
              != selectedRuneListItem.transform.Find("Icon").GetComponent<Image>().sprite
          && RuneIcon_2.transform.Find("Icon").GetComponent<Image>().sprite
              != selectedRuneListItem.transform.Find("Icon").GetComponent<Image>().sprite
      )
      {
        selectedRuneIcon.transform.Find("Icon").GetComponent<Image>().sprite =
            selectedRuneListItem.transform.Find("Icon").GetComponent<Image>().sprite;
        //记录当前符文id
        if (selectedRuneIcon == RuneIcon_1)
        {
          currentEquippedRuneIds[0] = runeSlot.slotId;
        }
        else if (selectedRuneIcon == RuneIcon_2)
        {
          currentEquippedRuneIds[1] = runeSlot.slotId;
        }
      }
      else
      {
        Debug.LogWarning("符文已装备在另一个插槽上了");
        return;
      }
    }

    private bool CanDeselectRuneIcons()
    {
      return RunePanel == null || !RunePanel.activeSelf;
    }

    private void StartButtonOnClick()
    {
      Debug.Log($"[LobbyUIManager] 点击就绪按钮: IsStayLobby={EventChannelLocator.MainContainer.gameSettings.IsStayLobby}, isReady={isReady}");
      if (!EventChannelLocator.MainContainer.gameSettings.IsStayLobby || isReady)
      {
        Debug.LogWarning($"[LobbyUIManager] 阻止就绪: IsStayLobby={EventChannelLocator.MainContainer.gameSettings.IsStayLobby}, isReady={isReady}");
        return;
      }
      EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.SyncAllPlayers);
      isReady = true;
      startButton.GetComponent<Button>().interactable = false;
      Text buttonText = startButton.GetComponentInChildren<Text>();
      if (buttonText != null)
      {
        buttonText.text = "已就绪";
      }

      var launcher = FindObjectOfType<Infrastructure.Network.Launcher>();
      Debug.Log($"[LobbyUIManager] Launcher 查找结果: {launcher}");
      if (launcher != null)
      {
        EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.SetLocalReady);
      }
    }

    private void LobbyButtonOnClick()
    {
      SetButtonSelected(lobbyButton);
      PostProcessVolume.weight = 0f; // 调整后处理效果的权重，例如模糊效果
      HideCharactorPanel();
      HideRunePanel();
    }

    private void CharacterButtonOnClick()
    {
      HideRunePanel();
      SetButtonSelected(characterButton); // 调整后处理效果的权重，例如模糊效果
      ShowCharactorPanel();
    }

    private void RuneButtonOnClick()
    {
      SetButtonSelected(runeButton);
      HideCharactorPanel();
      ShowRunePanel();
    }

    private void Rune_1ButtonOnClick()
    {
      HideCharactorPanel();
      ShowRunePanel();
      SetRuneIconSelected(RuneIcon_1);
    }

    private void Rune_2ButtonOnClick()
    {
      HideCharactorPanel();
      ShowRunePanel();
      SetRuneIconSelected(RuneIcon_2);
    }

    private void ShowRunePanel()
    {
      if (RunePanel == null)
        return;
      if (RunePanel.activeSelf)
        return;
      Lobby.GameManager.isOpenUI = true;
      SetRuneIconSelected(RuneIcon_1);
      PostProcessVolume.weight = 1f;
      RunePanel.SetActive(true);
      RectTransform panelRect = RunePanel.GetComponent<RectTransform>();
      if (panelRect == null)
        return;

      panelRect.DOKill();
      panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, Screen.height);
      panelRect.DOAnchorPosY(-185.53f, 0.6f).SetEase(Ease.OutBack);
    }

    private void HideRunePanel()
    {
      DeselectRuneIcons();

      if (RunePanel == null)
        return;

      RectTransform panelRect = RunePanel.GetComponent<RectTransform>();
      if (panelRect == null)
      {
        RunePanel.SetActive(false);
        return;
      }
      Lobby.GameManager.isOpenUI = false;
      PostProcessVolume.weight = 0f;
      panelRect.DOKill();
      panelRect
          .DOAnchorPosY(Screen.height, 0.3f)
          .SetEase(Ease.InBack)
          .OnComplete(() => RunePanel.SetActive(false));
    }

    private void ShowCharactorPanel()
    {
      if (CharactorPanel == null)
        return;
      PostProcessVolume.weight = 1f;
      CharactorPanel.SetActive(true);
      RectTransform panelRect = CharactorPanel.GetComponent<RectTransform>();
      if (panelRect == null)
        return;
      Lobby.GameManager.isOpenUI = true;
      EventChannelLocator.MainContainer.changeCanRotateChannel.Raise(true);
      panelRect.DOKill();
      panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, -Screen.height);
      panelRect.DOAnchorPosY(-180.34f, 0.6f).SetEase(Ease.OutBack);
    }

    private void HideCharactorPanel()
    {
      if (CharactorPanel == null)
        return;

      RectTransform panelRect = CharactorPanel.GetComponent<RectTransform>();
      if (panelRect == null)
      {
        CharactorPanel.SetActive(false);
        return;
      }
      EventChannelLocator.MainContainer.changeCanRotateChannel.Raise(false);
      Lobby.GameManager.isOpenUI = false;
      PostProcessVolume.weight = 0f;
      panelRect.DOKill();
      panelRect
          .DOAnchorPosY(-Screen.height, 0.3f)
          .SetEase(Ease.InBack)
          .OnComplete(() => CharactorPanel.SetActive(false));
    }

    //设置按钮为选中状态，其他按钮为正常
    public void SetButtonSelected(GameObject button)
    {
      EventSystem.current.SetSelectedGameObject(button.gameObject);
      lobbyButton.GetComponent<Button>().interactable = button != lobbyButton;
      characterButton.GetComponent<Button>().interactable = button != characterButton;
      runeButton.GetComponent<Button>().interactable = button != runeButton;
      MassionButton.GetComponent<Button>().interactable = button != MassionButton;
      //设置图片
      lobbyButton.GetComponent<Image>().sprite =
          button == lobbyButton ? selectedButtonImage : defaultButtonImage;
      characterButton.GetComponent<Image>().sprite =
          button == characterButton ? selectedButtonImage : defaultButtonImage;
      runeButton.GetComponent<Image>().sprite =
          button == runeButton ? selectedButtonImage : defaultButtonImage;
      MassionButton.GetComponent<Image>().sprite =
          button == MassionButton ? selectedButtonImage : defaultButtonImage;
    }

    //设置RuneIcon为选中状态
    private void SetRuneIconSelected(GameObject button)
    {
      selectedRuneIcon = button;
      RuneIcon_1.GetComponent<Image>().sprite =
          (button == RuneIcon_1) ? selectedButtonImage : defaultButtonImage;
      RuneIcon_2.GetComponent<Image>().sprite =
          (button == RuneIcon_2) ? selectedButtonImage : defaultButtonImage;

      RuneIcon_1.transform.DOScale(button == RuneIcon_1 ? 1.1f : 1f, 0.2f);
      RuneIcon_2.transform.DOScale(button == RuneIcon_2 ? 1.1f : 1f, 0.2f);
    }

    private void DeselectRuneIcons()
    {
      if (selectedRuneIcon == null)
        return;

      selectedRuneIcon = null;
      RuneIcon_1.GetComponent<Image>().sprite = defaultButtonImage;
      RuneIcon_2.GetComponent<Image>().sprite = defaultButtonImage;
      RuneIcon_1.transform.DOScale(1f, 0.2f);
      RuneIcon_2.transform.DOScale(1f, 0.2f);
    }

    private void OnRuneListItemClicked(GameObject runeIcon)
    {
      SetRuneListSelected(runeIcon);
    }

    private void SetRuneListSelected(GameObject button)
    {
      selectedRuneListItem = button;
      foreach (GameObject runeIcon in runeList)
      {
        if (runeIcon == null)
        {
          Debug.LogWarning("符文列表中有空引用");
          continue;
        }
        bool selected = runeIcon == button;
        Debug.Log(
            $"设置符文图标 '{runeIcon.name}' 的选中状态: {(selected ? "选中" : "未选中")}"
        );
        runeIcon.transform.DOScale(selected ? 1.1f : 1f, 0.2f);
      }
      //添加点击符文图标后的逻辑显示符文详情
      RuneSlot runeSlot = button.GetComponent<RuneSlot>();
      if (runeSlot != null)
      {
        var runeArgs = new Domain.Event.RuneEquipArgs(
            runeSlot.slotId,
            runeSlot.RuneName,
            runeSlot.runePowers,
            runeSlot.specialPowerName,
            runeSlot.specialPowerDescription
        );
        EventChannelLocator.MainContainer.runeInfoChannel.Raise(runeArgs);
      }
    }

    private void DeselectRuneListItems()
    {
      if (selectedRuneListItem == null)
        return;

      selectedRuneListItem = null;
      foreach (GameObject runeIcon in runeList)
      {
        if (runeIcon == null)
          continue;

        runeIcon.GetComponent<Image>().sprite = defaultButtonImage;
        runeIcon.transform.DOScale(1f, 0.2f);
      }
    }

    private bool IsPointerOverRuneIcon()
    {
      if (EventSystem.current == null)
        return false;

      PointerEventData pointerData = new PointerEventData(EventSystem.current)
      {
        position = Input.mousePosition,
      };

      List<RaycastResult> results = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pointerData, results);
      foreach (RaycastResult result in results)
      {
        if (
            result.gameObject == RuneIcon_1.gameObject
            || result.gameObject == RuneIcon_2.gameObject
            || result.gameObject.transform.IsChildOf(RuneIcon_1.transform)
            || result.gameObject.transform.IsChildOf(RuneIcon_2.transform)
        )
        {
          return true;
        }
      }

      return false;
    }

    //添加RuneIcon的鼠标悬停动画
    private void AddRuneIconHoverAnimation(GameObject button)
    {
      EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

      // PointerEnter 事件：变大
      EventTrigger.Entry entryEnter = new EventTrigger.Entry();
      entryEnter.eventID = EventTriggerType.PointerEnter;
      entryEnter.callback.AddListener(
          (data) =>
          {
            button.transform.DOScale(1.1f, 0.2f);
          }
      );
      trigger.triggers.Add(entryEnter);

      // PointerExit 事件：如果未选中，则变回原样
      EventTrigger.Entry entryExit = new EventTrigger.Entry();
      entryExit.eventID = EventTriggerType.PointerExit;
      entryExit.callback.AddListener(
          (data) =>
          {
            if (selectedRuneIcon != button)
              button.transform.DOScale(1f, 0.2f);
          }
      );
      trigger.triggers.Add(entryExit);
    }

    #region  UI切换角色相关方法区域
    //切换角色
    private void SwitchCharactor(int charactorIndex)
    {
      GameObject prefab = Resources.Load<GameObject>("UI/Model/" + charactorIndex);
      if (prefab == null)
      {
        Debug.LogWarning($"未找到角色模型资源: UI/Model/{charactorIndex}");
        return;
      }
      if (currentCharactorInstance != null)
      {
        Destroy(currentCharactorInstance);
        currentCharactorInstance = null;
      }
      currentCharactorIndex = charactorIndex;
      currentCharactorInstance = Instantiate(prefab, CharactorShowPosition.transform);
      currentCharactorInstance.transform.localPosition = Vector3.zero;
    }

    private void SwitchCharactorButtonClicked()
    {
      switch (currentCharactorIndex)
      {
        case 0: // CharactorIndex.WiZardBoy
          EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.SwitchCharacter);
          eventName = CharacterCardType.WizardBoy;
          break;
        case 1: // CharactorIndex.LittleRedGirl
                // EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.SwitchCharacter);
          break;
        default:
          break;
      }
    }
    #endregion
  }
}