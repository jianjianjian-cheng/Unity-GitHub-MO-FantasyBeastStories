using System;
using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using DG.Tweening;
using Core;
using Core.Channels.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
  public class AttributeInfoPanel : MonoBehaviour
  {
    [Header("显示属性的UI")]
    [SerializeField]
    private TextMeshProUGUI attackPowerText;
    [SerializeField]
    private TextMeshProUGUI defencePowerText;
    [SerializeField]
    private TextMeshProUGUI CriticalMultiplier;
    [SerializeField]
    private TextMeshProUGUI CriticalChance;
    [SerializeField]
    private TextMeshProUGUI maxHealthText;
    [SerializeField]
    private TextMeshProUGUI moveSpeed;
    [SerializeField]
    private TextMeshProUGUI healthRecover;
    [SerializeField]
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
      // UI引用已改为Inspector拖拽赋值
    }

    private void Update()
    {
      // PC 键盘快捷键：Tab 按住显示，松开隐藏
      if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
      {
        holdCoroutine = StartCoroutine(HoldCoroutine());
      }

      if (UnityEngine.Input.GetKeyUp(KeyCode.Tab))
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

    /// <summary>
    /// 供 UI 按钮调用的切换方法（手机端使用）
    /// </summary>
    public void TogglePanel()
    {
      if (isShow)
      {
        HidePanel();
      }
      else
      {
        ShowPanel();
      }
    }

    private IEnumerator HoldCoroutine()
    {
      yield return new WaitForSeconds(holdDuration);
      ShowPanel();
    }

    private void ShowPanel()
    {
      RefreshAttributeDisplay();
      isShow = true;
      currentTween?.Kill();
      currentTween = panel.DOAnchorPos(shownPosition, animationDuration).SetEase(Ease.OutQuad);
    }

    private void RefreshAttributeDisplay()
    {
      // 通过事件通道查询本地玩家属性
      var query = new PlayerAttributeData(PlayerAttributeQueryType.GetLocalPlayerAttribute)
      { attributeName = AttributeKeyConst.Main };
      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(query);

      AttributePlayerBase attr = query.attribute;
      if (attr == null)
        return;

      if (attackPowerText != null)
        attackPowerText.text = $"{attr.GetAttackPower():F0}";

      if (defencePowerText != null)
        defencePowerText.text = $"{attr.GetDefensePower():F0}";

      if (CriticalMultiplier != null)
        CriticalMultiplier.text = $"{attr.GetCriticalMultiplier():F1}x";

      if (CriticalChance != null)
        CriticalChance.text = $"{attr.GetCriticalChance() * 100:F0}%";

      if (maxHealthText != null)
        maxHealthText.text = $"{attr.GetMaxHealth():F0}";

      if (moveSpeed != null)
        moveSpeed.text = $"{attr.GetMoveSpeed():F1}";

      if (healthRecover != null)
        healthRecover.text = $"{attr.GetHealthRecover():F1}";

      if (attackSpeed != null)
        attackSpeed.text = $"{attr.GetAttackSpeed():F2}";
    }

    private void HidePanel()
    {
      isShow = false;
      currentTween?.Kill();
      currentTween = panel.DOAnchorPos(hiddenPosition, animationDuration).SetEase(Ease.InQuad);
    }
  }
}