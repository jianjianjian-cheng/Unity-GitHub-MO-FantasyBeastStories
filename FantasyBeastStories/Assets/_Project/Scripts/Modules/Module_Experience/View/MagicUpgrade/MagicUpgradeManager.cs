using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Controllers.Card;
using Controllers.Character;
using Controllers.Player;
using Core;
using Core.SharedModel;
using Core.Channels.General;
using Core.Channels.Player;
using Core.Contracts;
using Core.Network;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Core.Audio;
using DG.Tweening;

namespace UI
{
  /// <summary>
  /// 魔法升级管理器 — Controller + View 层。
  /// 面板状态和卡牌选择逻辑委托给 MagicUpgradeModel。
  /// UI 引用、动画、特效、网络同步由本类处理。
  /// </summary>
  public class MagicUpgradeManager : MonoBehaviour
  {
    #region 单例模式
    
    public static MagicUpgradeManager instance { get; private set; }

    void Awake()
    {
        instance = this;
        ServiceLocator.Register(this);
        Model = new MagicUpgradeModel();
        Model.SetCurrentEventName(eventName);
    }
    #endregion

    /// <summary>魔法升级模型实例（纯 C#，可单测）</summary>
    public MagicUpgradeModel Model { get; private set; }

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

    GameObject Card_1Catcher;
    GameObject Card_2Catcher;
    GameObject Card_3Catcher;

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

    private Image shadowImage;

    private GameObject AnimPoints;
    private Transform StartPoint;
    private Transform[] EndPoints;

    private Dictionary<ParticleSystem, Vector3> originalEffectScales =
        new Dictionary<ParticleSystem, Vector3>();

    private bool isInitialized = false;
    #endregion

    private const string PLAYER_UPGRADE_READY_KEY = "UpgradeReady";

    /// <summary>卡牌选择面板是否处于打开状态</summary>
    public bool IsPanelActive => Model.IsPanelActive;

    public bool isAllExCard
    {
      get => Model.IsAllExCard;
      set => Model.IsAllExCard = value;
    }

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

      NameText_1 = card1Transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
      ContentText_1 = card1Transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
      QualityText_1 = card1Transform.Find("QualityText")?.GetComponent<TextMeshProUGUI>();

      NameText_2 = card2Transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
      ContentText_2 = card2Transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
      QualityText_2 = card2Transform.Find("QualityText")?.GetComponent<TextMeshProUGUI>();

      NameText_3 = card3Transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
      ContentText_3 = card3Transform.Find("ContentText")?.GetComponent<TextMeshProUGUI>();
      QualityText_3 = card3Transform.Find("QualityText")?.GetComponent<TextMeshProUGUI>();

      if (NameText_1 == null || ContentText_1 == null || QualityText_1 == null
          || NameText_2 == null || ContentText_2 == null || QualityText_2 == null
          || NameText_3 == null || ContentText_3 == null || QualityText_3 == null)
      {
        Debug.LogError("文字组件未找到");
        return;
      }
      #endregion

      #region 寻找特效
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

      effects_1 = oneCardEffect.GetComponentsInChildren<ParticleSystem>();
      effects_2 = twoCardEffect.GetComponentsInChildren<ParticleSystem>();
      effects_3 = threeCardEffect.GetComponentsInChildren<ParticleSystem>();

      foreach (ParticleSystem ps in effects_1) OneCardEffects.Add(ps);
      foreach (ParticleSystem ps in effects_2) TwoCardEffects.Add(ps);
      foreach (ParticleSystem ps in effects_3) ThreeCardEffects.Add(ps);
      #endregion

      #region 其他元素
      var shadowTransform = GrossUpgradePanel.transform.Find("Shadow");
      if (shadowTransform != null)
        shadowImage = shadowTransform.GetComponent<Image>();

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

      SaveOriginalScales();

      Card_1Catcher = CreateEventCatcher(Card_1);
      Card_2Catcher = CreateEventCatcher(Card_2);
      Card_3Catcher = CreateEventCatcher(Card_3);

      isInitialized = true;
    }

    private GameObject CreateEventCatcher(GameObject card)
    {
      if (card == null) return null;
      var existingCatcher = card.transform.Find("EventCatcher");
      if (existingCatcher != null) return existingCatcher.gameObject;

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
          originalEffectScales[effect] = effect.transform.localScale;
      }
    }

    #region 鼠标悬停处理

    private void RegisterCardHoverEvents()
    {
      AddHoverEffect(Card_1Catcher, Card_1, 1.2f, 0.2f, OneCardEffects);
      AddHoverEffect(Card_2Catcher, Card_2, 1.2f, 0.2f, TwoCardEffects);
      AddHoverEffect(Card_3Catcher, Card_3, 1.2f, 0.2f, ThreeCardEffects);
    }

    private void AddHoverEffect(GameObject catcher, GameObject card, float scaleMultiplier, float duration, List<ParticleSystem> effectList)
    {
      if (catcher == null) return;

      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger == null) trigger = catcher.AddComponent<EventTrigger>();

      trigger.triggers.Clear();

      EventTrigger.Entry enterEntry = new EventTrigger.Entry();
      enterEntry.eventID = EventTriggerType.PointerEnter;
      enterEntry.callback.AddListener((data) => OnCardPointerEnter(card, scaleMultiplier, duration, effectList));
      trigger.triggers.Add(enterEntry);

      EventTrigger.Entry exitEntry = new EventTrigger.Entry();
      exitEntry.eventID = EventTriggerType.PointerExit;
      exitEntry.callback.AddListener((data) => OnCardPointerExit(card, duration, effectList));
      trigger.triggers.Add(exitEntry);
    }

    private void OnCardPointerEnter(GameObject card, float scaleMultiplier, float duration, List<ParticleSystem> effectList)
    {
      if (card == null) return;

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

    private void OnCardPointerExit(GameObject card, float duration, List<ParticleSystem> effectList)
    {
      if (card == null) return;

      card.transform.DOKill();
      card.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);

      if (effectList != null)
      {
        foreach (var effect in effectList)
        {
          if (effect != null && originalEffectScales.ContainsKey(effect))
          {
            effect.transform.DOKill();
            effect.transform.DOScale(originalEffectScales[effect], duration).SetEase(Ease.OutBack);
          }
        }
      }
    }

    #endregion

    #region 卡片点击事件
    private void AddOnClickEvent(GameObject catcher, GameObject card)
    {
      if (catcher == null) return;

      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger == null) trigger = catcher.AddComponent<EventTrigger>();

      EventTrigger.Entry clickEntry = new EventTrigger.Entry();
      clickEntry.eventID = EventTriggerType.PointerClick;
      clickEntry.callback.AddListener((data) => OnCardPointerClick(card, catcher));
      trigger.triggers.Add(clickEntry);
    }

    private void OnCardPointerClick(GameObject selectedCard, GameObject catcher)
    {
      AudioManager.Instance.PlayUI("sfx_card_select");
      PlayClickFeedback(selectedCard);

      int index = selectedCard.name switch
      {
        "Card_1" => 0,
        "Card_2" => 1,
        "Card_3" => 2,
        _ => 0
      };

      EventChannelLocator.MainContainer.cardReceivedChannel.Raise(Model.CardData[index]);

      if (selectedCard != null && catcher != null)
        StartCoroutine(EndMoveCard(selectedCard, catcher, 1f));
    }

    private void PlayClickFeedback(GameObject card)
    {
      if (card == null) return;

      List<ParticleSystem> effectList = GetCardEffectList(card);
      card.transform.DOKill();
      if (effectList != null)
        foreach (var effect in effectList)
          if (effect != null) effect.transform.DOKill();

      Sequence clickSeq = DOTween.Sequence();
      clickSeq.Append(card.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
      clickSeq.Append(card.transform.DOScale(1.2f, 0.1f));

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

    private List<ParticleSystem> GetCardEffectList(GameObject card)
    {
      if (card == null) return null;
      return card.name switch
      {
        "Card_1" => OneCardEffects,
        "Card_2" => TwoCardEffects,
        "Card_3" => ThreeCardEffects,
        _ => null
      };
    }

    private IEnumerator EndMoveCard(GameObject card, GameObject catcher, float rotationSpeed)
    {
      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger == null) yield break;

      trigger.enabled = false;
      Sequence seq = DOTween.Sequence();
      float moveTime = 0.3f;

      foreach (GameObject c in Cards)
      {
        if (c != card)
        {
          seq.Append(c.transform.DOMove(StartPoint.position, moveTime));
          seq.Join(c.transform.DOScale(c.transform.localScale * 0.1f, moveTime));
          GameObject effect = GetEffect(c);
          if (effect != null)
          {
            seq.Join(effect.transform.DOMove(
                new Vector3(StartPoint.position.x, StartPoint.position.y, effect.transform.position.z), moveTime));
            seq.Join(effect.transform.DOScale(effect.transform.localScale * 0.1f, moveTime));
          }
        }
        else
        {
          StartCoroutine(DelayMoveCard(c, 0.4f));
        }
      }
      yield return seq.WaitForCompletion();
    }

    IEnumerator DelayMoveCard(GameObject card, float duration)
    {
      yield return new WaitForSeconds(duration);
      yield return new WaitForSeconds(1f);
      OnCardSelectionComplete();
    }

    private void OnCardSelectionComplete()
    {
      if (!Model.TryConfirmSelection())
        return;

      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        CloseMagicUpgradePanel();
        return;
      }
      NetworkServiceLocator.PlayerService.SetCustomProperty(PLAYER_UPGRADE_READY_KEY, true);
    }

    private GameObject GetEffect(GameObject card)
    {
      if (card == null) return null;
      return card.name switch
      {
        "Card_1" => Card_1Effect,
        "Card_2" => Card_2Effect,
        "Card_3" => Card_3Effect,
        _ => null
      };
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

      seq.AppendCallback(() => AudioManager.Instance.PlayUI("sfx_card_deal"));
      seq.Append(Card_1.transform.DOMove(EndPoints[0].position, moveTime));
      seq.Join(Card_1Effect.transform.DOMove(
          new Vector3(EndPoints[0].position.x, EndPoints[0].position.y, Card_1Effect.transform.position.z), moveTime));

      seq.AppendCallback(() => AudioManager.Instance.PlayUI("sfx_card_deal"));
      seq.Append(Card_2.transform.DOMove(EndPoints[1].position, moveTime));
      seq.Join(Card_2Effect.transform.DOMove(
          new Vector3(EndPoints[1].position.x, EndPoints[1].position.y, Card_2Effect.transform.position.z), moveTime));

      seq.AppendCallback(() => AudioManager.Instance.PlayUI("sfx_card_deal"));
      seq.Append(Card_3.transform.DOMove(EndPoints[2].position, moveTime));
      seq.Join(Card_3Effect.transform.DOMove(
          new Vector3(EndPoints[2].position.x, EndPoints[2].position.y, Card_3Effect.transform.position.z), moveTime));

      yield return seq.WaitForCompletion();
      RegisterCardHoverEvents();
      AddOnClickEvent(Card_1Catcher, Card_1);
      AddOnClickEvent(Card_2Catcher, Card_2);
      AddOnClickEvent(Card_3Catcher, Card_3);
    }

    private void OnPlayerPropertyChanged(int actorNumber, string key, object value)
    {
      if (key != PLAYER_UPGRADE_READY_KEY || !NetworkServiceLocator.PlayerService.IsMasterClient)
        return;

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

    private void ResetCardState()
    {
      foreach (var card in Cards)
      {
        if (card == null) continue;

        card.transform.DOKill();
        card.transform.localScale = Vector3.one;
        card.transform.position = new Vector3(StartPoint.position.x, StartPoint.position.y, card.transform.position.z);
        ResetEffectScale(card);
        ResetEffectPosition(card);
      }
      ResetCatchers();
    }

    private void ResetEffectScale(GameObject card)
    {
      if (card == null) return;

      List<ParticleSystem> effectList = card.name switch
      {
        "Card_1" => OneCardEffects,
        "Card_2" => TwoCardEffects,
        "Card_3" => ThreeCardEffects,
        _ => null
      };

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
      if (card == null) return;
      GameObject effect = GetEffect(card);
      if (effect != null)
      {
        effect.transform.DOKill();
        effect.transform.position = new Vector3(StartPoint.position.x, StartPoint.position.y, effect.transform.position.z);
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
      if (catcher == null) return;
      EventTrigger trigger = catcher.GetComponent<EventTrigger>();
      if (trigger != null)
      {
        trigger.enabled = true;
        trigger.triggers.Clear();
      }
    }

    // ──────────────────────────────────
    //  面板开关
    // ──────────────────────────────────

    public void OpenMagicUpgradePanel()
    {
      if (Model.IsPanelActive) return;

      if (GrossUpgradePanel == null)
      {
        _ = Model; // ensure Model exists
        return;
      }

      Model.Open();
      EventChannelLocator.MainContainer.bloomChannel.Raise(8f);

      // 恢复 CanvasGroup alpha（关闭时被渐变为 0）
      HideOrShowGrossUpgradePanel(false);

      if (!isInitialized)
      {
        Initialize();
        if (!isInitialized) return;
      }

      // 获取卡牌数据（委托 Model 编排选择逻辑）
      var cards = Model.ResolveCardSelection(
          getCards: GetCardData,
          getRandomExCard: (eventName) =>
          {
              var exCardQuery = new SkillQueryData(SkillQueryType.GetRandomEXCard, eventName);
              EventChannelLocator.MainContainer.skillQueryChannel.Raise(exCardQuery);
              return exCardQuery.cardResult;
          },
          luckRate: GetLuckRate()
      );

      Model.SetCardData(cards);

      ResetCardState();
      GrossUpgradePanel.SetActive(true);
      OpenMagicUpgradePanelAnim();
      OpenPanelInit(cards);
      EventChannelLocator.MainContainer.pauseChannel.Raise(true);
    }

    private void OpenPanelInit(CardConfigSO[] cardData)
    {
      NetworkServiceLocator.PlayerService.SetCustomProperty(PLAYER_UPGRADE_READY_KEY, false);
      SetCardData(cardData);
    }

    public void CloseMagicUpgradePanel()
    {
      Model.Close();
      EventChannelLocator.MainContainer?.bloomChannel?.Raise(5f);

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
            ServiceLocator.Unregister<MagicUpgradeManager>();
      EventChannelLocator.MainContainer?.magicUpgradeChannel?.UnregisterListener(OnMagicUpgradeRequested);
    }

    void OnMagicUpgradeRequested(bool isOpen)
    {
      if (isOpen)
      {
        var localAttr = ServiceLocator.Get<PlayerManager>()?.GetLocalPlayerAttribute(AttributeKeyConst.Main);
        if (localAttr != null && localAttr.GetIsDead())
        {
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

    public void HideOrShowGrossUpgradePanel(bool isHide)
    {
      if (GrossUpgradePanel == null) return;

      CanvasGroup canvasGroup = GrossUpgradePanel.GetComponent<CanvasGroup>();
      if (canvasGroup == null)
        canvasGroup = GrossUpgradePanel.AddComponent<CanvasGroup>();

      if (!isHide)
      {
        GrossUpgradePanel.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.6f).SetEase(Ease.InOutQuad);
        return;
      }

      canvasGroup.DOFade(0f, 0.6f).SetEase(Ease.InOutQuad)
          .OnComplete(() => GrossUpgradePanel.SetActive(false));
    }

    #region 获取卡牌

    private int GetLuckRate()
    {
      var luckQuery = new SkillQueryData(SkillQueryType.GetLuckRate);
      EventChannelLocator.MainContainer.skillQueryChannel.Raise(luckQuery);
      return luckQuery.intValue;
    }

    private void SetCardData(CardConfigSO[] threeCards)
    {
      if (threeCards == null || threeCards.Length < 3) return;

      SetSingleCardData(threeCards[0], NameText_1, ContentText_1, QualityText_1, effects_1);
      SetSingleCardData(threeCards[1], NameText_2, ContentText_2, QualityText_2, effects_2);
      SetSingleCardData(threeCards[2], NameText_3, ContentText_3, QualityText_3, effects_3);
    }

    private void SetSingleCardData(CardConfigSO cardData, TextMeshProUGUI nameText, TextMeshProUGUI contentText, TextMeshProUGUI qualityText, ParticleSystem[] effects)
    {
      if (cardData == null) return;

      nameText.text = cardData.cardName;
      string quality = cardData.quality.ToString();

      switch (quality)
      {
        case "Normal":
          contentText.text = cardData.description.Contains("生命")
              ? $"{cardData.description}<color=#CCCCCC>{cardData.value}</color>"
              : $"{cardData.description}<color=#CCCCCC>{cardData.value}</color>%";
          qualityText.text = $"<color=#CCCCCC>普通</color>";
          SetEffectActive(effects, 0);
          break;
        case "Epic":
          contentText.text = cardData.description.Contains("生命")
              ? $"{cardData.description}<color=#800080>{cardData.value}</color>"
              : $"{cardData.description}<color=#800080>{cardData.value}</color>%";
          qualityText.text = $"<color=#800080>史诗</color>";
          SetEffectActive(effects, 1);
          break;
        case "Legend":
          contentText.text = cardData.description.Contains("生命")
              ? $"{cardData.description}<color=#FF4444>{cardData.value}</color>"
              : $"{cardData.description}<color=#FF4444>{cardData.value}</color>%";
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
          effects[i].gameObject.SetActive(i == activeIndex);
      }
    }

    private CardConfigSO[] GetCardData()
    {
      if (Model.IsAllExCard)
      {
        var query = new SkillQueryData(SkillQueryType.GetThreeRandomEXCards, Model.CurrentEventName);
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
      Model.SetCurrentEventName(eventName);
    }
  }
}
