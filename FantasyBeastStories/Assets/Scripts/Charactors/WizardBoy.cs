using System.Collections;
using System.Collections.Generic;
using CardData;
using Charactors;
using Manager;
using UnityEngine;

public class WizardBoy : PlayerController
{
    protected override void OnEnable()
    {
        base.OnEnable();
        EventManager.instance.RegisterCardEvent(
            EventNames.OnReceiveCard_WizardBoy,
            OnApplicationCard
        );
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventManager.instance.UnRegisterCardEvent(EventNames.OnReceiveCard_WizardBoy);
    }

    protected override void OnApplicationCard(CardConfigBase card)
    {
        base.OnApplicationCard(card);
    }
}
