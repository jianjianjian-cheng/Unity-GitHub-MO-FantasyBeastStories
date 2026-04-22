using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;

public class RotateModel : MonoBehaviour
{
    [SerializeField] private Transform modelTransform;
    private bool canRotate;
    [SerializeField] private float rotationScale = 1f;
    private bool isRotate;
    private Vector3 startpoint;
    private Vector3 startAngle;
    // Start is called before the first frame update
    void Start()
    {
        modelTransform = transform;
    }

    // Update is called once per frame
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
        if (EventManager.instance != null)
        {
            EventManager.instance.RegisterBoolEvent(EventNames.ChangeCanRotate, OnChangeCanRotate);
        }
        else
        {
            //创建一个新的EventManager实例并注册事件
            GameObject eventManagerObj = new GameObject("EventManager");
            EventManager eventManager = eventManagerObj.AddComponent<EventManager>();
            eventManager.RegisterBoolEvent(EventNames.ChangeCanRotate, OnChangeCanRotate);
            EventManager.instance = eventManager;
        }
    }

    void OnDisable()
    {
        EventManager.instance.UnRegisterBoolEvent(EventNames.ChangeCanRotate);
    }

    private void OnChangeCanRotate(bool canRotate)
    {
        this.canRotate = canRotate;
        Debug.Log($"canRotate: {canRotate}");
        //重置旋转角度
        modelTransform.eulerAngles = Vector3.zero;
    }
}
