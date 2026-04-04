using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCanvas : MonoBehaviour
{
    public static LoadingCanvas instance;
    private Animator loadingAnimator;
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
        loadingAnimator = GameObject.Find("LoadingPanel").GetComponent<Animator>();
        HideLoading();
    }
    [SerializeField] private GameObject loadingImage;

    public void ShowLoading()
    {
        loadingAnimator.SetBool("FadeIn", true);
        loadingImage.SetActive(true);
    }

    public void HideLoading()
    {
        loadingAnimator.SetBool("FadeIn", false);
        loadingImage.SetActive(false);
    }
}
