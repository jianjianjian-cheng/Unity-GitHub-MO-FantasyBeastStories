using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Controllers.CardData;
using Controllers.Character;
using Controllers.Player;
using DG.Tweening;
using Core;
using Core.Channels.General;
using Core.Channels.Player;
using Core.Contracts;
using Core.Network;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Core.Audio;


namespace UI
{
  public class MagicUpgradeManager : MonoBehaviour
  {
    #region 单例模式
    public static MagicUpgradeManager instance;

    void Awake()
    {
      if (instance == null)
      {
        instance = this;
      }
      else
      {
        Destroy(gameObject);
      }
      SetCurrentEventName(eventName);
    }
    #endregion



    [Header("事件名称配置")]
    [SerializeField]
    private string eventName = CharacterCardType.WizardBoy;
    #region UI对象
    List<GameObject> CardEffects;
    GameObject GrossUpgradePanel;
    GameObject Card_1Effect;
    ParticleSystem[] effects_1 = new ParticleSystem[3];
    List<ParticleSystem> OneCardEffects;
    GameObject Card_2Effect;
    ParticleSystem[] effects_2 = new ParticleSystem[3];
    List<ParticleSystem> TwoCardEffects;
    GameObject Card_3Effect;
    ParticleSystem[] effects_3 = new ParticleSystem[3];
    List<ParticleSystem> ThreeCardEffects;

    //事件捕获层（透明Image覆盖层，确保鼠标事件不被子对象拦截）
    GameObject Card_1Catcher;
    GameObject Card_2Catcher;
    GameObject Card_3Catcher;

    //魔法升级面板，以及面板下卡片的文字变量
    GameObject MagicUpgradePanel;
    GameObject Card_1;
    TextMeshProUGUI NameText_1;
    TextMeshProUGUI ContentText_1;
    TextMeshProUGUI QualityText_1;
    GameObject Card_2;
    TextMeshProUGUI NameText_2;
    TextMeshProUGUI ContentText_2;
    TextMeshProUGUI QualityText_2;
    GameObject Card_3;
    TextMeshProUGUI NameText_3;
    TextMeshProUGUI ContentText_3;
    TextMeshProUGUI QualityText_3;
    private List<GameObject> Cards;

    //背景阴影
    private Image shadowImage;

    //卡片动画相关位置
    private GameObject AnimPoints;
    private Transform StartPoint;
    private Transform[] EndPoints;

    //保存特效原始Scale
    private Dictionary<ParticleSystem, Vector3> originalEffectScales =
        new Dictionary<ParticleSystem, Vector3>();

    // 初始化完成标志
    private bool isInitialized = false;
    #endregion

    // 是否已确认
    private bool isConfirmed = false;
    // 防止面板重复打开
    private bool _isPanelActive = false;

    private const string PLAYER_UPGRADE_READY_KEY = "UpgradeReady";

    public bool isAllExCard = false;

    //当前选择的角色事件名，用于触发事件
    private string currentEventName;
    private CardConfigSO[] cardData;

    void Start()
    {
      CardEffects = new List<GameObject>();
      Cards = new List<GameObject>();
      OneCardEffects = new List<ParticleSystem>();
      TwoCardEffects = new List<ParticleSystem>();
      ThreeCardEffects = new List<ParticleSystem>();
      Initialize();
    }

    void Update() { }

    private void Initialize()
    {
      if (isInitialized)
        return;

      GrossUpgradePanel = transform.Find("GrossUpgradePanel")?.gameObject;
      if (GrossUpgradePanel == null)
      {
        Debug.LogError("GrossUpgradePanel 未找到");
        return;
      }

      #region 寻找卡片
      MagicUpgradePanel = GrossUpgradePanel.transform.Find("MagicUpgradePanel")?.gameObject;
      if (MagicUpgradePanel == null)
      {
        Debug.LogError("MagicUpgradePanel 未找到");
        return;
      }

      // 批量查找卡片
      var card1Transform = MagicUpgradePanel.transform.Find("Card_1");
      var card2Transform = MagicUpgradePanel.transform.Find("Card_2");
      var card3Transform = MagicUpgradePanel.transform.Find("Card_3");

      if (card1Transform == null || card2Transform == null || card3Transform == null)
      {
        Debug.LogError("卡片未找到");
        return;
      }

      Card_1 = card1Transform.gameObject;
      Card_2 = card2Transform.gameObject;
      Card_3 = card3Transform.gameObject;

      Cards.Add(Card_1);
      Cards.Add(Card_2);
      Cards.Add(Card_3);

      // 批量获取文字组件
      NameText_1 = card1Transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
      ContentText_1 = card1Transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
      QualityText_1 = card1Transform.Find("QualityText")?.GetComponent<TextMeshProUGUI>();

      NameText_2 = card2Transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
      ContentText_2 = card2Transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
      QualityText_2 = card2Transform.Find("QualityText")?.GetComponent<TextMeshProUGUI>();

      NameText_3 = card3Transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
      ContentText_3 = card3Transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
      QualityText_3 = card3Transform.Find("QualityText")?.GetComponent<TextMeshProUGUI>();

      if (
          NameText_1 == null
          || ContentText_1 == null
          || QualityText_1 == null
          || NameText_2 == null
          || ContentText_2 == null
          || QualityText_2 == null
          || NameText_3 == null
          || ContentText_3 == null
          || QualityText_3 == null
      )
      {
        Debug.LogError("文字组件未找到");
        return;
      }
      #endregion

      #region 寻找特效
      // 批量查找特效
      var oneCardEffect = MagicUpgradePanel.transform.Find("OneCardEffect");
      var twoCardEffect = MagicUpgradePanel.transform.Find("TwoCardEffect");
      var threeCardEffect = MagicUpgradePanel.transform.Find("ThreeCardEffect");

      if (oneCardEffect == null || twoCardEffect == null || threeCardEffect == null)
      {
        Debug.LogError("特效未找到");
        return;
      }

      Card_1Effect = oneCardEffect.gameObject;
      Card_2Effect = twoCardEffect.gameObject;
      Card_3Effect = threeCardEffect.gameObject;

      CardEffects.Add(Card_1Effect);
      CardEffects.Add(Card_2Effect);
      CardEffects.Add(Card_3Effect);

      // 缓存特效组件
      effects_1 = oneCardEffect.GetComponentsInChildren<ParticleSystem>();
      effects_2 = twoCardEffect.GetComponentsInChildren<ParticleSystem>();
      effects_3 = threeCardEffect.GetComponentsInChildren<ParticleSystem>();

      foreach (ParticleSystem ps in effects_1)
        OneCardEffects.Add(ps);
      foreach (ParticleSystem ps in effects_2)
        TwoCardEffects.Add(ps);
      foreach (ParticleSystem ps in effects_3)
        ThreeCardEffects.Add(ps);
      #endregion

      #region 其他元素
      // 查找阴影
      var shadowTransform = GrossUpgradePanel.transform.Find("Shadow");
      if (shadowTransform != null)
      {
        shadowImage = shadowTransform.GetComponent<Image>();
      }

      // 查找动画点
      AnimPoints = GrossUpgradePanel.transform.Find("AnimPoints")?.gameObject;
      if (AnimPoints != null)
      {
        StartPoint = AnimPoints.transform.Find("StartPoint");
        EndPoints = AnimPoints
            .GetComponentsInChildren<Transform>()
            .Where(x => x.name == "EndPoint")
            .ToArray();
      }
      #endregion

      //保存所有特效的原始Scale
      SaveOriginalScales();

      //为每张卡片创建事件捕获层
      Card_1Catcher = CreateEventCatcher(Card_1);
      Card_2Catcher = CreateEventCatcher(Card_2);
      Card_3Catcher = CreateEventCatcher(Card_3);

      isInitialized = true;
    }

    // 为卡片创建透明的事件捕获层
    private GameObject CreateEventCatcher(GameObject card)
    {
      if (card == null)
        return null;

      // 检查是否已存在
      var existingCatcher = card.transform.Find("EventCatcher");
      if (existingCatcher != null)
        return existingCatcher.gameObject;

      GameObject catcher = new GameObject("EventCatcher");
      catcher.transform.SetParent(card.transform, false);
      catcher.transform.SetAsLastSibling();

      Image img = catcher.AddComponent<Image>();
      img.color = Color.clear;
      img.raycastTarget = true;

      RectTransform rt = catcher.GetComponent<RectTransform>();
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.one;
      rt.offsetMin = Vector2.zero;
      rt.offsetMax = Vector2.zero;

      return catcher;
    }

    //保存所有特效的原始Scale
    private void SaveOriginalScales()
    {
      originalEffectScales.Clear();
      SaveEffectScales(OneCardEffects);
      SaveEffectScales(TwoCardEffects);
      SaveEffectScales(ThreeCardEffects);
    }

    private void SaveEffectScales(List<ParticleSystem> effects)
    {
      foreach (var effect in effects)
      {
        if (effect != null && !originalEffectScales.ContainsKey(effect))
        {
          originalEffectScales[effect] = effect.transform.localScale;
        }
      }
    }

    #region 鼠标悬停处理

    // 为卡片添加鼠标悬停事件
    private void RegisterCardHoverEvents()
    {
      AddHoverEffect(Card_1Catcher, Card_1, 1.2f, 0.2f, OneCardEffects);
      AddHoverEffect(Card_2Catcher, Card_2, 1.2f, 0.2f, TwoCardEffects);
      AddHoverEffect(Card_3Catcher, Card_3, 1.2f, 0.2f, ThreeCardEffects);
    }

    private void AddHoverEffect(
        GameObject catcher,
        GameObject card,
        float scaleMultiplier,
        float duration,
        List<ParticleSystem> effectList
    )
    {
      if (catcher == null)
        return;

      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger == null)
        trigger = catcher.AddComponent<EventTrigger>();

      // 清除现有事件
      trigger.triggers.Clear();

      // 鼠标进入事件
      EventTrigger.Entry enterEntry = new EventTrigger.Entry();
      enterEntry.eventID = EventTriggerType.PointerEnter;
      enterEntry.callback.AddListener(
          (data) => OnCardPointerEnter(card, scaleMultiplier, duration, effectList)
      );
      trigger.triggers.Add(enterEntry);

      // 鼠标退出事件
      EventTrigger.Entry exitEntry = new EventTrigger.Entry();
      exitEntry.eventID = EventTriggerType.PointerExit;
      exitEntry.callback.AddListener((data) => OnCardPointerExit(card, duration, effectList));
      trigger.triggers.Add(exitEntry);
    }

    private void OnCardPointerEnter(
        GameObject card,
        float scaleMultiplier,
        float duration,
        List<ParticleSystem> effectList
    )
    {
      if (card == null)
        return;

      AudioManager.Instance.PlayUI("sfx_card_deal");

      card.transform.DOKill();
      card.transform.DOScale(scaleMultiplier, duration).SetEase(Ease.OutBack);

      if (effectList != null)
      {
        foreach (var effect in effectList)
        {
          if (effect != null && originalEffectScales.ContainsKey(effect))
          {
            effect.transform.DOKill();
            Vector3 targetScale = originalEffectScales[effect] * scaleMultiplier;
            effect.transform.DOScale(targetScale, duration).SetEase(Ease.OutBack);
          }
        }
      }
    }

    private void OnCardPointerExit(
        GameObject card,
        float duration,
        List<ParticleSystem> effectList
    )
    {
      if (card == null)
        return;

      card.transform.DOKill();
      card.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);

      if (effectList != null)
      {
        foreach (var effect in effectList)
        {
          if (effect != null && originalEffectScales.ContainsKey(effect))
          {
            effect.transform.DOKill();
            effect
                .transform.DOScale(originalEffectScales[effect], duration)
                .SetEase(Ease.OutBack);
          }
        }
      }
    }

    #endregion

    #region 卡片点击事件
    private void AddOnClickEvent(GameObject catcher, GameObject card)
    {
      if (catcher == null)
        return;

      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger == null)
        trigger = catcher.AddComponent<EventTrigger>();

      // 点击事件
      EventTrigger.Entry clickEntry = new EventTrigger.Entry();
      clickEntry.eventID = EventTriggerType.PointerClick;
      clickEntry.callback.AddListener((data) => OnCardPointerClick(card, catcher));
      trigger.triggers.Add(clickEntry);
    }

    private void OnCardPointerClick(GameObject selectedCard, GameObject catcher)
    {
      Debug.Log("点击了卡片");
      AudioManager.Instance.PlayUI("sfx_card_select");
      PlayClickFeedback(selectedCard);
      string cardName = selectedCard.gameObject.name;
      int index = 0;
      switch (cardName)
      {
        case "Card_1":
          index = 0;
          break;
        case "Card_2":
          index = 1;
          break;
        case "Card_3":
          index = 2;
          break;
      }
      Debug.LogWarning("开始准备触发卡牌事件");
      EventChannelLocator.MainContainer.cardReceivedChannel.Raise(cardData[index]);

      if (selectedCard != null && catcher != null)
      {
        StartCoroutine(EndMoveCard(selectedCard, catcher, 1f));
      }
    }

    /// <summary>
    /// 播放卡片点击反馈动画：先缩小再放大（含特效同步变化）
    /// </summary>
    private void PlayClickFeedback(GameObject card)
    {
      if (card == null) return;

      List<ParticleSystem> effectList = GetCardEffectList(card);
      card.transform.DOKill();
      if (effectList != null)
        foreach (var effect in effectList)
          if (effect != null) effect.transform.DOKill();

      Sequence clickSeq = DOTween.Sequence();

      // 快速缩小
      clickSeq.Append(card.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));

      // 恢复原始缩放
      clickSeq.Append(card.transform.DOScale(1.2f, 0.1f));

      // 同步特效振动
      if (effectList != null)
      {
        foreach (var effect in effectList)
        {
          if (effect != null && originalEffectScales.ContainsKey(effect))
          {
            Vector3 originalScale = originalEffectScales[effect];
            Sequence effectSeq = DOTween.Sequence();
            effectSeq.Append(effect.transform.DOScale(originalScale * 0.9f, 0.1f).SetEase(Ease.InOutQuad));
            effectSeq.Append(effect.transform.DOScale(originalScale * 1.2f, 0.1f));
          }
        }
      }
    }

    /// <summary>
    /// 获取卡片对应的特效列表
    /// </summary>
    private List<ParticleSystem> GetCardEffectList(GameObject card)
    {
      if (card == null)
        return null;

      switch (card.name)
      {
        case "Card_1":
          return OneCardEffects;
        case "Card_2":
          return TwoCardEffects;
        case "Card_3":
          return ThreeCardEffects;
        default:
          return null;
      }
    }

    //点击结束的时候移动卡片
    private IEnumerator EndMoveCard(GameObject card, GameObject catcher, float rotationSpeed)
    {
      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger == null)
      {
        yield break;
      }
      trigger.enabled = false;
      Sequence seq = DOTween.Sequence();
      float moveTime = 0.3f;

      foreach (GameObject Card in Cards)
      {
        if (Card != card)
        {
          seq.Append(Card.transform.DOMove(StartPoint.position, moveTime));
          seq.Join(Card.transform.DOScale(Card.transform.localScale * 0.1f, moveTime));
          GameObject effect = GetEffect(Card);
          if (effect != null)
          {
            seq.Join(
                effect.transform.DOMove(
                    new Vector3(
                        StartPoint.transform.position.x,
                        StartPoint.transform.position.y,
                        effect.transform.position.z
                    ),
                    moveTime
                )
            );
            seq.Join(
                effect.transform.DOScale(effect.transform.localScale * 0.1f, moveTime)
            );
          }
        }
        else
        {
          StartCoroutine(DelayMoveCard(Card, 0.4f));
        }
      }
      yield return seq.WaitForCompletion();
    }

    IEnumerator DelayMoveCard(GameObject card, float duration)
    {
      yield return new WaitForSeconds(duration);
      // card.transform.DOMove(EndPoints[1].position, duration);
      // GameObject effect = GetEffect(card);
      // if (effect != null)
      // {
      //     effect.transform.DOMove(
      //         new Vector3(
      //             EndPoints[1].transform.position.x,
      //             EndPoints[1].transform.position.y,
      //             effect.transform.position.z
      //         ),
      //         duration
      //     );
      //     effect.transform.DOScale(effect.transform.localScale * 0.1f, duration);
      // }

      yield return new WaitForSeconds(1f);
      OnCardSelectionComplete();
    }

    private void OnCardSelectionComplete()
    {
      if (isConfirmed)
      {
        return;
      }
      isConfirmed = true;
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        CloseMagicUpgradePanel();
        return;
      }
      NetworkServiceLocator.PlayerService.SetCustomProperty(PLAYER_UPGRADE_READY_KEY, true);
    }

    //获取卡牌对应特效
    private GameObject GetEffect(GameObject card)
    {
      if (card == null)
        return null;

      string cardName = card.name;
      switch (cardName)
      {
        case "Card_1":
          return Card_1Effect;
        case "Card_2":
          return Card_2Effect;
        case "Card_3":
          return Card_3Effect;
        default:
          return null;
      }
    }

    #endregion

    #region 动画处理
    private void OpenMagicUpgradePanelAnim()
    {
      StartCoroutine(AnimateShadowAlpha());
    }

    IEnumerator MoveCardEndToPosition()
    {
      Sequence seq = DOTween.Sequence();
      float moveTime = 0.3f;

      // ===== 第1步：发第1张牌 =====
      seq.AppendCallback(() => AudioManager.Instance.PlayUI("sfx_card_deal"));
      seq.Append(Card_1.transform.DOMove(EndPoints[0].position, moveTime));
      seq.Join(
          Card_1Effect.transform.DOMove(
              new Vector3(
                  EndPoints[0].position.x,
                  EndPoints[0].position.y,
                  Card_1Effect.transform.position.z
              ),
              moveTime
          )
      );

      // ===== 第2步：发第2张牌 =====
      seq.AppendCallback(() => AudioManager.Instance.PlayUI("sfx_card_deal"));
      seq.Append(Card_2.transform.DOMove(EndPoints[1].position, moveTime));
      seq.Join(
          Card_2Effect.transform.DOMove(
              new Vector3(
                  EndPoints[1].position.x,
                  EndPoints[1].position.y,
                  Card_2Effect.transform.position.z
              ),
              moveTime
          )
      );

      // ===== 第3步：发第3张牌 =====
      seq.AppendCallback(() => AudioManager.Instance.PlayUI("sfx_card_deal"));
      seq.Append(Card_3.transform.DOMove(EndPoints[2].position, moveTime));
      seq.Join(
          Card_3Effect.transform.DOMove(
              new Vector3(
                  EndPoints[2].position.x,
                  EndPoints[2].position.y,
                  Card_3Effect.transform.position.z
              ),
              moveTime
          )
      );

      yield return seq.WaitForCompletion();
      RegisterCardHoverEvents();
      AddOnClickEvent(Card_1Catcher, Card_1);
      AddOnClickEvent(Card_2Catcher, Card_2);
      AddOnClickEvent(Card_3Catcher, Card_3);
      OnCardMoveToEndPositionComplete();
    }

    private void OnCardMoveToEndPositionComplete()
    {
      Debug.Log("卡片移动到end位置");
    }

    private void OnPlayerPropertyChanged(int actorNumber, string key, object value)
    {
      if (key != PLAYER_UPGRADE_READY_KEY || !NetworkServiceLocator.PlayerService.IsMasterClient)
      {
        return;
      }

      if (NetworkServiceLocator.PlayerService.AllPlayersHaveProperty(PLAYER_UPGRADE_READY_KEY, true))
      {
        EventChannelLocator.MainContainer.gameActionChannel.Raise(GameActionType.UpgradeAllConfirmed);
      }
    }

    private IEnumerator AnimateShadowAlpha()
    {
      float alpha = 0;
      while (alpha < 0.7f)
      {
        alpha += Time.deltaTime / 1;
        shadowImage.color = new Color(0, 0, 0, alpha);
        yield return null;
      }
      StartCoroutine(MoveCardEndToPosition());
    }

    private IEnumerator AnimateShadowAlphaBack()
    {
      float alpha = 0.7f;
      while (alpha > 0)
      {
        alpha -= Time.deltaTime / 1;
        shadowImage.color = new Color(0, 0, 0, alpha);
        yield return null;
      }
    }
    #endregion

    //重置卡片的大小和位置
    private void ResetCardState()
    {
      foreach (var card in Cards)
      {
        if (card == null)
          continue;

        card.transform.DOKill();
        card.transform.localScale = Vector3.one;
        card.transform.position = new Vector3(
            StartPoint.position.x,
            StartPoint.position.y,
            card.transform.position.z
        );
        ResetEffectScale(card);
        ResetEffectPosition(card);
      }
      ResetCatchers();
    }

    private void ResetEffectScale(GameObject card)
    {
      if (card == null)
        return;

      List<ParticleSystem> effectList = null;

      switch (card.name)
      {
        case "Card_1":
          effectList = OneCardEffects;
          break;
        case "Card_2":
          effectList = TwoCardEffects;
          break;
        case "Card_3":
          effectList = ThreeCardEffects;
          break;
      }

      if (effectList != null)
      {
        foreach (var effect in effectList)
        {
          if (effect != null && originalEffectScales.ContainsKey(effect))
          {
            effect.transform.DOKill();
            effect.transform.localScale = originalEffectScales[effect];
          }
        }
      }
    }

    private void ResetEffectPosition(GameObject card)
    {
      if (card == null)
        return;

      GameObject effect = GetEffect(card);
      if (effect != null)
      {
        effect.transform.DOKill();
        effect.transform.position = new Vector3(
            StartPoint.position.x,
            StartPoint.position.y,
            effect.transform.position.z
        );
      }
    }

    private void ResetCatchers()
    {
      ResetCatcher(Card_1Catcher);
      ResetCatcher(Card_2Catcher);
      ResetCatcher(Card_3Catcher);
    }

    private void ResetCatcher(GameObject catcher)
    {
      if (catcher == null)
        return;

      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger != null)
      {
        trigger.enabled = true;
        trigger.triggers.Clear();
      }
    }

    //打开魔法升级面板
    public void OpenMagicUpgradePanel()
    {
      if (_isPanelActive)
      {
        Debug.LogWarning("MagicUpgradePanel 已打开，忽略重复请求");
        return;
      }

      // 检查关键 UI 对象是否存活
      if (GrossUpgradePanel == null)
      {
        Debug.LogWarning("[MagicUpgradeManager] GrossUpgradePanel 已销毁，无法打开升级面板");
        _isPanelActive = false;
        return;
      }

      _isPanelActive = true;
      EventChannelLocator.MainContainer.bloomChannel.Raise(8f);

      Debug.Log("打开魔法升级面板");
      HideOrShowGrossUpgradePanel(false);
      // 确保初始化完成
      if (!isInitialized)
      {
        Initialize();
        if (!isInitialized)
          return;
      }

      cardData = null;
      // 先获取数据再重置状态
      cardData = GetCardData();

      //根据幸运值抽取角色专属卡牌，若抽到则随机替换一张公用卡牌，未抽到则无变化
      var luckQuery = new SkillQueryData(SkillQueryType.GetLuckRate);
      EventChannelLocator.MainContainer.skillQueryChannel.Raise(luckQuery);
      int luckRate = luckQuery.intValue;

      // 根据幸运值判断是否触发专属卡牌抽取
      float exCardChance = luckRate * 0.8f; // 幸运值越高，触发概率越大
      if (!isAllExCard)
      {
        if (Random.Range(0f, 100f) < exCardChance)
        {
          var exCardQuery = new SkillQueryData(SkillQueryType.GetRandomEXCard, currentEventName);
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(exCardQuery);
          CardConfigSO exCard = exCardQuery.cardResult;
          if (exCard != null && cardData != null)
          {
            // 随机替换一张公用卡牌
            int replaceIndex = Random.Range(0, cardData.Length);
            cardData[replaceIndex] = exCard;
            Debug.Log($"触发专属卡牌！替换了第{replaceIndex}张卡牌为: {exCard.cardName}");
          }
          else
          {
            Debug.LogWarning("没有可用的专属卡牌！");
          }
        }
      }
      isAllExCard = false;

      ResetCardState();
      GrossUpgradePanel.SetActive(true);
      OpenMagicUpgradePanelAnim();
      OpenPanelInit(cardData);
      EventChannelLocator.MainContainer.pauseChannel.Raise(true);
    }

    private void OpenPanelInit(CardConfigSO[] cardData)
    {
      isConfirmed = false;
      NetworkServiceLocator.PlayerService.SetCustomProperty(PLAYER_UPGRADE_READY_KEY, false);
      SetCardData(cardData);
    }

    //关闭魔法升级面板
    public void CloseMagicUpgradePanel()
    {
      _isPanelActive = false;
      EventChannelLocator.MainContainer?.bloomChannel?.Raise(5f);

      Debug.Log("关闭魔法升级面板");

      // 重置确认状态
      isConfirmed = false;

      // 重置网络属性
      if (NetworkServiceLocator.IsInitialized)
        NetworkServiceLocator.PlayerService.SetCustomProperty(PLAYER_UPGRADE_READY_KEY, false);

      if (GrossUpgradePanel != null)
      {
        StartCoroutine(AnimateShadowAlphaBack());
        HideOrShowGrossUpgradePanel(true);
      }
      EventChannelLocator.MainContainer?.pauseChannel?.Raise(false);
    }


    void OnEnable()
    {
      EventChannelLocator.MainContainer.magicUpgradeChannel.RegisterListener(OnMagicUpgradeRequested);
      if (NetworkServiceLocator.IsInitialized)
        NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged += OnPlayerPropertyChanged;
    }

    void OnDisable()
    {
      EventChannelLocator.MainContainer?.magicUpgradeChannel?.UnregisterListener(OnMagicUpgradeRequested);
      if (NetworkServiceLocator.IsInitialized)
        NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged -= OnPlayerPropertyChanged;
    }

    void OnDestroy()
    {
      EventChannelLocator.MainContainer?.magicUpgradeChannel?.UnregisterListener(OnMagicUpgradeRequested);
      if (instance == this)
        instance = null;
    }

    void OnMagicUpgradeRequested(bool isOpen)
    {
      if (isOpen)
      {
        // 死亡玩家不参与卡牌选择，但仍然参与暂停/恢复循环
        var localAttr = PlayerManager.instance?.GetLocalPlayerAttribute(AttributeKeyConst.Main);
        if (localAttr != null && localAttr.GetIsDead())
        {
          // 同步暂停状态，确保 MasterClient 的 RPC 能发出
          EventChannelLocator.MainContainer.pauseChannel?.Raise(true);
          if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
            NetworkServiceLocator.PlayerService.SetCustomProperty(PLAYER_UPGRADE_READY_KEY, true);
          return;
        }
        OpenMagicUpgradePanel();
      }
      else
      {
        CloseMagicUpgradePanel();
      }
    }


    /// <summary>
    /// 隐藏升级面板
    /// </summary>
    /// <param name="duration">淡出时长，默认1.5秒</param>
    public void HideOrShowGrossUpgradePanel(bool isHide)
    {
      if (GrossUpgradePanel == null)
      {
        Debug.LogWarning("[MagicUpgradeManager] GrossUpgradePanel 为 null，跳过");
        return;
      }

      // 获取或添加 CanvasGroup 组件
      CanvasGroup canvasGroup = GrossUpgradePanel.GetComponent<CanvasGroup>();
      if (canvasGroup == null)
        canvasGroup = GrossUpgradePanel.AddComponent<CanvasGroup>();

      // 隐藏或显示面板

      if (!isHide)
      {
        GrossUpgradePanel.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.6f).SetEase(Ease.InOutQuad);
        return;
      }

      // 缓慢透明消失
      canvasGroup.DOFade(0f, 0.6f)
          .SetEase(Ease.InOutQuad)
          .OnComplete(() =>
          {
            GrossUpgradePanel.SetActive(false);
          });
    }

    #region 获取卡牌

    private void SetCardData(CardConfigSO[] threeCards)
    {
      if (threeCards == null || threeCards.Length < 3)
        return;

      SetSingleCardData(threeCards[0], NameText_1, ContentText_1, QualityText_1, effects_1);
      SetSingleCardData(threeCards[1], NameText_2, ContentText_2, QualityText_2, effects_2);
      SetSingleCardData(threeCards[2], NameText_3, ContentText_3, QualityText_3, effects_3);
    }

    private void SetSingleCardData(
        CardConfigSO cardData,
        TextMeshProUGUI nameText,
        TextMeshProUGUI contentText,
        TextMeshProUGUI qualityText,
        ParticleSystem[] effects
    )
    {
      if (cardData == null)
        return;

      nameText.text = cardData.cardName;
      string quality = cardData.quality.ToString();

      switch (quality)
      {
        case "Normal":
          contentText.text = cardData.description.Contains("生命")
              ? contentText.text =
                  $"{cardData.description}<color=#CCCCCC>{cardData.value}</color>"
              : contentText.text =
                  $"{cardData.description}<color=#CCCCCC>{cardData.value}</color>%";

          qualityText.text = $"<color=#CCCCCC>普通</color>";
          SetEffectActive(effects, 0);
          break;
        case "Epic":
          contentText.text = cardData.description.Contains("生命")
              ? contentText.text =
                  $"{cardData.description}<color=#800080>{cardData.value}</color>"
              : contentText.text =
                  $"{cardData.description}<color=#800080>{cardData.value}</color>%";
          qualityText.text = $"<color=#800080>史诗</color>";
          SetEffectActive(effects, 1);
          break;
        case "Legend":
          contentText.text = cardData.description.Contains("生命")
              ? contentText.text =
                  $"{cardData.description}<color=#FF4444>{cardData.value}</color>"
              : contentText.text =
                  $"{cardData.description}<color=#FF4444>{cardData.value}</color>%";
          qualityText.text = $"<color=#FF4444>传说</color>";
          SetEffectActive(effects, 2);
          break;
      }
    }

    private void SetEffectActive(ParticleSystem[] effects, int activeIndex)
    {
      for (int i = 0; i < effects.Length; i++)
      {
        if (effects[i] != null)
        {
          effects[i].gameObject.SetActive(i == activeIndex);
        }
      }
    }

    private CardConfigSO[] GetCardData()
    {
      if (isAllExCard)
      {
        var query = new SkillQueryData(SkillQueryType.GetThreeRandomEXCards, currentEventName);
        EventChannelLocator.MainContainer.skillQueryChannel.Raise(query);
        return query.cardsResult;
      }
      var cardQuery = new SkillQueryData(SkillQueryType.GetThreeRandomCards);
      EventChannelLocator.MainContainer.skillQueryChannel.Raise(cardQuery);
      return cardQuery.cardsResult;
    }

    #endregion

    public void SetCurrentEventName(string eventName)
    {
      currentEventName = eventName;
    }
  }
}