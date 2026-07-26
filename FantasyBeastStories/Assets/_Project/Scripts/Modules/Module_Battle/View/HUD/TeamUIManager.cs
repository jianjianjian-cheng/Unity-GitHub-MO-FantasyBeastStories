using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Channels.General;
using Controllers.Player;
using Core.Contracts;
using Core.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Managers;

namespace UI
{
  public class TeamUIManager : MonoBehaviour
  {
    #region 单例模式
    

    void Awake()
    {
        ServiceLocator.Register(this);
    }
    #endregion
    private GameObject player1;
    private Text namePlayer1;
    private Slider sliderPlayer1_HP;
    private GameObject player2;
    private Text namePlayer2;
    private Slider sliderPlayer2_HP;
    private GameObject player3;
    private Text namePlayer3;
    private Slider sliderPlayer3_HP;

    //用于绑定血条UI的ID，其他玩家的UI需要根据ID来更新血条数值.分别对应三个玩家(排除本地玩家)
    // 玩家ID与血条的映射
    private Dictionary<string, Slider> otherPlayerSlidersDict =
        new Dictionary<string, Slider>();
    private Dictionary<string, Text> otherPlayerNameTextsDict = new Dictionary<string, Text>();

    //本地玩家的血条UI
    private Slider localPlayerSlider_HP;
    private Text localPlayer_HP_Text;
    private GameObject localPlayerHP_Root;

    //退出游戏按钮
    private Button exitButton;

    void Start()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        Intilize();
      }
    }

    void OnEnable()
    {
      EventChannelLocator.MainContainer.hpChangedChannel.RegisterListener(SetLocalPlayerSlider_HP);
      EventChannelLocator.MainContainer.healthUpdateChannel.RegisterListener(OnHealthUpdate);
      EventChannelLocator.MainContainer.gameActionChannel.RegisterListener(OnGameAction);
      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
      EventChannelLocator.MainContainer.hpChangedChannel.UnregisterListener(SetLocalPlayerSlider_HP);
      EventChannelLocator.MainContainer.healthUpdateChannel.UnregisterListener(OnHealthUpdate);
      EventChannelLocator.MainContainer.gameActionChannel.UnregisterListener(OnGameAction);
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
      ServiceLocator.Unregister<TeamUIManager>();
    }

    private void OnHealthUpdate(HealthUpdateData data)
    {
      if (!data.isLocalPlayer)
      {
        SetOtherPlayerSlider_HP(data.playerId, data.maxHp, data.currentHp);
      }
    }

    private void OnGameAction(GameActionType actionType)
    {
      if (actionType == GameActionType.SyncAllPlayers)
      {
        // 玩家数据同步后刷新其他玩家的 UI
        SetOtherTeamUI();
      }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      if (scene.buildIndex > 1)
      {
        Intilize();
      }
    }

    public void Intilize()
    {
      // exitButton = GameObject.Find("ExitToLobby").GetComponent<Button>();

      // exitButton.onClick.AddListener(() =>
      // {
      //   if (NetworkServiceLocator.PlayerService.IsMasterClient)
      //   {
      //     NetworkServiceLocator.ObjectPoolService.ReturnToLobby();
      //   }
      // });

      localPlayerSlider_HP = GameObject
          .Find("LocalPlayerSlider_HP")
          .GetComponentInChildren<Slider>();
      if (localPlayerSlider_HP == null)
      {
        Debug.LogError("LocalPlayerSlider_HP 未找到");
        return;
      }
      localPlayerHP_Root = localPlayerSlider_HP.transform.parent != null
          ? localPlayerSlider_HP.transform.parent.gameObject
          : localPlayerSlider_HP.gameObject;
      localPlayer_HP_Text = localPlayerSlider_HP.GetComponentInChildren<Text>();
      if (localPlayer_HP_Text == null)
      {
        Debug.LogError("LocalPlayer_HP_Text 未找到");
        return;
      }

      player1 = GameObject.Find("Player1");
      player2 = GameObject.Find("Player2");
      player3 = GameObject.Find("Player3");
      if (player1 == null)
      {
        Debug.LogError("Player1 未找到");
        return;
      }
      if (player2 == null)
      {
        Debug.LogError("Player2 未找到");
        return;
      }
      if (player3 == null)
      {
        Debug.LogError("Player3 未找到");
        return;
      }
      namePlayer1 = player1.GetComponentInChildren<Text>();
      namePlayer2 = player2.GetComponentInChildren<Text>();
      namePlayer3 = player3.GetComponentInChildren<Text>();

      SetUpTeamUI();
    }

    private void SetUpTeamUI()
    {
      SetOtherTeamUI();
    }

    public void SetOtherTeamUI()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
        return;

      // 防止 SyncAllPlayers 事件在 Intilize() 之前触发导致空引用
      if (player1 == null || player2 == null || player3 == null)
        return;
      #region  设置其他玩家的UI可见性
      switch (ServiceLocator.Get<PlayerManager>().PlayerCount)
      {
        case 1:
          player1.SetActive(false);
          player2.SetActive(false);
          player3.SetActive(false);
          break;
        case 2:
          player1.SetActive(true);
          sliderPlayer1_HP = player1.GetComponentInChildren<Slider>();
          player2.SetActive(false);
          player3.SetActive(false);
          break;
        case 3:
          player1.SetActive(true);
          sliderPlayer1_HP = player1.GetComponentInChildren<Slider>();
          player2.SetActive(true);
          sliderPlayer2_HP = player2.GetComponentInChildren<Slider>();
          player3.SetActive(false);
          break;
        case 4:
          player1.SetActive(true);
          sliderPlayer1_HP = player1.GetComponentInChildren<Slider>();
          player2.SetActive(true);
          sliderPlayer2_HP = player2.GetComponentInChildren<Slider>();
          player3.SetActive(true);
          sliderPlayer3_HP = player3.GetComponentInChildren<Slider>();
          break;
      }
      #endregion
      List<PlayerData> allPlayers = ServiceLocator.Get<PlayerManager>().PlayerList;
      // 过滤并设置UI
      var otherPlayers = allPlayers
          .Where(p => p.PlayerId != NetworkServiceLocator.PlayerService.GetLocalUserId())
          .Take(3)
          .ToList();

      Text[] nameTexts = { namePlayer1, namePlayer2, namePlayer3 };
      Slider[] otherPlayersliders = { sliderPlayer1_HP, sliderPlayer2_HP, sliderPlayer3_HP };

      for (int i = 0; i < nameTexts.Length; i++)
      {
        if (i < otherPlayers.Count && !string.IsNullOrEmpty(otherPlayers[i].PlayerName))
        {
          string playerName = otherPlayers[i].PlayerName;
          int maxLength = Mathf.Min(playerName.Length, 6);
          nameTexts[i].text = playerName.Substring(0, maxLength);

          otherPlayerNameTextsDict[otherPlayers[i].PlayerId] = nameTexts[i];
          otherPlayerSlidersDict[otherPlayers[i].PlayerId] = otherPlayersliders[i];
        }
        else
        {
          nameTexts[i].text = i < otherPlayers.Count ? "未命名" : "";
        }
      }
    }

    /// <summary>隐藏本地玩家血条UI（死亡时调用）</summary>
    public void HideLocalPlayerHP()
    {
      if (localPlayerHP_Root != null)
        localPlayerHP_Root.SetActive(false);
    }

    //设置UI血条的数值
    public void SetLocalPlayerSlider_HP(float MaxHP, float CurrentHP)
    {
      if (localPlayerSlider_HP == null)
      {
        return;
      }
      localPlayerSlider_HP.maxValue = 1;
      localPlayerSlider_HP.value = CurrentHP / MaxHP;
      if (localPlayer_HP_Text != null)
      {
        localPlayer_HP_Text.text = $"{CurrentHP}/{MaxHP}";
      }
    }

    // 设置其他玩家的血条数值,在玩家脚本中RPC方法中调用
    public void SetOtherPlayerSlider_HP(string playerId, float MaxHP, float CurrentHP)
    {
      if (otherPlayerSlidersDict.ContainsKey(playerId))
      {
        Slider slider = otherPlayerSlidersDict[playerId];
        slider.maxValue = 1;
        slider.value = CurrentHP / MaxHP;
        Debug.Log($"更新玩家 {playerId} 的血条: {CurrentHP}/{MaxHP}");
      }
      else
      {
        Debug.LogWarning($"未找到玩家 {playerId} 的血条绑定");
      }
    }
  }
}
