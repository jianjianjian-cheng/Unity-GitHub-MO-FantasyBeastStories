using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Other
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
                    testobject.GetComponent<Controllers.Character.Pets.BallRobot_Blue>().StartTransfer();
                });
            }
        }

        void Update()
        {
        }
    }
}
