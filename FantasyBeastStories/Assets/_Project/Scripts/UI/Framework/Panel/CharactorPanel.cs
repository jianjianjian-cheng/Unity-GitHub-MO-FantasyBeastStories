using System.Collections;
using System.Collections.Generic;
using Managers;
using Controllers.Character;
using Core;
using Controllers.Network;
using UI.Framework.Base;
using UI.Framework.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharactorPanel : UIScreen
{
  //角色选择面板
  [SerializeField]
  private GameObject nextCharacterButton; //下一个角色按钮
  [SerializeField]
  private GameObject previousCharacterButton; //上一个角色按钮
  [SerializeField]
  private GameObject SwitchButton; //切换角色按钮
  [SerializeField]
  private GameObject CharactorShowPosition; //当前角色位置对象
  private int currentCharactorIndex = 0; //当前角色索引
  private GameObject currentCharactorInstance; //当前角色实例

  [Tooltip("角色信息相关")]
  [SerializeField] private GameObject infoItemParent; //角色信息项父对象
  [SerializeField] private TextMeshProUGUI characterNameText; //角色名称文本
  [SerializeField] private Image characterIconImage; //角色图标
  [Header("角色信息库")]
  [SerializeField] private CharacterInfoLibrarySO characterInfoLibrary;
  [SerializeField] private GameObject charactorInfoItemPrefab; //角色信息项预制体

  private bool _buttonsInitialized = false; // 按钮是否已初始化

  protected override void Awake()
  {
    screenId = "CharactorPanel";
    Debug.Log($"[CharactorPanel] Awake: screenId = {screenId}");
    base.Awake();
    // 必须在 Awake 中注册，因为 base.Awake() 会 SetActive(false)，
    // 导致 Start() 永远不会被调用（非激活对象不执行 Start）
    Debug.Log("[CharactorPanel] Awake: 正在注册到 UIManager...");
    UIManager.Instance.RegisterScreen(this);
    Debug.Log("[CharactorPanel] Awake: 注册完成");
    // 注意：不再在 Awake 中初始化角色预览，
    // 因为此时 SaveManager 尚未加载存档，SelectedCharacterIndex 始终为默认值 0。
    // 改为在 OnBeforeOpen（每次打开面板时）延迟初始化，确保存档已加载。
  }

  /// <summary>
  /// 初始化按钮事件（仅执行一次）
  /// </summary>
  private void InitializeButtons()
  {
    if (_buttonsInitialized)
      return;

    if (previousCharacterButton == null || nextCharacterButton == null || SwitchButton == null)
    {
      Debug.LogError("[CharactorPanel] 角色选择按钮未找到，请检查场景中是否存在 nextCharacterButton, previousCharacterButton, SwitchButton");
      return;
    }

    // 翻转 previousCharacterButton 的图片（向左箭头）
    Image previousCharacterButtonImage = previousCharacterButton.GetComponent<Image>();
    Vector3 scale = previousCharacterButtonImage.transform.localScale;
    scale.x *= -1;
    previousCharacterButtonImage.transform.localScale = scale;

    // 添加上一个角色按钮的点击事件
    previousCharacterButton
        .GetComponent<Button>()
        .onClick.AddListener(() =>
        {
          int newIndex = currentCharactorIndex - 1;
          if (newIndex < 0)
            newIndex = 0;
          SwitchCharactor(newIndex);
        });

    // 添加下一个角色按钮的点击事件
    nextCharacterButton
        .GetComponent<Button>()
        .onClick.AddListener(() =>
        {
          int newIndex = currentCharactorIndex + 1;
          if (newIndex >= 2)
            newIndex = 1;
          SwitchCharactor(newIndex);
        });

    // 添加切换按钮的点击事件
    SwitchButton
        .GetComponent<Button>()
        .onClick.AddListener(() =>
        {
          SwitchCharactorButtonClicked();
        });

    _buttonsInitialized = true;
  }

  protected override void OnBeforeOpen()
  {
    base.OnBeforeOpen();
    Debug.Log($"[CharactorPanel] OnBeforeOpen: 面板即将打开, IsOpen={IsOpen}");

    // 首次打开时初始化按钮事件
    InitializeButtons();

    // 每次打开面板时，从已加载的存档同步角色索引，确保预览与存档一致
    // 此时 SaveManager.Start() 早已执行完毕，存档已加载，能读到正确的值
    int savedIndex = SaveManager.Instance != null
        ? SaveManager.SelectedCharacterIndex
        : 0;
    SwitchCharactor(savedIndex);

    // 重置预览角色旋转角度，避免上次旋转残留
    if (currentCharactorInstance != null)
      currentCharactorInstance.transform.rotation = Quaternion.identity;

    EventChannelLocator.MainContainer.changeCanRotateChannel.Raise(true);
  }

  protected override void OnAfterClose()
  {
    base.OnAfterClose();
    Debug.Log($"[CharactorPanel] OnAfterClose: 面板已关闭, IsOpen={IsOpen}");
    EventChannelLocator.MainContainer.changeCanRotateChannel.Raise(false);
  }

  #region UI切换角色相关方法区域
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

    // 同步更新角色信息展示
    RefreshCharacterInfo(charactorIndex);
  }

  /// <summary>
  /// 根据角色索引刷新名称、图标、能力介绍列表
  /// </summary>
  private void RefreshCharacterInfo(int charactorIndex)
  {
    if (characterInfoLibrary == null)
      return;

    CharacterInfoSO info = characterInfoLibrary.GetInfo(charactorIndex);
    if (info == null)
      return;

    // 名称
    if (characterNameText != null)
      characterNameText.text = info.characterName;

    // 图标
    if (characterIconImage != null)
      characterIconImage.sprite = info.characterIcon;

    // 能力介绍列表：先清除旧的
    if (infoItemParent == null || charactorInfoItemPrefab == null)
      return;

    for (int i = infoItemParent.transform.childCount - 1; i >= 0; i--)
    {
      Transform child = infoItemParent.transform.GetChild(i);
      Destroy(child.gameObject);
    }

    // 为每条描述创建新的 Item
    foreach (string desc in info.abilityDescriptions)
    {
      GameObject itemObj = Instantiate(charactorInfoItemPrefab, infoItemParent.transform);
      CharactorInfoItem item = itemObj.GetComponent<CharactorInfoItem>();
      if (item != null)
        item.SetContent(desc);
    }
  }

  private void SwitchCharactorButtonClicked()
  {
    // 保存到存档，确保下次打开面板时同步
    SaveManager.SelectedCharacterIndex = currentCharactorIndex;

    // 立即持久化到本地存档，确保进入大厅时 LoadGame 读到最新值
    if (SaveManager.Instance != null)
      SaveManager.Instance.SaveGame();

    switch (currentCharactorIndex)
    {
      case CharactorIndex.WiZardBoy:
        Launcher.instance.SwitchCharacter(CharactorName.WiZardBoy);
        break;
      case CharactorIndex.BingNv:
        Launcher.instance.SwitchCharacter(CharactorName.BingNv);
        break;
      default:
        Debug.LogWarning($"[CharactorPanel] 未知角色索引: {currentCharactorIndex}");
        break;
    }
  }
  #endregion
}