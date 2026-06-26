using System.Collections;
using System.Collections.Generic;
using Domain.Character.Attribute;
using Domain.CardData;
using Cinemachine;
using Domain.Event;
using Domain.Event.Channels.Player;
using Domain.Player;
using Presentation.Other;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Application;

namespace Domain.Character
{
  public class PlayerController : MonoBehaviourPun
  {
    [Header("纯数据")]
    [SerializeField]
    protected PlayerMovementData movementData;

    [SerializeField]
    protected bool isOnlyShow = false; // 是否只为显示角色而不用于其他操作

    [SerializeField]
    protected GameObject virtualCamera; // 虚拟摄像机组件

    [Header("移动设置")]
    [SerializeField]
    protected Rigidbody rb; // 物理组件

    [SerializeField]
    protected Animator animator; // 动画组件

    [Header("旋转设置")]
    protected AttributePlayerBase attributePlayer; // 玩家属性组件

    [SerializeField]
    protected bool isInLobby; // 是否在大厅场景

    [SerializeField]
    protected GameObject isReadyPanel; // 准备界面
    int localActorNumber; // 本地玩家ActorNumber
    int sceneIndex; // 场景索引

    // 公开属性：外部通过该属性访问生成点索引（底层数据存储在 movementData 中）
    public int spawnPointIndex
    {
      get => movementData.spawnPointIndex;
      set => movementData.spawnPointIndex = value;
    }

    protected virtual void Awake()
    {
      movementData = new PlayerMovementData();
      isInLobby = EventChannelLocator.MainContainer.gameSettings.IsStayLobby;
      attributePlayer = new AttributePlayerBase(300f, 0f, 300f, movementData.moveSpeed, 1.2f, 0.1f);
    }

    protected virtual void Start()
    {
      if (!photonView.IsMine)
      {
        return; // 只处理本地玩家的输入和动画
      }
      if (isInLobby)
      {
        isReadyPanel.SetActive(true); // 显示准备界面
      }
      else
      {
        isReadyPanel.SetActive(false); // 隐藏准备界面
      }
      movementData.moveSpeed = attributePlayer.GetMoveSpeed();
      // 获取或添加Rigidbody组件
      if (rb == null)
      {
        rb = gameObject.GetComponent<Rigidbody>();
      }

      if (rb != null)
      {
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = !isOnlyShow;
      }
      if (!isOnlyShow)
        rb.useGravity = true; // 启用重力
      if (animator == null)
      {
        animator = GetComponent<Animator>();
      }
      sceneIndex = SceneManager.GetActiveScene().buildIndex;
      SetAndChangeHPUI();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsPaused)
        return;
      if (isOnlyShow)
      {
        return; // 如果只显示角色，不处理输入
      }
      if (!photonView.IsMine && photonView != null && !EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        return; // 只处理本地玩家的输入和动画
      }
      if (EventChannelLocator.MainContainer.gameSettings.IsStayLobby)
      {
        return; // 如果在大厅场景，不处理输入
      }
      HealthRecover();
      HandleInput();
    }

    protected virtual void FixedUpdate()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsPaused)
      {
        // 暂停时停止移动和旋转
        if (rb != null)
        {
          rb.velocity = Vector3.zero;
        }
        // 重置动画状态
        if (animator != null)
        {
          animator.SetBool("isRun", false);
        }
        return;
      }
      if (!photonView.IsMine && photonView != null)
      {
        return; // 只处理本地玩家的输入和动画
      }
      // 物理移动
      MoveCharacter();
    }

    protected virtual void OnEnable()
    {
      localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

      // 注册玩家GameObject到PlayerManager（供敌人追踪使用，所有客户端都需要注册）
      if (PlayerManager.instance != null)
        EventChannelLocator.MainContainer.playerQueryChannel.Raise(new PlayerQueryData(PlayerQueryType.RegisterPlayerObject) { playerObject = gameObject });

      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
          new PlayerAttributeData(PlayerAttributeQueryType.RegisterAttribute, AttributeKeyConst.Main, attributePlayer)
          { playerId = localActorNumber.ToString(), attributeName = AttributeKeyConst.Main }
      );
      EventChannelLocator.MainContainer.playerDamageEventChannel.RegisterListener(OnDamageReceived);
    }

    protected virtual void OnDisable()
    {
      // 从PlayerManager注销玩家GameObject
      if (PlayerManager.instance != null)
        EventChannelLocator.MainContainer.playerQueryChannel.Raise(new PlayerQueryData(PlayerQueryType.UnregisterPlayerObject) { playerObject = gameObject });

      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
          new PlayerAttributeData(PlayerAttributeQueryType.UnregisterAttribute)
          { playerId = localActorNumber.ToString(), attributeName = AttributeKeyConst.Main }
      );
      EventChannelLocator.MainContainer.playerDamageEventChannel.UnregisterListener(OnDamageReceived);
    }

    protected virtual void HandleInput()
    {
      // 获取水平输入（A/D或左右箭头）
      float horizontal = Input.GetAxis("Horizontal");
      // 获取垂直输入（W/S或上下箭头）
      float vertical = Input.GetAxis("Vertical");
      // 计算移动方向（基于世界坐标）
      movementData.movementDirection = new Vector3(horizontal, 0f, vertical).normalized;
    }

    protected virtual void MoveCharacter()
    {
      // 确保不受外力旋转影响
      rb.angularVelocity = Vector3.zero;

      // 计算移动向量
      Vector3 moveVelocity = movementData.movementDirection * movementData.moveSpeed;
      movementData.isRun = movementData.movementDirection != Vector3.zero;
      animator.SetBool("isRun", movementData.isRun);

      // 保持Y轴速度不变（重力影响）
      moveVelocity.y = rb.velocity.y;

      // 应用速度
      rb.velocity = moveVelocity;

      // 旋转逻辑
      if (movementData.movementDirection != Vector3.zero)
      {
        Quaternion targetRotation = Quaternion.LookRotation(movementData.movementDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            movementData.rotationSpeed * UnityEngine.Time.fixedDeltaTime
        );
      }
    }

    protected virtual void OnDestroy()
    {
      // 只有在正常游戏中（非退出流程）才清理生成点
      if (
          photonView.IsMine
          && PhotonNetwork.InRoom
          && PhotonNetwork.NetworkClientState == ClientState.Joined
      )
      {
        ClearSpawnPointOccupation();
      }
      // 在 OnDestroy 中也进行清理，确保万无一失
      if (EventChannelLocator.MainContainer != null)
      {
        EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
            new PlayerAttributeData(PlayerAttributeQueryType.UnregisterAttribute)
            { playerId = localActorNumber.ToString(), attributeName = AttributeKeyConst.Main }
        );
      }
    }

    protected virtual void ClearSpawnPointOccupation()
    {
      if (
          !PhotonNetwork.IsConnected
          || PhotonNetwork.NetworkClientState == ClientState.Disconnecting
      )
      {
        Debug.LogWarning("[PlayerController] Photon 已断开连接，跳过清理生成点属性");
        return;
      }
      if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("CurrentSpawnPoint"))
      {
        int spawnPointId = (int)
            PhotonNetwork.LocalPlayer.CustomProperties["CurrentSpawnPoint"];
        SpawnPoint sp = GameManager.instance.GetSpawnPointById(spawnPointId);
        if (sp != null && sp.GetOccupiedByPlayer() == PhotonNetwork.LocalPlayer.ActorNumber)
        {
          sp.ForceRelease();
        }

        // 清除玩家属性
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable()
                {
                    { "CurrentSpawnPoint", null },
                };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
      }
    }

    //触发HP变化事件
    protected virtual void SetAndChangeHPUI()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        return;
      }
      if (sceneIndex > 1)
      {
        EventChannelLocator.MainContainer.hpChangedChannel.Raise(attributePlayer.GetMaxHealth(), attributePlayer.GetCurrentHealth());
        photonView.RPC(
            "NoticeOtherPlayerDamage",
            RpcTarget.Others,
            PlayerManager.instance.GetLocalPlayer().PlayerId.ToString(),
            attributePlayer.GetMaxHealth(),
            attributePlayer.GetCurrentHealth()
        );
      }
      Debug.Log(
          $"[PlayerController] SetAndChangeHPUI - {attributePlayer.GetCurrentHealth()}/{attributePlayer.GetMaxHealth()}"
      );
    }

    //遭受伤害时触发的事件
    protected virtual void OnDamageReceived(EventArgsBase args)
    {
      DamageEventArgs damageEventArgs = args as DamageEventArgs;
      if (damageEventArgs.damgeTarget != gameObject)
      {
        return;
      }
      if (!photonView.IsMine)
        return;
      //向上取整
      int damage = Mathf.CeilToInt(damageEventArgs.baseDamageValue);
      int finalDamage = CalculateFinalDamage(damage);
      Debug.LogWarning($"受到伤害：{finalDamage}");
      // 应用最终伤害
      attributePlayer.Damage(finalDamage);
      // 触发HP变化事件
      SetAndChangeHPUI();
      // 通知其他玩家我受到了伤害
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
        return;
    }

    //根据防御伤害计算最终伤害
    protected virtual int CalculateFinalDamage(int damage)
    {
      return damage - (int)attributePlayer.GetDefensePower();
    }

    //通知其他玩家我受到了伤害，让其更新UI
    [PunRPC]
    protected virtual void NoticeOtherPlayerDamage(
        string playerId,
        float MaxHP,
        float CurrentHP
    )
    {
      // 更新其他玩家的血条数值
      var hpChannel = EventChannelLocator.MainContainer.healthUpdateChannel;
      if (hpChannel != null)
        hpChannel.Raise(HealthUpdateData.OtherPlayer(playerId, MaxHP, CurrentHP));
    }

    // 当玩家断开连接时
    protected virtual void OnApplicationQuit()
    {
      ClearSpawnPointOccupation();
    }

    private float recoverTimer = 0f; // 计时器

    //每秒回复生命值
    protected virtual void HealthRecover()
    {
      if (
          attributePlayer.GetIsDead()
          || attributePlayer.GetCurrentHealth() >= attributePlayer.GetMaxHealth()
      )
      {
        return;
      }
      recoverTimer += UnityEngine.Time.deltaTime;
      if (recoverTimer >= movementData.recoverInterval)
      {
        recoverTimer = 0f;
        //回复生命值
        attributePlayer.AddCurrentHealth(movementData.healthRecover);

        SetAndChangeHPUI();
      }
    }

    protected virtual void SwitchElement(Element element)
    {
      attributePlayer.SetCurrentElement(element);
      SyncElementToAll(element);
    }

    // 添加元素同步 RPC
    [PunRPC]
    protected virtual void RPC_SyncPlayerElement(int actorNumber, int elementInt)
    {
      var query = new PlayerAttributeData(PlayerAttributeQueryType.GetAttributeById)
      { playerId = actorNumber.ToString(), attributeName = AttributeKeyConst.Main };
      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(query);
      AttributePlayerBase playerAttr = query.attribute;
      if (playerAttr != null)
      {
        playerAttr.SetCurrentElement((Element)elementInt);
      }
    }

    // 同步元素到所有客户端
    protected virtual void SyncElementToAll(Element element)
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
        return;

      photonView.RPC(
          "RPC_SyncPlayerElement",
          RpcTarget.All,
          PhotonNetwork.LocalPlayer.ActorNumber,
          (int)element
      );
    }

    protected virtual void OnApplicationCard(CardConfigBase card)
    {
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        if (!photonView.IsMine)
        {
          return;
        }
      }
      Debug.LogWarning("应用了卡牌效果：" + card.Name + ":" + card.Content + card.Value);

      switch (card.Name)
      {
        //以下是所有角色公用的卡牌效果
        //------普通品质-------
        case "锋利的短剑":
          attributePlayer.AddAttackPower(8);
          break;
        case "饱满的生命":
          attributePlayer.AddMaxHealth(30);
          SetAndChangeHPUI();
          break;
        case "温暖的篝火":
          movementData.healthRecover += 2;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "敏锐的直觉":
          attributePlayer.AddCriticalChance(6);
          break;
        case "坚韧的意志":
          attributePlayer.AddDefensePower(8);
          break;
        case "涌动的暗劲":
          attributePlayer.AddCriticalMultiplier(10);
          break;
        case "迅捷的手腕":
          attributePlayer.ReduceAttackInterval(10);
          break;
        case "稚嫩四叶草":
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(new SkillQueryData(SkillQueryType.AddLuckRate, 5));
          break;
        //------史诗品质-------
        case "锐利的狼牙":
          attributePlayer.AddAttackPower(20);
          break;
        case "不朽的壁垒":
          attributePlayer.AddDefensePower(18);
          break;
        case "巨人的血脉":
          attributePlayer.AddMaxHealth(80);
          SetAndChangeHPUI();
          break;
        case "涌动的生机":
          movementData.healthRecover += 3;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "鹰眼的凝视":
          attributePlayer.AddCriticalChance(15);
          break;
        case "断罪的裁决":
          attributePlayer.AddCriticalMultiplier(20);
          break;
        case "狂乱的舞步":
          attributePlayer.ReduceAttackInterval(20);
          break;
        case "青年四叶草":
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(new SkillQueryData(SkillQueryType.AddLuckRate, 10));
          break;
        //------传说品质-------
        case "弑神的魔剑":
          attributePlayer.AddAttackPower(40);
          break;
        case "圣光的庇护":
          attributePlayer.AddDefensePower(25);
          break;
        case "永恒的生命":
          attributePlayer.AddMaxHealth(150);
          SetAndChangeHPUI();
          break;
        case "神愈的圣光":
          movementData.healthRecover += 5;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "必然的邂逅":
          attributePlayer.AddCriticalChance(25);
          break;
        case "终末的号角":
          attributePlayer.AddCriticalMultiplier(50);
          break;
        case "极限的超越":
          attributePlayer.ReduceAttackInterval(30);
          break;
        case "幸运四叶草":
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(new SkillQueryData(SkillQueryType.AddLuckRate, 20));
          break;
      }
    }
  }
}