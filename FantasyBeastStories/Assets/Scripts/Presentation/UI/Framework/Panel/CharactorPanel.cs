using System.Collections;
using System.Collections.Generic;
using Domain.CardData;
using Domain.Event;
using Domain.Event.Channels.General;
using Domain.Services;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
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

  private string eventName = CharacterCardType.WizardBoy;

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
    InitializeCharacterPanel();
    Debug.Log("[CharactorPanel] Awake: 初始化完成");
  }

  private void InitializeCharacterPanel()
  {

    if (previousCharacterButton == null || nextCharacterButton == null || SwitchButton == null)
    {
      Debug.LogError("[CharactorPanel] 角色选择按钮未找到，请检查场景中是否存在 nextCharacterButton, previousCharacterButton, SwitchButton");
      return;
    }

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
  }

  protected override void OnBeforeOpen()
  {
    base.OnBeforeOpen();
    Debug.Log($"[CharactorPanel] OnBeforeOpen: 面板即将打开, IsOpen={IsOpen}");
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