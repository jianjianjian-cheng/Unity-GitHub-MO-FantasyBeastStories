using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DG.Tweening;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

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
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region UI对象
        List<GameObject> CardEffects;
        GameObject GrossUpgradePanel;
        GameObject Card_1Effect;
        List<ParticleSystem> OneCardEffects;
        GameObject Card_2Effect;
        List<ParticleSystem> TwoCardEffects;
        GameObject Card_3Effect;
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
        private Dictionary<ParticleSystem, Vector3> originalEffectScales = new Dictionary<ParticleSystem, Vector3>();
        #endregion
        void Start()
        {
            CardEffects = new List<GameObject>();
            Cards = new List<GameObject>();
            OneCardEffects = new List<ParticleSystem>();
            TwoCardEffects = new List<ParticleSystem>();
            ThreeCardEffects = new List<ParticleSystem>();
            Initialize();
        }

        void Update()
        {

        }

        private void Initialize()
        {
            GrossUpgradePanel = transform.Find("GrossUpgradePanel").gameObject;
            if (GrossUpgradePanel == null)
            {
                Debug.LogError("GrossUpgradePanel 未找到");
                return;
            }

            #region 寻找卡片
            //寻找魔法升级面板
            MagicUpgradePanel = GrossUpgradePanel.transform.Find("MagicUpgradePanel").gameObject;
            if (MagicUpgradePanel == null)
            {
                Debug.LogError("MagicUpgradePanel 未找到");
                return;
            }

            //寻找第一卡片
            Card_1 = MagicUpgradePanel.transform.Find("Card_1").gameObject;
            if (Card_1 == null)
            {
                Debug.LogError("Card_1 未找到");
                return;
            }
            else
            {
                Cards.Add(Card_1);
                //寻找该卡片卡片位置下的所有文字
                NameText_1 = Card_1.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                if (NameText_1 == null)
                {
                    Debug.LogError("NameText_1 未找到");
                    return;
                }
                ContentText_1 = Card_1.transform.Find("ContentText").GetComponent<TextMeshProUGUI>();
                if (ContentText_1 == null)
                {
                    Debug.LogError("ContentText_1 未找到");
                    return;
                }
                QualityText_1 = Card_1.transform.Find("QualityText").GetComponent<TextMeshProUGUI>();
                if (QualityText_1 == null)
                {
                    Debug.LogError("QualityText_1 未找到");
                    return;
                }
            }

            //寻找第二卡片
            Card_2 = MagicUpgradePanel.transform.Find("Card_2").gameObject;

            if (Card_2 == null)
            {
                Debug.LogError("Card_2 未找到");
                return;
            }
            else
            {
                Cards.Add(Card_2);
                //寻找该卡片卡片位置下的所有文字
                NameText_2 = Card_2.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                if (NameText_2 == null)
                {
                    Debug.LogError("NameText_2 未找到");
                    return;
                }
                ContentText_2 = Card_2.transform.Find("ContentText").GetComponent<TextMeshProUGUI>();
                if (ContentText_2 == null)
                {
                    Debug.LogError("ContentText_2 未找到");
                    return;
                }
                QualityText_2 = Card_2.transform.Find("QualityText").GetComponent<TextMeshProUGUI>();
                if (QualityText_2 == null)
                {
                    Debug.LogError("QualityText_2 未找到");
                    return;
                }
            }

            //寻找第三卡片
            Card_3 = MagicUpgradePanel.transform.Find("Card_3").gameObject;
            if (Card_3 == null)
            {
                Debug.LogError("Card_3 未找到");
                return;
            }
            else
            {
                Cards.Add(Card_3);
                //寻找该卡片卡片位置下的所有文字
                NameText_3 = Card_3.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                if (NameText_3 == null)
                {
                    Debug.LogError("NameText_3 未找到");
                    return;
                }
                ContentText_3 = Card_3.transform.Find("ContentText").GetComponent<TextMeshProUGUI>();
                if (ContentText_3 == null)
                {
                    Debug.LogError("ContentText_3 未找到");
                    return;
                }
                QualityText_3 = Card_3.transform.Find("QualityText").GetComponent<TextMeshProUGUI>();
                if (QualityText_3 == null)
                {
                    Debug.LogError("QualityText_3 未找到");
                    return;
                }
            }
            #endregion

            #region 寻找特效
            //寻找卡片一位置的特效
            Card_1Effect = MagicUpgradePanel.transform.Find("OneCardEffect").gameObject;
            if (Card_1Effect == null)
            {
                Debug.LogError("OneCardEffect 未找到");
                return;
            }
            else
            {
                CardEffects.Add(Card_1Effect);
                //寻找该该卡片位置下的所有特效
                foreach (Transform child in Card_1Effect.transform)
                {
                    if (child.GetComponent<ParticleSystem>() != null)
                    {
                        OneCardEffects.Add(child.GetComponent<ParticleSystem>());
                    }
                }
            }

            //寻找卡片二位置的特效
            Card_2Effect = MagicUpgradePanel.transform.Find("TwoCardEffect").gameObject;
            if (Card_2Effect == null)
            {
                Debug.LogError("TwoCardEffect 未找到");
                return;
            }
            else
            {
                CardEffects.Add(Card_2Effect);
                //寻找该该卡片位置下的所有特效
                foreach (Transform child in Card_2Effect.transform)
                {
                    if (child.GetComponent<ParticleSystem>() != null)
                    {
                        TwoCardEffects.Add(child.GetComponent<ParticleSystem>());
                    }
                }
            }

            //寻找卡片三位置的特效
            Card_3Effect = MagicUpgradePanel.transform.Find("ThreeCardEffect").gameObject;
            if (Card_3Effect == null)
            {
                Debug.LogError("ThreeCardEffect 未找到");
                return;
            }
            else
            {
                CardEffects.Add(Card_3Effect);
                //寻找该该卡片位置下的所有特效
                foreach (Transform child in Card_3Effect.transform)
                {
                    if (child.GetComponent<ParticleSystem>() != null)
                    {
                        ThreeCardEffects.Add(child.GetComponent<ParticleSystem>());
                    }
                }
            }
            #endregion

            #region 其他元素
            //寻找背景阴影
            shadowImage = GrossUpgradePanel.transform.Find("Shadow").GetComponent<Image>();
            if (shadowImage == null)
            {
                Debug.LogError("ShadowImage 未找到");
                return;
            }

            //寻找卡片动画相关位置
            AnimPoints = GrossUpgradePanel.transform.Find("AnimPoints").gameObject;
            if (AnimPoints == null)
            {
                Debug.LogError("AnimPoints 未找到");
                return;
            }
            EndPoints = AnimPoints.GetComponentsInChildren<Transform>()
            .Where(x => x.name == "EndPoint").ToArray();
            if (EndPoints.Length < 3)
            {
                Debug.LogError("EndPoints 数量不足");
                return;
            }
            StartPoint = AnimPoints.transform.Find("StartPoint").transform;
            if (StartPoint == null)
            {
                Debug.LogError("StartPoint 未找到");
                return;
            }
            #endregion

            //保存所有特效的原始Scale
            SaveOriginalScales();

            //为每张卡片创建事件捕获层，确保鼠标事件不被子对象拦截
            Card_1Catcher = CreateEventCatcher(Card_1);
            Card_2Catcher = CreateEventCatcher(Card_2);
            Card_3Catcher = CreateEventCatcher(Card_3);
        }




        // 为卡片创建透明的事件捕获层，覆盖在所有子对象之上
        // 确保鼠标射线始终命中捕获层而非子对象，避免悬停动画被文字等子对象拦截
        private GameObject CreateEventCatcher(GameObject card)
        {
            if (card == null) return null;

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
            foreach (var effect in OneCardEffects)
            {
                if (effect != null && !originalEffectScales.ContainsKey(effect))
                {
                    originalEffectScales[effect] = effect.transform.localScale;
                }
            }
            foreach (var effect in TwoCardEffects)
            {
                if (effect != null && !originalEffectScales.ContainsKey(effect))
                {
                    originalEffectScales[effect] = effect.transform.localScale;
                }
            }
            foreach (var effect in ThreeCardEffects)
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

        // 为单个卡片的特效列表添加悬停缩放效果
        // catcher: 事件捕获层，用于接收鼠标事件
        // card: 实际需要执行动画的卡片对象
        private void AddHoverEffect(GameObject catcher, GameObject card, float scaleMultiplier, float duration, List<ParticleSystem> effectList)
        {
            if (catcher == null) return;

            // 获取或添加EventTrigger组件到捕获层
            EventTrigger trigger = catcher.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = catcher.AddComponent<EventTrigger>();

            // 鼠标进入事件
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => OnCardPointerEnter(card, scaleMultiplier, duration, effectList));
            trigger.triggers.Add(enterEntry);

            // 鼠标退出事件
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnCardPointerExit(card, duration, effectList));
            trigger.triggers.Add(exitEntry);
        }

        private void OnCardPointerEnter(GameObject card, float scaleMultiplier, float duration, List<ParticleSystem> effectList)
        {
            card.transform.DOKill();
            card.transform.DOScale(scaleMultiplier, duration).SetEase(Ease.OutBack);

            // 基于原始Scale放大列表中每一个特效
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
            card.transform.DOKill();
            card.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);

            // 恢复到原始Scale
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
        // 卡片点击事件
        private void AddOnClickEvent(GameObject catcher, GameObject card)
        {
            // 为卡片添加点击事件
            if (catcher == null) return;

            EventTrigger trigger = catcher.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = catcher.AddComponent<EventTrigger>();

            // 点击事件
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => OnCardPointerClick(card, catcher));
            trigger.triggers.Add(clickEntry);
        }

        //点击回调处理
        private void OnCardPointerClick(GameObject selectedCard, GameObject catcher)
        {
            Debug.Log("点击了卡片");
            if (selectedCard != null && catcher != null)
            {
                StartCoroutine(RotateCard(selectedCard, catcher, 1f));
            }
        }
        
        //点击卡片后进行一定的动画播放
        private IEnumerator RotateCard(GameObject card, GameObject catcher, float rotationSpeed)
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
                    //特效仅仅在x,y轴移动，不改变z轴位置
                    if (effect != null)
                    {
                        seq.Join(effect.transform.DOMove(new Vector3(StartPoint.transform.position.x, StartPoint.transform.position.y, effect.transform.position.z), moveTime));
                        seq.Join(effect.transform.DOScale(effect.transform.localScale * 0.1f, moveTime));
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
            card.transform.DOMove(EndPoints[1].position, duration);
            GameObject effect = GetEffect(card);
            if (effect != null)
            {
                effect.transform.DOMove(new Vector3(EndPoints[1].transform.position.x, EndPoints[1].transform.position.y, effect.transform.position.z), duration);
                effect.transform.DOScale(effect.transform.localScale * 0.1f, duration);
            }

            yield return new WaitForSeconds(1f);
            OnCardSelectionComplete();
        }
        
        //选择完卡片后的处理
        
        private void OnCardSelectionComplete()
        {
            GamePlayingManager.instance.OnPlayerUpgradeChoiceConfirmed();
        }
        
        //获取卡牌对应特效
        private GameObject GetEffect(GameObject card)
        {
            string cardName = card.name;
            //获取卡牌对应特效
            foreach (GameObject effect in CardEffects)
            {
                switch (cardName)
                {
                    case "Card_1":
                    return Card_1Effect;
                    case "Card_2":
                    return Card_2Effect;
                    case "Card_3":
                    return Card_3Effect;
                }
            }

            return null;
        }

        private void UpdateCardContentAfterRotation(GameObject card)
        {
            //更新卡片图片显示背面
        }


        #endregion

        #region 动画处理
        private void OpenMagicUpgradePanelAnim()
        {
            StartCoroutine(AnimateShadowAlpha());
        }

        //移动卡片到指定位置
        IEnumerator MoveCardEndToPosition()
        {
            Sequence seq = DOTween.Sequence();
            float moveTime = 0.3f;
            // ===== 第1步：发第1张牌 =====
            seq.Append(Card_1.transform.DOMove(EndPoints[0].position, moveTime));
            //特效移动
            //特效仅仅在x,y轴移动，不改变z轴位置
            seq.Join(Card_1Effect.transform.DOMove(new Vector3(EndPoints[0].position.x, EndPoints[0].position.y, EndPoints[0].position.z - 1f), moveTime));

            // ===== 第2步：发第2张牌 =====
            seq.Append(Card_2.transform.DOMove(EndPoints[1].position, moveTime));
            //特效移动
            seq.Join(Card_2Effect.transform.DOMove(new Vector3(EndPoints[1].position.x, EndPoints[1].position.y, EndPoints[1].position.z - 1f), moveTime));

            // ===== 第3步：发第3张牌 =====
            seq.Append(Card_3.transform.DOMove(EndPoints[2].position, moveTime));
            //特效移动
            seq.Join(Card_3Effect.transform.DOMove(new Vector3(EndPoints[2].position.x, EndPoints[2].position.y, EndPoints[2].position.z - 1f), moveTime));

            yield return seq.WaitForCompletion();
            RegisterCardHoverEvents();
            AddOnClickEvent(Card_1Catcher, Card_1);
            AddOnClickEvent(Card_2Catcher, Card_2);
            AddOnClickEvent(Card_3Catcher, Card_3);
            OnCardMoveToEndPositionComplete();
        }

        //卡片移动到end位置回调
        private void OnCardMoveToEndPositionComplete()
        {
            Debug.Log("卡片移动到end位置");
        }


        //阴影背景1秒内透明度变化，协程
        private IEnumerator AnimateShadowAlpha()
        {
            // 阴影背景1秒内透明度变化，协程
            float alpha = 0;
            while (alpha < 0.7f)
            {
                alpha += Time.deltaTime / 1;
                shadowImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            StartCoroutine(MoveCardEndToPosition());
        }

        //阴影背景1秒内透明度变化，协程
        private IEnumerator AnimateShadowAlphaBack()
        {
            // 阴影背景1秒内透明度变化，协程
            float alpha = 0.7f;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime / 1;
                shadowImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
        }
        #endregion

        //打开魔法升级面板
        public void OpenMagicUpgradePanel()
        {
            // 打开魔法升级面板时，全局Bloom强度增加
            if (GlobalVolumeManager.instance != null)
                GlobalVolumeManager.instance.SetBloomIntensity(15f);
            Debug.Log("打开魔法升级面板");
            GrossUpgradePanel.SetActive(true);
            OpenMagicUpgradePanelAnim();
        }

        //关闭魔法升级面板
        public void CloseMagicUpgradePanel()
        {
            // 关闭魔法升级面板时，全局Bloom强度减少
            if (GlobalVolumeManager.instance != null)
                GlobalVolumeManager.instance.SetBloomIntensity(5f);
            Debug.Log("关闭魔法升级面板");
            StartCoroutine(AnimateShadowAlphaBack());
            GrossUpgradePanel.SetActive(false);
        }
    }
}