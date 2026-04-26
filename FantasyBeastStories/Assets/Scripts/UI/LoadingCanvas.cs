using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCanvas : MonoBehaviour
{
    public static LoadingCanvas instance;
    private GameObject LoadingPanel;
    private ParticleSystem loadingParticle;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        HideLoading();
        // 可以找到所有物体，包括未激活的
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "LoadingPanel")
            {
                LoadingPanel = obj;
                loadingParticle = LoadingPanel.GetComponentInChildren<ParticleSystem>();
                break;
            }
        }
        LoadingPanel.SetActive(false);
    }

    public void ShowLoading()
    {
        if (loadingParticle != null)
        {
            Transitioner.Instance.TransitionOutWithoutChangingScene();
        }
    }

    public void HideLoading()
    {
        if (loadingParticle != null)
        {

        }
    }
}
