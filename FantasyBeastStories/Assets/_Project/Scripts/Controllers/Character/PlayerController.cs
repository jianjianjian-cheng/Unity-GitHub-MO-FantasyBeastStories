using System.Collections;
using System.Collections.Generic;
using Controllers.Character;
using Controllers.CardData;
using Cinemachine;
using Core;
using Core.Channels.Player;
using Controllers.Player;
using Controllers.Rune;
using Controllers.Services;
using UI.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;

namespace Controllers.Character
{
  public class PlayerController : MonoBehaviour
  {
    [Header("纯数据")]
    [SerializeField]
    protected PlayerMovementData movementData;

    [SerializeField]
    protected PlayerAttributeConfigSO playerAttributeConfig;

    [SerializeField]
    protected bool isOnlyShow = false; // 是否只为显示角色而不用于其他操作

    [SerializeField]
    protected GameObject virtualCamera; // 虚拟摄像机组件

    [Header("移动设置")]
    [SerializeField]
    protected Rigidbody rb; // 物理组件

    [SerializeField]
    protected Animator animator; // 动画组件

    [Header("输入设置")]
    // 输入读取统一由 PlayerInputHandler 静态类处理

    [Header("旋转设置")]
    protected AttributePlayerBase attributePlayer; // 玩家属性组件

    [SerializeField]
    protected bool isInLobby; // 是否在大厅场景

    [SerializeField]
    protected GameObject isReadyPanel; // 准备界面
    int localActorNumber; // 本地玩家ActorNumber
    int sceneIndex; // 场景索引


    protected PlayerInputHandler playerInputHandler;

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
      if (playerAttributeConfig == null)
        playerAttributeConfig = Resources.Load<PlayerAttributeConfigSO>("Config/PlayerAttributeConfig");
      attributePlayer = new AttributePlayerBase(playerAttributeConfig);
      attributePlayer.SetMoveSpeed(movementData.moveSpeed);

      playerInputHandler = new PlayerInputHandler();
    }

    protected virtual void Start()
    {
      // 重新读取 IsStayLobby — Launcher.OnSceneLoaded 在 Awake 之后、Start 之前将其设为 false
      isInLobby = EventChannelLocator.MainContainer.gameSettings.IsStayLobby;

      // 应用符文效果（仅在游戏场景，非大厅）— 放在 Start 确保场景切换标志已更新
      if (!isInLobby)
        RuneEffectApplier.ApplyEquippedRunes(attributePlayer);

      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
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
      if (GamePauseManager.isPaused)
        return;
      if (isOnlyShow)
      {
        return; // 如果只显示角色，不处理输入
      }
      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject) && !EventChannelLocator.MainContainer.gameSettings.IsTest)
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
      if (GamePauseManager.isPaused)
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
      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
      {
        return; // 只处理本地玩家的输入和动画
      }
      // 物理移动
      MoveCharacter();
    }

    protected virtual void OnEnable()
    {
      localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();

      // 注册玩家GameObject到PlayerManager（供敌人追踪使用，所有客户端都需要注册）
      if (PlayerManager.instance != null)
        EventChannelLocator.MainContainer.playerQueryChannel.Raise(new PlayerQueryData(PlayerQueryType.RegisterPlayerObject) { playerObject = gameObject });

      // 使用该 GameObject 的 Owner ActorNumber 注册属性，避免非拥有者覆盖本地玩家缓存
      // 说明：同一客户端上所有 PlayerController 共用 localActorNumber，
      // 若用 localActorNumber 注册非拥有者的属性，会覆盖拥有者的缓存条目，
      // 导致卡牌效果更新了拥有者属性后，Tab 面板读取的是被覆盖的旧值。
      int ownerActorNumber = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this);
      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
          new PlayerAttributeData(PlayerAttributeQueryType.RegisterAttribute, AttributeKeyConst.Main, attributePlayer)
          { playerId = ownerActorNumber.ToString(), attributeName = AttributeKeyConst.Main }
      );
      EventChannelLocator.MainContainer.playerDamageEventChannel.RegisterListener(OnDamageReceived);

      // 所有角色共用的频道注册
      EventChannelLocator.MainContainer.cardReceivedChannel.RegisterListener(OnApplicationCard);
      EventChannelLocator.MainContainer.skillQueryChannel.RegisterListener(OnSkillQuery);
      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected virtual void OnDisable()
    {
      // 从PlayerManager注销玩家GameObject
      if (PlayerManager.instance != null)
        EventChannelLocator.MainContainer.playerQueryChannel.Raise(new PlayerQueryData(PlayerQueryType.UnregisterPlayerObject) { playerObject = gameObject });

      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
          new PlayerAttributeData(PlayerAttributeQueryType.UnregisterAttribute)
          { playerId = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this).ToString(), attributeName = AttributeKeyConst.Main }
      );
      EventChannelLocator.MainContainer.playerDamageEventChannel.UnregisterListener(OnDamageReceived);

      // 所有角色共用的频道注销
      EventChannelLocator.MainContainer.cardReceivedChannel.UnregisterListener(OnApplicationCard);
      EventChannelLocator.MainContainer.skillQueryChannel.UnregisterListener(OnSkillQuery);
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    protected virtual void HandleInput()
    {
      // 驱动输入处理器更新原始输入值
      playerInputHandler.Update();

      float h = playerInputHandler.Horizontal;
      float v = playerInputHandler.Vertical;

      movementData.movementDirection = new Vector3(h, 0f, v).normalized;
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
      var playerService = NetworkServiceLocator.PlayerService;
      if (playerService.IsOwnerOf(gameObject) && playerService.IsConnectedAndInRoom)
      {
        ClearSpawnPointOccupation();
      }
      // 在 OnDestroy 中也进行清理，确保万无一失
      if (EventChannelLocator.MainContainer != null)
      {
        EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
            new PlayerAttributeData(PlayerAttributeQueryType.UnregisterAttribute)
            { playerId = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this).ToString(), attributeName = AttributeKeyConst.Main }
        );
      }
    }

    protected virtual void ClearSpawnPointOccupation()
    {
      var playerService = NetworkServiceLocator.PlayerService;
      if (!playerService.IsConnectedAndInRoom)
      {
        Debug.LogWarning("[PlayerController] Photon 已断开连接，跳过清理生成点属性");
        return;
      }
      object spawnPointObj = playerService.GetCustomProperty("CurrentSpawnPoint");
      if (spawnPointObj != null)
      {
        int spawnPointId = (int)spawnPointObj;
        var spawnService = DomainServiceLocator.Get<ISpawnPointService>();
        ISpawnPoint sp = spawnService?.GetSpawnPointById(spawnPointId);
        // sp 可能已被场景卸载销毁（场景切换时 SpawnPoint 先于 DontDestroyOnLoad 对象销毁）
        // 对 Unity 对象必须用 MonoBehaviour 转换判断，不能用 Equals(null)
        if (sp != null && (sp as MonoBehaviour) != null && sp.GetOccupiedByPlayer() == playerService.GetLocalActorNumber())
        {
          sp.ForceRelease();
        }

        // 清除玩家属性
        playerService.SetCustomProperty("CurrentSpawnPoint", null);
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
        NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "NoticeOtherPlayerDamage",
            NetworkTarget.Others,
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
      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
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

    // ---- 静态 Handler 方法（供 DomainRpcBridge 调用） ----

    /// <summary>
    /// 由 DomainRpcBridge.RPC_SyncPlayerElement 调用
    /// </summary>
    public static void HandleSyncPlayerElement(int actorNumber, int elementInt)
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

    // 同步元素到所有客户端
    protected virtual void SyncElementToAll(Element element)
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
        return;

      NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "RPC_SyncPlayerElement",
            NetworkTarget.All,
            NetworkServiceLocator.PlayerService.GetLocalActorNumber(),
            (int)element
        );
    }

    protected virtual void OnApplicationCard(CardConfigBase card)
    {
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
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
          attributePlayer.AddAttackPower(card.Value);
          break;
        case "饱满的生命":
          attributePlayer.AddMaxHealth(card.Value);
          SetAndChangeHPUI();
          break;
        case "温暖的篝火":
          movementData.healthRecover += card.Value;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "敏锐的直觉":
          attributePlayer.AddCriticalChance(card.Value);
          break;
        case "坚韧的意志":
          attributePlayer.AddDefensePower(card.Value);
          break;
        case "涌动的暗劲":
          attributePlayer.AddCriticalMultiplier(card.Value);
          break;
        case "迅捷的手腕":
          attributePlayer.ReduceAttackInterval(card.Value);
          break;
        case "稚嫩四叶草":
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(new SkillQueryData(SkillQueryType.AddLuckRate, card.Value));
          break;
        //------史诗品质-------
        case "锐利的狼牙":
          attributePlayer.AddAttackPower(card.Value);
          break;
        case "不朽的壁垒":
          attributePlayer.AddDefensePower(card.Value);
          break;
        case "巨人的血脉":
          attributePlayer.AddMaxHealth(card.Value);
          SetAndChangeHPUI();
          break;
        case "涌动的生机":
          movementData.healthRecover += card.Value;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "鹰眼的凝视":
          attributePlayer.AddCriticalChance(card.Value);
          break;
        case "断罪的裁决":
          attributePlayer.AddCriticalMultiplier(card.Value);
          break;
        case "狂乱的舞步":
          attributePlayer.ReduceAttackInterval(card.Value);
          break;
        case "青年四叶草":
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(new SkillQueryData(SkillQueryType.AddLuckRate, card.Value));
          break;
        //------传说品质-------
        case "弑神的魔剑":
          attributePlayer.AddAttackPower(card.Value);
          break;
        case "圣光的庇护":
          attributePlayer.AddDefensePower(card.Value);
          break;
        case "永恒的生命":
          attributePlayer.AddMaxHealth(card.Value);
          SetAndChangeHPUI();
          break;
        case "神愈的圣光":
          movementData.healthRecover += card.Value;
          attributePlayer.SetHealthRecover(movementData.healthRecover);
          break;
        case "必然的邂逅":
          attributePlayer.AddCriticalChance(card.Value);
          break;
        case "终末的号角":
          attributePlayer.AddCriticalMultiplier(card.Value);
          break;
        case "极限的超越":
          attributePlayer.ReduceAttackInterval(card.Value);
          break;
        case "幸运四叶草":
          EventChannelLocator.MainContainer.skillQueryChannel.Raise(new SkillQueryData(SkillQueryType.AddLuckRate, card.Value));
          break;
      }
    }

    /// <summary>
    /// 技能查询回调（由子类重写处理角色专属的查询，如 GetMaxAttackCount）
    /// </summary>
    protected virtual void OnSkillQuery(SkillQueryData data) { }

    /// <summary>
    /// 场景加载完成回调（由子类重写处理角色专属的场景加载逻辑）
    /// </summary>
    protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode) { }

    /// <summary>获取最大攻击次数</summary>
    protected int GetMaxAttackCount()
    {
      return attributePlayer.GetMaxAttackCount();
    }
  }
}