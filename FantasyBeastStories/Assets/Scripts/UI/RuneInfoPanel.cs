using System.Collections;
using System.Collections.Generic;
using Events;
using Manager;
using UnityEngine;
using UnityEngine.UI;

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
            EventManager.instance.RegisterEventComplex(EventNames.RuneInfo, RuneEventTran);
            Intilize();
        }

        private void Intilize()
        {
            runeNameText = transform.Find("RuneName").GetComponent<Text>();
            specialPowerNameText = transform.Find("SpecialPower/SpecialName").GetComponent<Text>();
            specialPowerDescriptionText = transform.Find("SpecialPower/SpecialDescription").GetComponent<Text>();
            runePowers_1Text = transform.Find("PowerPanel/PowerInfo_1").GetComponent<Text>();
            runePowers_2Text = transform.Find("PowerPanel/PowerInfo_2").GetComponent<Text>();
        }

        private void OnEnable()
        {
            EventManager.instance.RegisterEventComplex(EventNames.RuneInfo, RuneEventTran);
        }

        private void OnDisable()
        {
            EventManager.instance.UnRegisterEventComplex(EventNames.RuneInfo, RuneEventTran);
        }

        private void RuneEventTran(EventArgsBase args)
        {
            RuneEquipArgs runeEquipArgs = args as RuneEquipArgs;
            if (runeEquipArgs == null) return;

            UpdateRuneInfo(runeEquipArgs.runeName, runeEquipArgs.runePowers,
                           runeEquipArgs.specialPowerName, runeEquipArgs.specialPowerDescription);
        }

        private void UpdateRuneInfo(string runeName, Dictionary<int, string> runePowers, string specialPowerName, string specialPowerDescription)
        {
            Debug.Log("UpdateRuneInfo");
            runeNameText.text = runeName;
            int index = 1;
            specialPowerNameText.text = specialPowerName;
            specialPowerDescriptionText.text = specialPowerDescription;
            if (runePowers == null) return;
            foreach (var power in runePowers)
            {
                if (index == 1)
                {
                    if (power.Key > 0)
                    {
                        runePowers_1Text.text = "+" + power.Key + power.Value;
                    }
                    else
                    {
                        runePowers_1Text.text = "-" + power.Key + power.Value;
                    }
                }
                else if (index == 2)
                {
                    if (power.Key > 0)
                    {
                        runePowers_2Text.text = "+" + power.Key + power.Value;
                    }
                    else
                    {
                        runePowers_2Text.text = "-" + power.Key + power.Value;
                    }
                }
                index++;
            }
        }
    }
}
