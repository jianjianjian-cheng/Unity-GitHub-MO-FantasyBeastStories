using System;
using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Presentation.Lobby
{
    public class LobbyAllGameManager : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector vcTimeLine;
        private GameObject startButton;
        private GameObject exitButton;
        private GameObject optionButton;
        private GameObject gameNameModel;

        private void Start()
        {
            Initilize();
        }

        /// <summary>
        /// 初始化LobbyAllGameManager
        /// </summary>
        public void Initilize()
        {
            vcTimeLine = GameObject.Find("Director").GetComponent<PlayableDirector>();
            startButton = GameObject.Find("StartButton");
            exitButton = GameObject.Find("ExitButton");
            optionButton = GameObject.Find("OptionsButton");
            startButton.GetComponent<Button>().onClick.AddListener(Startbutton);
            exitButton.GetComponent<Button>().onClick.AddListener(Exitbutton);
            optionButton.GetComponent<Button>().onClick.AddListener(Optionsbutton);
            gameNameModel = GameObject.Find("GameNameModel");
        }

        public void Startbutton()
        {
            vcTimeLine.Play();
            sceneChange();
        }

        public void Exitbutton()
        {
            UnityEngine.Application.Quit();
        }

        public void Optionsbutton()
        {
            Debug.Log("Optionsbutton");
        }

        private void sceneChange()
        {
            StartCoroutine(loadScene(1));
        }

        IEnumerator loadScene(int index)
        {
            yield return new WaitForSeconds(2f);
            EventChannelLocator.MainContainer.loadingChannel.Raise(true);
            yield return new WaitForSeconds(2f);
            AsyncOperation asyn = SceneManager.LoadSceneAsync(index);
            asyn.completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperation operation)
        {
            // LoadingCanvas.instance.HideLoading();
            // loadingAnimator.SetBool("FadeIn", false);
        }
    }
}