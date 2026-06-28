using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels;
using UnityEngine;


namespace Domain.Item
{
    public class ExperienceBallBase : DropItemBase
    {
        protected int ExperienceValue;

        protected override void OnReachPlayer()
        {
            EventChannelLocator.MainContainer.experienceChannel.Raise(ExperienceValue);
            base.OnReachPlayer();
        }
    }
}