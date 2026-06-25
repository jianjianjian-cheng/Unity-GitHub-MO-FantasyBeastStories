using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.Other
{
    public class TestScript : MonoBehaviour
    {
        [SerializeField]
        private Button testCardEffect;
        [SerializeField]
        private GameObject testobject;

        protected virtual void Start()
        {
            if (testCardEffect != null)
            {
                testCardEffect.onClick.AddListener(() =>
                {
                    testobject.GetComponent<Domain.Character.Pets.BallRobot_Blue>().StartTransfer();
                });
            }
        }

        void Update()
        {
        }
    }
}
