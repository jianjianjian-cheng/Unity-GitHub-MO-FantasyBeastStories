using System.Collections.Generic;
using Core;
using Controllers.Rune;
using UnityEngine;
using UnityEngine.UI;
using Core.SharedModel;

namespace UI
{
  public class RuneInfoPanel : MonoBehaviour
  {
    private Text runeNameText;
    private Text runePowers_1Text;
    private Text runePowers_2Text;
    private Text specialPowerNameText;
    private Text specialPowerDescriptionText;

    void Awake()
    {
      Intilize();
    }

    private void Intilize()
    {
      // 使用安全的组件获取方式
      runeNameText = SafeGetComponent<Text>(transform.Find("RuneName"));
      specialPowerNameText = SafeGetComponent<Text>(
          transform.Find("SpecialPower/SpecialName")
      );
      specialPowerDescriptionText = SafeGetComponent<Text>(
          transform.Find("SpecialPower/SpecialDescription")
      );
      runePowers_1Text = SafeGetComponent<Text>(transform.Find("PowerPanel/PowerInfo_1"));
      runePowers_2Text = SafeGetComponent<Text>(transform.Find("PowerPanel/PowerInfo_2"));
    }

    private T SafeGetComponent<T>(Transform parent)
        where T : Component
    {
      if (parent == null)
      {
        Debug.LogWarning($"[RuneInfoPanel] 找不到父对象");
        return null;
      }

      T component = parent.GetComponent<T>();
      if (component == null)
      {
        Debug.LogWarning($"[RuneInfoPanel] 找不到 {typeof(T).Name} 组件");
      }
      return component;
    }

    private void OnEnable()
    {
      if (EventChannelLocator.MainContainer != null)
        EventChannelLocator.MainContainer.runeInfoChannel.RegisterListener(RuneEventTran);
    }

    private void OnDisable()
    {
      UnregisterEvents();
    }

    private void OnDestroy()
    {
      UnregisterEvents();
    }

    private void UnregisterEvents()
    {
      if (EventChannelLocator.MainContainer != null)
        EventChannelLocator.MainContainer.runeInfoChannel.UnregisterListener(RuneEventTran);
    }

    private void RuneEventTran(RuneEquipArgs runeEquipArgs)
    {
      // 防御性检查：对象可能已销毁
      if (this == null || runeEquipArgs == null)
        return;

      UpdateRuneInfo(
          runeEquipArgs.runeName,
          runeEquipArgs.runePowers,
          runeEquipArgs.specialPowerName,
          runeEquipArgs.specialPowerDescription
      );
    }

    private void UpdateRuneInfo(
        string runeName,
        List<RunePower> runePowers,
        string specialPowerName,
        string specialPowerDescription
    )
    {
      Debug.Log("UpdateRuneInfo");

      if (runeNameText != null)
        runeNameText.text = runeName;

      if (specialPowerNameText != null)
        specialPowerNameText.text = specialPowerName;

      if (specialPowerDescriptionText != null)
        specialPowerDescriptionText.text = specialPowerDescription;

      // 清空两个属性文本，防止上次残留
      if (runePowers_1Text != null)
        runePowers_1Text.text = string.Empty;
      if (runePowers_2Text != null)
        runePowers_2Text.text = string.Empty;

      if (runePowers == null)
        return;

      for (int i = 0; i < runePowers.Count && i < 2; i++)
      {
        var power = runePowers[i];
        string text = power.value > 0
            ? $"+{power.value}{power.label}"
            : $"{power.value}{power.label}";

        if (i == 0 && runePowers_1Text != null)
          runePowers_1Text.text = text;
        else if (i == 1 && runePowers_2Text != null)
          runePowers_2Text.text = text;
      }
    }
  }
}