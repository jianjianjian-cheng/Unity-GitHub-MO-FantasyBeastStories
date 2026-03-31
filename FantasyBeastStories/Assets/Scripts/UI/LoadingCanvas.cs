using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCanvas : MonoBehaviour
{
    public static LoadingCanvas instance;
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
        HideLoading();
    }
    [SerializeField] private GameObject loadingImage;

    public void ShowLoading()
    {
        loadingImage.SetActive(true);
    }

    public void HideLoading()
    {
        loadingImage.SetActive(false);
    }
}
