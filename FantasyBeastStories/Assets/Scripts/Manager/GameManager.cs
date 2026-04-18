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
using Events;
using ExitGames.Client.Photon.StructWrapping;

namespace Manager
{
    public class GameManager : MonoBehaviour
    {
        //符文部分
        private Gameobject runeButton;//大厅打开符文面板
        private GameObject RunePanel;//整个符文面板对象
        private List<Gameobject> runeList = new List<Gameobject>(); //符文图标按钮数组
        private Gameobject EquipButton; //符文装备按钮
        private int[] currentEquippedRuneIds = new int[2]; //当前装备的符文ID数组

        //角色选择面板
        private Gameobject nextCharacterButton; //下一个角色按钮
        private Gameobject previousCharacterButton; //上一个角色按钮
        private Gameobject SwitchButton; //切换角色按钮
        private Gameobject CharactorIll; //当前角色
        private int currentCharactorIndex = 0; //当前角色索引

        private Gameobject MassionButton;
        private Text nameUIText;
        public int sceneIndex = 2;
        private Gameobject startButton;
        private bool isReady = false;
        private GameObject CharactorPanel;
        private Volume PostProcessVolume;
        public Gameobject lobbyButton;
        private Gameobject characterButton;
        private Gameobject RuneIcon_1;
        private Gameobject RuneIcon_2;
        private Gameobject selectedRuneIcon;
        private Gameobject selectedRuneListItem;
        private Sprite selectedButtonImage;
        private Sprite defaultButtonImage;
        [SerializeField] GameObject[] spawnPoints = { }; // 生成点列表
        //静态全局变量isTest，控制是否进入测试模式
        public static bool isTest = false; // 是否测试模式
        public static bool isStayLobby = true; // 是否在大厅lobby场景
        [SerializeField] private bool isStayLobbyInspector; // 在Inspector面板中设置的是否在大厅场景
        [SerializeField] private bool isTestInspector; // 在Inspector面板中设置的测试模式
        public static GameManager instance;
        void Awake()
        {
            isStayLobby = isStayLobbyInspector;
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
            if (isTest) return;
            EquipButton = Launcher.instance.GetInactiveObjectByName("EquipButton")?.GetComponent<Gameobject>();
            MassionButton = GameObject.Find("MassionButton").GetComponent<Gameobject>();
            runeButton = GameObject.Find("RuneButton").GetComponent<Gameobject>();
            RunePanel = Launcher.instance.GetInactiveObjectByName("RunePanel");
            nameUIText = GameObject.Find("NameUIText").GetComponent<Text>();
            PostProcessVolume = GameObject.Find("PostProcessVolume").GetComponent<Volume>();
            CharactorPanel = Launcher.instance.GetInactiveObjectByName("CharactorPanel");
            selectedButtonImage = Resources.Load<Sprite>("UI/SelectedButton");
            defaultButtonImage = Resources.Load<Sprite>("UI/DefaultButton");
            lobbyButton = GameObject.Find("LobbyButton").GetComponent<Gameobject>();
            characterButton = GameObject.Find("CharactorButton").GetComponent<Gameobject>();
            RuneIcon_1 = GameObject.Find("RuneIcon_1").GetComponent<Gameobject>();
            RuneIcon_2 = GameObject.Find("RuneIcon_2").GetComponent<Gameobject>();
            startButton = GameObject.Find("StartButton").GetComponent<Gameobject>();

            //角色选择面板
            CharactorIll = Launcher.instance.GetInactiveObjectByName("CharactorIll")?.GetComponent<Gameobject>();
            nextCharacterButton = Launcher.instance.GetInactiveObjectByName("nextCharacterButton")?.GetComponent<Gameobject>();
            previousCharacterButton = Launcher.instance.GetInactiveObjectByName("previousCharacterButton")?.GetComponent<Gameobject>();
            SwitchButton = Launcher.instance.GetInactiveObjectByName("SwitchButton")?.GetComponent<Gameobject>();

            if (SwitchButton == null)
            {
                Debug.LogWarning("未找到 SwitchButton 按钮");
            }
            if (nextCharacterButton == null)
            {
                Debug.LogWarning("未找到 NextCharacterButton 按钮");
            }
            if (previousCharacterButton == null)
            {
                Debug.LogWarning("未找到 PreviousCharacterButton 按钮");
            }

            if (CharactorIll == null)
            {
                Debug.LogWarning("未找到 Charactor 对象");
            }

            //翻转nextCharacterButton的图片
            Image previousCharacterButtonImage = previousCharacterButton.GetComponent<Image>();
            Vector3 scale = previousCharacterButtonImage.transform.localScale;
            scale.x *= -1;
            previousCharacterButtonImage.transform.localScale = scale;
            SwitchCharactor(0); //默认角色为第一个角色
            //添加角色选择按钮的点击事件
            previousCharacterButton.onClick.AddListener(() =>
            {
                int newIndex = currentCharactorIndex - 1;
                if (newIndex < 0)
                    newIndex = 0;
                SwitchCharactor(newIndex);
            });

            nextCharacterButton.onClick.AddListener(() =>
            {
                int newIndex = currentCharactorIndex + 1;
                if (newIndex >= 2)
                    newIndex = 1;
                SwitchCharactor(newIndex);
            });



            //寻找符文插槽
            FindRuneIcons();


            //设置默认选中大厅按钮
            SetButtonSelected(lobbyButton);
            lobbyButton.onClick.AddListener(LobbyButtonOnClick);
            characterButton.onClick.AddListener(CharacterButtonOnClick);
            RuneIcon_1.onClick.AddListener(Rune_1ButtonOnClick);
            RuneIcon_2.onClick.AddListener(Rune_2ButtonOnClick);
            startButton.onClick.AddListener(StartButtonOnClick);
            runeButton.onClick.AddListener(RuneButtonOnClick);
            EquipButton.onClick.AddListener(EquipButtonOnClick);

            for (int i = 0; i < runeList.Count; i++)
            {
                Gameobject runeIcon = runeList[i];
                if (runeIcon == null)
                    continue;

                int index = i;
                runeIcon.onClick.AddListener(() => OnRuneListItemClicked(runeList[index]));
            }

            if (runeList.Count > 0)
            {
                SetRuneListSelected(runeList[0]);
            }

            FindSpawnPoints();

            // 添加RuneIcon的鼠标悬停动画
            AddRuneIconHoverAnimation(RuneIcon_1);
            AddRuneIconHoverAnimation(RuneIcon_2);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetButtonSelected(lobbyButton); // 调整后处理效果的权重，例如模糊效果
                HideCharactorPanel();
                HideRunePanel();
            }

            if (Input.GetMouseButtonDown(0) && selectedRuneIcon != null && CanDeselectRuneIcons() && !IsPointerOverRuneIcon())
            {
                DeselectRuneIcons();
            }
        }

        private void FindRuneIcons()
        {
            for (int i = 1; i <= 2; i++)
            {
                Gameobject runeIcon = Launcher.instance.GetInactiveObjectByName($"RuneSlot_{i}")?.GetComponent<Gameobject>();
                if (runeIcon != null)
                {
                    runeList.Add(runeIcon);
                }
                else
                {
                    Debug.LogWarning($"未找到 RuneIcon_{i} 按钮");
                }
            }
        }

        private void EquipButtonOnClick()
        {
            if (selectedRuneListItem == null)
            {
                Debug.LogWarning("没有选中的符文图标");
                return;
            }

            RuneSlot runeSlot = selectedRuneListItem.GetComponent<RuneSlot>();
            if (runeSlot == null)
            {
                Debug.LogWarning("选中的符文图标没有 RuneSlot 组件");
                return;
            }
            Debug.Log($"装备符文: {runeSlot.RuneName}");
            if (RuneIcon_1.transform.Find("Icon").GetComponent<Image>().sprite != selectedRuneListItem.transform.Find("Icon").GetComponent<Image>().sprite &&
               RuneIcon_2.transform.Find("Icon").GetComponent<Image>().sprite != selectedRuneListItem.transform.Find("Icon").GetComponent<Image>().sprite)
            {
                selectedRuneIcon.transform.Find("Icon").GetComponent<Image>().sprite = selectedRuneListItem.transform.Find("Icon").GetComponent<Image>().sprite;
                //记录当前符文id
                if (selectedRuneIcon == RuneIcon_1)
                {
                    currentEquippedRuneIds[0] = runeSlot.slotId;
                }
                else if (selectedRuneIcon == RuneIcon_2)
                {
                    currentEquippedRuneIds[1] = runeSlot.slotId;
                }
            }
            else
            {
                Debug.LogWarning("符文已装备在另一个插槽上了");
                return;
            }
        }

        private bool CanDeselectRuneIcons()
        {
            return RunePanel == null || !RunePanel.activeSelf;
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
            SetRuneIconSelected(RuneIcon_1);
        }

        private void Rune_2ButtonOnClick()
        {
            HideCharactorPanel();
            ShowRunePanel();
            SetRuneIconSelected(RuneIcon_2);
        }

        private void ShowRunePanel()
        {
            if (RunePanel == null)
                return;
            if (RunePanel.activeSelf)
                return;
            SetRuneIconSelected(RuneIcon_1);
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
            DeselectRuneIcons();

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
        public void SetButtonSelected(Gameobject button)
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

        //设置RuneIcon为选中状态
        private void SetRuneIconSelected(Gameobject button)
        {
            selectedRuneIcon = button;
            RuneIcon_1.image.sprite = (button == RuneIcon_1) ? selectedButtonImage : defaultButtonImage;
            RuneIcon_2.image.sprite = (button == RuneIcon_2) ? selectedButtonImage : defaultButtonImage;

            RuneIcon_1.transform.DOScale(button == RuneIcon_1 ? 1.1f : 1f, 0.2f);
            RuneIcon_2.transform.DOScale(button == RuneIcon_2 ? 1.1f : 1f, 0.2f);
        }

        private void DeselectRuneIcons()
        {
            if (selectedRuneIcon == null)
                return;

            selectedRuneIcon = null;
            RuneIcon_1.image.sprite = defaultButtonImage;
            RuneIcon_2.image.sprite = defaultButtonImage;
            RuneIcon_1.transform.DOScale(1f, 0.2f);
            RuneIcon_2.transform.DOScale(1f, 0.2f);
        }

        private void OnRuneListItemClicked(Gameobject runeIcon)
        {
            SetRuneListSelected(runeIcon);
        }

        private void SetRuneListSelected(Gameobject button)
        {
            selectedRuneListItem = button;
            foreach (Gameobject runeIcon in runeList)
            {
                if (runeIcon == null)
                {
                    Debug.LogWarning("符文列表中有空引用");
                    continue;
                }
                bool selected = runeIcon == button;
                Debug.Log($"设置符文图标 '{runeIcon.name}' 的选中状态: {(selected ? "选中" : "未选中")}");
                runeIcon.transform.DOScale(selected ? 1.1f : 1f, 0.2f);
            }
            //添加点击符文图标后的逻辑显示符文详情
            RuneSlot runeSlot = button.GetComponent<RuneSlot>();
            if (runeSlot != null)
            {
                EventArgsBase eventArgs = new RuneEquipArgs(runeSlot.slotId, runeSlot.RuneName, runeSlot.runePowers, runeSlot.specialPowerName, runeSlot.specialPowerDescription);
                EventManager.instance.TriggerEventComplex(EventNames.RuneInfo, eventArgs);
            }
        }

        private void DeselectRuneListItems()
        {
            if (selectedRuneListItem == null)
                return;

            selectedRuneListItem = null;
            foreach (Gameobject runeIcon in runeList)
            {
                if (runeIcon == null)
                    continue;

                runeIcon.image.sprite = defaultButtonImage;
                runeIcon.transform.DOScale(1f, 0.2f);
            }
        }

        private bool IsPointerOverRuneIcon()
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (RaycastResult result in results)
            {
                if (result.gameObject == RuneIcon_1.gameObject || result.gameObject == RuneIcon_2.gameObject ||
                    result.gameObject.transform.IsChildOf(RuneIcon_1.transform) || result.gameObject.transform.IsChildOf(RuneIcon_2.transform))
                {
                    return true;
                }
            }

            return false;
        }

        //添加RuneIcon的鼠标悬停动画
        private void AddRuneIconHoverAnimation(Gameobject button)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

            // PointerEnter 事件：变大
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { button.transform.DOScale(1.1f, 0.2f); });
            trigger.triggers.Add(entryEnter);

            // PointerExit 事件：如果未选中，则变回原样
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) =>
            {
                if (selectedRuneIcon != button)
                    button.transform.DOScale(1f, 0.2f);
            });
            trigger.triggers.Add(entryExit);
        }
        #region  UI切换角色相关方法区域
        //切换角色
        private void SwitchCharactor(int charactorIndex)
        {
            Image characterImage = CharactorIll.GetComponent<Image>();
            if (characterImage)
            {
                Sprite newSprite = Resources.Load<Sprite>("UI/Characters/" + charactorIndex);
                if (newSprite == null)
                {
                    Debug.LogWarning($"未找到角色图片资源: UI/Characters/{charactorIndex}");
                    return;
                }
                characterImage.sprite = newSprite;
            }
            else
            {
                Debug.LogWarning("CharactorIll对象上没有Image组件");
            }
        }
        #endregion


    }

    public class CharactorIndex
    {
        public const int WiZardBoy = 0;
        public const int LittleRedGirl = 1;
    }
}
