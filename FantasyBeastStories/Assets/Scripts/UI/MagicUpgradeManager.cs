using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        GameObject GrossUpgradePanel;
        GameObject Card_1Effect;
        List<ParticleSystem> OneCardEffects;
        GameObject Card_2Effect;
        List<ParticleSystem> TwoCardEffects;
        GameObject Card_3Effect;
        List<ParticleSystem> ThreeCardEffects;

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

        // 为卡片添加鼠标悬停事件（在Initialize中调用）
        private void RegisterCardHoverEvents()
        {
            AddHoverEffect(Card_1, 1.2f, 0.2f, OneCardEffects);
            AddHoverEffect(Card_2, 1.2f, 0.2f, TwoCardEffects);
            AddHoverEffect(Card_3, 1.2f, 0.2f, ThreeCardEffects);
        }

        // 为单个卡片的特效列表添加悬停缩放效果
        private void AddHoverEffect(GameObject card, float scaleMultiplier, float duration, List<ParticleSystem> effectList)
        {
            if (card == null) return;

            // 获取或添加EventTrigger组件
            EventTrigger trigger = card.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = card.AddComponent<EventTrigger>();

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
            card.transform.SetAsLastSibling();

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
        private void AddOnClickEvent(GameObject card, UnityEngine.Events.UnityAction callBack)
        {
            // 为卡片添加点击事件
            if (card == null) return;

            EventTrigger trigger = card.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = card.AddComponent<EventTrigger>();

            // 点击事件
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => callBack?.Invoke());
            trigger.triggers.Add(clickEntry);
        }

        //点击回调处理
        private void OnCardPointerClick()
        {
            Debug.Log("点击了卡片");
            GameObject selectedCard = EventSystem.current.currentSelectedGameObject;
            if (selectedCard != null)
            {
                StartCoroutine(RotateCard(selectedCard, 1f));
            }
        }
        
        //点击卡片后进行旋转的方法
        private IEnumerator RotateCard(GameObject card, float rotationSpeed)
        {
            EventTrigger trigger = card.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                yield return null;
            }
            trigger.enabled = false;
            Sequence rotateSequence = DOTween.Sequence();
            // 先旋转到90度（侧面对玩家）
            rotateSequence.Append(card.transform.DORotate(new Vector3(0, 90, 0), 0.3f, RotateMode.Fast));
    
            // 更新卡片内容（如果需要显示升级后的信息）
            rotateSequence.AppendCallback(() =>
            {
                // 在这里可以更新卡片显示的内容
                UpdateCardContentAfterRotation(card);
            });
    
            // 再旋转到0度（正面对玩家）
            rotateSequence.Append(card.transform.DORotate(new Vector3(0, 0, 0), 0.3f, RotateMode.Fast));
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
            AddOnClickEvent(Card_1, OnCardPointerClick);
            AddOnClickEvent(Card_2, OnCardPointerClick);
            AddOnClickEvent(Card_3, OnCardPointerClick);
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