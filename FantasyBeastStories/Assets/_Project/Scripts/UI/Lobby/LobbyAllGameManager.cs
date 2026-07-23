using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Managers;
using UI.Framework.Panel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Core.Audio;

namespace UI.Lobby
{
    public class LobbyAllGameManager : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector vcTimeLine;

        [SerializeField]
        private SceneConfigSO sceneConfig;

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
            //播放大厅音乐
            AudioManager.Instance.PlayBGM("bgm_main_menu");
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
            StartCoroutine(loadScene(sceneConfig != null ? sceneConfig.lobbySceneIndex : 1));
        }

        IEnumerator loadScene(int index)
        {
            yield return new WaitForSeconds(2f);
            yield return StartCoroutine(Loading.Instance.Show());
            yield return new WaitForSeconds(1f);
            AsyncOperation asyn = SceneManager.LoadSceneAsync(index);
            asyn.completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperation operation)
        {
        }
    }
}