using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    [SerializeField]
    private Button testCardEffect;
    [SerializeField]
    private GameObject testobject;

    // Start is called before the first frame update
      protected virtual void Start()
    {
        if (testCardEffect != null)
        {
            testCardEffect.onClick.AddListener(() =>
            {
                testobject.GetComponent<BallRobot_Blue>().StartTransfer();
            });
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
