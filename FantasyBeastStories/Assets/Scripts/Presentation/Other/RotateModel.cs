using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Domain.Event;

namespace Presentation.Other
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
            if (Input.GetMouseButtonDown(0) && !isRotate)
            {
                startpoint = Input.mousePosition;
                startAngle = modelTransform.eulerAngles;
                isRotate = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                isRotate = false;
            }
            if (isRotate)
            {
                var currentpoint = Input.mousePosition;
                var x = startpoint.x - currentpoint.x;
                modelTransform.eulerAngles = startAngle + new Vector3(0, x * rotationScale, 0);
            }
        }

        void OnEnable()
        {
            EventChannelLocator.MainContainer.changeCanRotateChannel.RegisterListener(OnChangeCanRotate);
        }

        void OnDisable()
        {
            EventChannelLocator.MainContainer.changeCanRotateChannel.UnregisterListener(OnChangeCanRotate);
        }

        private void OnChangeCanRotate(bool canRotate)
        {
            this.canRotate = canRotate;
            Debug.Log($"canRotate: {canRotate}");
            modelTransform.eulerAngles = Vector3.zero;
        }
    }
}
