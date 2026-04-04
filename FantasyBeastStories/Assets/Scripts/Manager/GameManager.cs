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
        private Button MassionButton;
        private Button runeButton;
        private GameObject RunePanel;
        private Text nameUIText;
        public int sceneIndex = 2;
        private Button startButton;
        private bool isReady = false;
        private GameObject CharactorPanel;
        private Volume PostProcessVolume;
        public Button lobbyButton;
        private Button characterButton;
        private Button RuneButton_1;
        private Button RuneButton_2;
        private Sprite selectedButtonImage;
        private Sprite defaultButtonImage;
        [SerializeField] GameObject[] spawnPoints = { }; // 生成点列表
        //静态全局变量isTest，控制是否进入测试模式
        public static bool isTest = false; // 是否测试模式
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
            MassionButton = GameObject.Find("MassionButton").GetComponent<Button>();
            runeButton = GameObject.Find("RuneButton").GetComponent<Button>();
            RunePanel = Launcher.instance.GetInactiveObjectByName("RunePanel");
            nameUIText = GameObject.Find("NameUIText").GetComponent<Text>();
            PostProcessVolume = GameObject.Find("PostProcessVolume").GetComponent<Volume>();
            CharactorPanel = Launcher.instance.GetInactiveObjectByName("CharactorPanel");
            selectedButtonImage = Resources.Load<Sprite>("UI/SelectedButton");
            defaultButtonImage = Resources.Load<Sprite>("UI/DefaultButton");
            lobbyButton = GameObject.Find("LobbyButton").GetComponent<Button>();
            characterButton = GameObject.Find("CharactorButton").GetComponent<Button>();
            RuneButton_1 = GameObject.Find("RuneButton_1").GetComponent<Button>();
            RuneButton_2 = GameObject.Find("RuneButton_2").GetComponent<Button>();
            startButton = GameObject.Find("StartButton").GetComponent<Button>();

            //设置默认选中大厅按钮
            SetButtonSelected(lobbyButton);
            lobbyButton.onClick.AddListener(LobbyButtonOnClick);
            characterButton.onClick.AddListener(CharacterButtonOnClick);
            RuneButton_1.onClick.AddListener(Rune_1ButtonOnClick);
            RuneButton_2.onClick.AddListener(Rune_2ButtonOnClick);
            startButton.onClick.AddListener(StartButtonOnClick);
            runeButton.onClick.AddListener(RuneButtonOnClick);

            FindSpawnPoints();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetButtonSelected(lobbyButton); // 调整后处理效果的权重，例如模糊效果
                HideCharactorPanel();
                HideRunePanel();
            }
        }

        private void StartButtonOnClick()
        {
            if (!isStayLobby || isReady)
            {
                return;
            }

            isReady = true;
            startButton.interactable = false;
            Text buttonText = startButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "已就绪";
            }

            if (Launcher.instance != null)
            {
                Launcher.instance.SetLocalReady(true);
            }
        }

        private void LobbyButtonOnClick()
        {
            SetButtonSelected(lobbyButton);
            PostProcessVolume.weight = 0f; // 调整后处理效果的权重，例如模糊效果
            HideCharactorPanel();
            HideRunePanel();
        }

        private void CharacterButtonOnClick()
        {
            HideRunePanel();
            SetButtonSelected(characterButton); // 调整后处理效果的权重，例如模糊效果
            ShowCharactorPanel();
        }

        private void RuneButtonOnClick()
        {
            SetButtonSelected(runeButton);
            HideCharactorPanel();
            ShowRunePanel();
        }

        private void Rune_1ButtonOnClick()
        {
            HideCharactorPanel();
            ShowRunePanel();
        }

        private void Rune_2ButtonOnClick()
        {
            HideCharactorPanel();
            ShowRunePanel();
        }

        private void ShowRunePanel()
        {
            if (RunePanel == null)
                return;
            PostProcessVolume.weight = 1f;
            RunePanel.SetActive(true);
            RectTransform panelRect = RunePanel.GetComponent<RectTransform>();
            if (panelRect == null)
                return;

            panelRect.DOKill();
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, Screen.height);
            panelRect.DOAnchorPosY(-185.53f, 0.6f).SetEase(Ease.OutBack);
        }

        private void HideRunePanel()
        {
            if (RunePanel == null)
                return;

            RectTransform panelRect = RunePanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                RunePanel.SetActive(false);
                return;
            }
            PostProcessVolume.weight = 0f;
            panelRect.DOKill();
            panelRect.DOAnchorPosY(Screen.height, 0.3f).SetEase(Ease.InBack).OnComplete(() => RunePanel.SetActive(false));
        }

        private void ShowCharactorPanel()
        {
            if (CharactorPanel == null)
                return;
            PostProcessVolume.weight = 1f;
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
            PostProcessVolume.weight = 0f;
            panelRect.DOKill();
            panelRect.DOAnchorPosY(-Screen.height, 0.3f).SetEase(Ease.InBack).OnComplete(() => CharactorPanel.SetActive(false));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindSpawnPoints();
        }

        public void FindSpawnPoints()
        {
            GameObject[] spawnPointsArray = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPointsArray = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints = new GameObject[spawnPointsArray.Length];
            for (int i = 0; i < spawnPointsArray.Length; i++)
            {
                if (spawnPointsArray[i] != null
                && spawnPointsArray[i].GetComponent<SpawnPoint>() != null)
                    spawnPoints[i] = spawnPointsArray[i];
            }
        }

        public GameObject GetEmptySpawnPoint()
        {
            foreach (GameObject spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                {
                    Debug.LogWarning("生成点列表中有空引用");
                    return null;
                }
                if (spawnPoint.GetComponent<SpawnPoint>() == null)
                {
                    Debug.LogWarning($"生成点 '{spawnPoint.name}' 没有 SpawnPoint 组件");
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
            runeButton.interactable = button != runeButton;
            MassionButton.interactable = button != MassionButton;
            //设置图片
            lobbyButton.image.sprite = button == lobbyButton ? selectedButtonImage : defaultButtonImage;
            characterButton.image.sprite = button == characterButton ? selectedButtonImage : defaultButtonImage;
            runeButton.image.sprite = button == runeButton ? selectedButtonImage : defaultButtonImage;
            MassionButton.image.sprite = button == MassionButton ? selectedButtonImage : defaultButtonImage;
        }
    }
}
