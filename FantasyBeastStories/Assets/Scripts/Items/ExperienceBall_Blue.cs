using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Items
{
    public class ExperienceBall_Blue : ExperienceBallBase
    {
        protected override void Start()
        {
            ExperienceValue = Random.Range(20, 31);
        }
    }
}
