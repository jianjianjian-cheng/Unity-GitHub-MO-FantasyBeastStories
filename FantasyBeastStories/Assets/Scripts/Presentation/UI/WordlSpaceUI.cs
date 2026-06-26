using System.Collections;
using System.Collections.Generic;
using Domain.Services;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Presentation.UI
{
    public class WordlSpaceUI : MonoBehaviour
    {
        [SerializeField] public Image isMineIcon;
        [SerializeField] public Text playerName;
        [SerializeField] public Text isReadyIcon;

        private int _ownerActorNumber = -1;

        void Start()
        {
            _ownerActorNumber = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this);
        }

        void Update()
        {
            if (NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
            {
                isMineIcon.gameObject.SetActive(true);
                playerName.text = " ";
            }
            else
            {
                isMineIcon.gameObject.SetActive(false);
                playerName.text = NetworkServiceLocator.ObjectService.GetOwnerNickname(this);
            }
            //名称一直朝向摄像机
            Vector3 direction = transform.position - Camera.main.transform.position;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (NetworkServiceLocator.IsInitialized)
                NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged += OnPlayerPropertyChanged;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (NetworkServiceLocator.IsInitialized)
                NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged -= OnPlayerPropertyChanged;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex > 1)
            {
                isReadyIcon.gameObject.SetActive(false);
                isMineIcon.gameObject.SetActive(false);
            }
        }

        public void UpDatePlayerName(string name)
        {
            if (playerName == null) return;
            if (NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
            {
                playerName.text = " ";
            }
            else
            {
                playerName.text = name;
            }
        }

        private void OnPlayerPropertyChanged(int actorNumber, string key, object value)
        {
            if (actorNumber != _ownerActorNumber)
                return;

            if (key == "PlayerName")
            {
                string newName = value as string ?? "";
                UpDatePlayerName(newName);
            }
            else if (key == "PlayerReady")
            {
                bool isReady = value is bool b && b;
                isReadyIcon.text = isReady ? "已就绪" : "未就绪";
            }
        }
    }
}