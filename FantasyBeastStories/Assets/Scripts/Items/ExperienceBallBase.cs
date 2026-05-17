using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;


namespace Items
{
    public class ExperienceBallBase : DropItemBase
    {
        protected int ExperienceValue;

        protected override void OnReachPlayer()
        {
            GamePlayingManager.instance.AddExperience(ExperienceValue);
            base.OnReachPlayer();
        }
    }
}
