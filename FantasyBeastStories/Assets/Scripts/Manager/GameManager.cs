using System.Collections;
using System.Collections.Generic;
using Other;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using Cinemachine;
using DG.Tweening;
using System;
using Photon.Pun;

namespace Manager
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int sceneIndex = 2;
        private Button startButton;
        private GameObject CharactorPanel;
        private Volume PostProcessVolume;
        public Button lobbyButton;
        private Button characterButton;
        private Button magicButton;
        private Button RuneButton;
        private Sprite selectedButtonImage;
        private Sprite defaultButtonImage;
        [SerializeField] private List<GameObject> spawnPoints = new List<GameObject>(); // 生成点列表
        //静态全局变量isTest，控制是否进入测试模式
        public static bool isTest; // 是否测试模式
        public static bool isStayLobby = true; // 是否在大厅lobby场景
        [SerializeField] private bool isTestInspector; // 在Inspector面板中设置的测试模式
        public static GameManager instance;
        void Awake()
        {
            isTest = isTestInspector;
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
            Intilize();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Intilize()
        {
            PostProcessVolume = GameObject.Find("PostProcessVolume").GetComponent<Volume>();
            CharactorPanel = Launcher.instance.GetInactiveObjectByName("CharactorPanel");
            selectedButtonImage = Resources.Load<Sprite>("UI/SelectedButton");
            defaultButtonImage = Resources.Load<Sprite>("UI/DefaultButton");
            lobbyButton = GameObject.Find("LobbyButton").GetComponent<Button>();
            characterButton = GameObject.Find("CharactorButton").GetComponent<Button>();
            magicButton = GameObject.Find("MagicButton").GetComponent<Button>();
            RuneButton = GameObject.Find("RuneButton").GetComponent<Button>();
            startButton = GameObject.Find("StartButton").GetComponent<Button>();

            //设置默认选中大厅按钮
            SetButtonSelected(lobbyButton);
            lobbyButton.onClick.AddListener(LobbyButtonOnClick);
            characterButton.onClick.AddListener(CharacterButtonOnClick);
            magicButton.onClick.AddListener(MagicButtonOnClick);
            RuneButton.onClick.AddListener(RuneButtonOnClick);
            startButton.onClick.AddListener(StartButtonOnClick);
            FindSpawnPoints();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetButtonSelected(lobbyButton);
                PostProcessVolume.weight = 0f; // 调整后处理效果的权重，例如模糊效果
                HideCharactorPanel();
            }
        }

        private void StartButtonOnClick()
        {
            if (isStayLobby)
            {
                StartCoroutine(DelaySwitchScene());
                isStayLobby = false;
            }
        }

        private IEnumerator DelaySwitchScene()
        {
            LoadingCanvas.instance.ShowLoading();
            yield return new WaitForSeconds(1.5f);
            spawnPoints.Clear();
            PhotonNetwork.LoadLevel(sceneIndex);
        }

        private void LobbyButtonOnClick()
        {
            SetButtonSelected(lobbyButton);
            PostProcessVolume.weight = 0f; // 调整后处理效果的权重，例如模糊效果
            HideCharactorPanel();
        }

        private void CharacterButtonOnClick()
        {
            SetButtonSelected(characterButton);
            PostProcessVolume.weight = 1f; // 调整后处理效果的权重，例如模糊效果
            ShowCharactorPanel();
        }

        private void MagicButtonOnClick()
        {
            SetButtonSelected(magicButton);
            HideCharactorPanel();
        }

        private void RuneButtonOnClick()
        {
            SetButtonSelected(RuneButton);
            HideCharactorPanel();
        }

        private void ShowCharactorPanel()
        {
            if (CharactorPanel == null)
                return;

            CharactorPanel.SetActive(true);
            RectTransform panelRect = CharactorPanel.GetComponent<RectTransform>();
            if (panelRect == null)
                return;

            panelRect.DOKill();
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, -Screen.height);
            panelRect.DOAnchorPosY(-180.34f, 0.6f).SetEase(Ease.OutBack);
        }

        private void HideCharactorPanel()
        {
            if (CharactorPanel == null)
                return;

            RectTransform panelRect = CharactorPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                CharactorPanel.SetActive(false);
                return;
            }

            panelRect.DOKill();
            panelRect.DOAnchorPosY(-Screen.height, 0.3f).SetEase(Ease.InBack).OnComplete(() => CharactorPanel.SetActive(false));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindSpawnPoints();
        }

        public void FindSpawnPoints()
        {
            GameObject[] spawnPointsList = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPointsList = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawnPointsList.Length == 0)
            {
                return;
            }
            foreach (GameObject spawnPoint in spawnPointsList)
            {
                if (!spawnPoints.Contains(spawnPoint)
                && spawnPoint.GetComponent<SpawnPoint>() != null
                && spawnPoint != null)
                    spawnPoints.Add(spawnPoint);
            }
        }

        public GameObject GetEmptySpawnPoint()
        {
            foreach (GameObject spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                {
                    Debug.LogWarning("生成点列表中有空引用");
                    spawnPoints.Remove(spawnPoint);
                    return null;
                }
                if (spawnPoint.GetComponent<SpawnPoint>() == null)
                {
                    Debug.LogWarning($"生成点 '{spawnPoint.name}' 没有 SpawnPoint 组件");
                    spawnPoints.Remove(spawnPoint);
                    return null;
                }
                if (spawnPoint.GetComponent<SpawnPoint>().isEmpty)
                {
                    Debug.Log("返回空闲的生成点: " + spawnPoint.name);
                    return spawnPoint;
                }
            }
            Debug.LogWarning("没有空闲的生成点了");
            return null;
        }

        //设置按钮为选中状态，其他按钮为正常
        public void SetButtonSelected(Button button)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            lobbyButton.interactable = button != lobbyButton;
            characterButton.interactable = button != characterButton;
            magicButton.interactable = button != magicButton;
            RuneButton.interactable = button != RuneButton;
            //设置图片
            lobbyButton.image.sprite = button == lobbyButton ? selectedButtonImage : defaultButtonImage;
            characterButton.image.sprite = button == characterButton ? selectedButtonImage : defaultButtonImage;
            magicButton.image.sprite = button == magicButton ? selectedButtonImage : defaultButtonImage;
            RuneButton.image.sprite = button == RuneButton ? selectedButtonImage : defaultButtonImage;
        }
    }
}
