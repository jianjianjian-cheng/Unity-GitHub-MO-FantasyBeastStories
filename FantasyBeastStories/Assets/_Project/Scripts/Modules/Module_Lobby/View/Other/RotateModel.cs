using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;
using UI.Input;

namespace UI.Other
{
    public class RotateModel : MonoBehaviour
    {
        [SerializeField] private Transform modelTransform;
        private bool canRotate;
        [SerializeField] private float rotationScale = 1f;
        private bool isRotate;
        private Vector3 startpoint;
        private Vector3 startAngle;

        void Start()
        {
            modelTransform = transform;
        }

        void Update()
        {
            if (!canRotate) return;
            if (MobileInputHelper.GetPointerDown() && !isRotate)
            {
                startpoint = MobileInputHelper.GetScreenPosition();
                startAngle = modelTransform.eulerAngles;
                isRotate = true;
            }
            if (MobileInputHelper.GetPointerUp())
            {
                isRotate = false;
            }
            if (isRotate)
            {
                var currentpoint = MobileInputHelper.GetScreenPosition();
                var x = startpoint.x - currentpoint.x;
                modelTransform.eulerAngles = startAngle + new Vector3(0, x * rotationScale, 0);
            }
        }

        void OnEnable()
        {
            canRotate = true;
        }

        void OnDisable()
        {
            canRotate = false;
        }
    }
}