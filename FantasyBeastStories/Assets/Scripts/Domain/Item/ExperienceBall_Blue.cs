using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Domain.Item
{
    public class ExperienceBall_Blue : ExperienceBallBase
    {
        protected override void Start()
        {
            ExperienceValue = Random.Range(50, 71);
        }
    }
}
