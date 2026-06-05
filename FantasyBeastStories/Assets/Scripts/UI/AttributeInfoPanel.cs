using System;
using System.Collections;
using System.Collections.Generic;
using Atttibute;
using DG.Tweening;
using Manager;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace UI
{
    public class AttributeInfoPanel : MonoBehaviourPun
    {
        [Header("显示属性的UI")]
        private TextMeshProUGUI attackPowerText;
        private TextMeshProUGUI defencePowerText;
        private TextMeshProUGUI CriticalMultiplier;
        private TextMeshProUGUI CriticalChance;
        private TextMeshProUGUI maxHealthText;
        private TextMeshProUGUI moveSpeed;
        private TextMeshProUGUI healthRecover;
        private TextMeshProUGUI attackSpeed;

        [SerializeField]
        private RectTransform panel;

        private bool isShow = false;
        private float holdDuration = 0.1f; // 长按判定时间
        private float animationDuration = 0.2f; //动画持续时间

        [Header("面板位置")]
        [SerializeField]
        private Vector2 hiddenPosition; // 隐藏位置

        [SerializeField]
        private Vector2 shownPosition; // 显示位置

        private Coroutine holdCoroutine;
        private Tween currentTween;

        private void Awake()
        {
            panel = GameObject.Find("AttributeInfoPanel")?.GetComponent<RectTransform>();
            attackPowerText = transform
                .Find("AttributeInfoPanel/InfoAera/AttackPower")
                .GetComponent<TextMeshProUGUI>();
            defencePowerText = transform
                .Find("AttributeInfoPanel/InfoAera/DefencePower")
                .GetComponent<TextMeshProUGUI>();
            CriticalMultiplier = transform
                .Find("AttributeInfoPanel/InfoAera/CriticalMultiplier")
                .GetComponent<TextMeshProUGUI>();
            CriticalChance = transform
                .Find("AttributeInfoPanel/InfoAera/CriticalChance")
                .GetComponent<TextMeshProUGUI>();
            maxHealthText = transform
                .Find("AttributeInfoPanel/InfoAera/MaxHealth")
                .GetComponent<TextMeshProUGUI>();
            moveSpeed = transform
                .Find("AttributeInfoPanel/InfoAera/MoveSpeed")
                .GetComponent<TextMeshProUGUI>();
            healthRecover = transform
                .Find("AttributeInfoPanel/InfoAera/HealthRecover")
                .GetComponent<TextMeshProUGUI>();
            attackSpeed = transform
                .Find("AttributeInfoPanel/InfoAera/AttackSpeed")
                .GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (!GameManager.isTest)
            {
                if (!photonView.IsMine)
                {
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                holdCoroutine = StartCoroutine(HoldCoroutine());
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                if (holdCoroutine != null)
                {
                    StopCoroutine(holdCoroutine);
                }

                if (isShow)
                {
                    HidePanel();
                }
            }
        }

        private IEnumerator HoldCoroutine()
        {
            yield return new WaitForSeconds(holdDuration);
            ShowPanel();
        }

        private void ShowPanel()
        {
            isShow = true;
            currentTween?.Kill();

            panel.anchoredPosition = hiddenPosition;
            panel.gameObject.SetActive(true);

            Setinfo();

            //面板从隐藏位置显示到显示位置
            currentTween = panel.DOAnchorPos(shownPosition, animationDuration);
        }

        private void HidePanel()
        {
            isShow = false;
            currentTween?.Kill();

            currentTween = panel
                .DOAnchorPos(hiddenPosition, animationDuration)
                .SetEase(Ease.InBack) // 收回效果
                .OnComplete(() =>
                {
                    panel.gameObject.SetActive(false);
                });
        }

        private void Setinfo()
        {
            AttributePlayerBase attributePlayerBase = EventManager.instance.GetLocalPlayerAttribute(
                EventNames.PlayerAttribute_Main
            );

            attackPowerText.text = attributePlayerBase.GetAttackPower().ToString();
            defencePowerText.text = attributePlayerBase.GetDefensePower().ToString();
            CriticalMultiplier.text =
                (attributePlayerBase.GetCriticalMultiplier() * 100).ToString() + "%";
            CriticalChance.text = (attributePlayerBase.GetCriticalChance() * 100).ToString() + "%";
            maxHealthText.text = attributePlayerBase.GetMaxHealth().ToString();
            moveSpeed.text = attributePlayerBase.GetMoveSpeed().ToString();
            healthRecover.text = attributePlayerBase.GetHealthRecover().ToString();
            attackSpeed.text = attributePlayerBase.GetAttackSpeed().ToString() + "%";
        }
    }
}
