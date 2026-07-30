using System.Collections;
using System.Collections.Generic;
using Controllers.Network;
using Controllers.Character;
using Controllers.Card;
using Core;
using Core.SharedModel;
using Core.Channels.Player;
using Controllers.Player;
using Controllers.Rune;
using Core.Contracts;
using Core.Network;
using UI.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UI;
using Controllers.Battle;

namespace Controllers.Character
{
  public class PlayerController : MonoBehaviour, ICardEffectContext, IPunInstantiateMagicCallback
  {
    [Header("纯数据")]
    [SerializeField]
    protected PlayerMovementData movementData;

    protected PlayerAttributeConfigSO playerAttributeConfig;

    [SerializeField]
    protected bool isOnlyShow = false;

    [SerializeField]
    protected GameObject virtualCamera;

    [SerializeField]
    protected SpectatorCameraController spectatorCameraController;

    [SerializeField]
    private string deathEffectAddress = "Level1_Player_PlayerDeath";
    private GameObject _deathEffectPrefab;

    [Header("移动设置")]
    [SerializeField]
    protected Rigidbody rb;

    [SerializeField]
    protected Animator animator;

    [SerializeField]
    protected bool isInLobby;

    [SerializeField]
    protected GameObject isReadyPanel;
    int localActorNumber;
    int sceneIndex;

    protected PlayerInputHandler playerInputHandler;

    // ==================== Model ====================
    /// <summary>玩家模型实例（纯 C#，可单测）</summary>
    public PlayerModel Model { get; protected set; }

    // ==================== Lua bridge ====================
    private HeroLuaBridge _luaBridge;

    /// <summary>兼容旧代码：直接访问 attributePlayer</summary>
    protected AttributePlayerBase attributePlayer => Model?.Attributes;

    public IReadOnlyCollection<Element> GetUnlockedElements() => Model?.UnlockedElements;
    public void AddUnlockedElement(Element element) => Model?.AddUnlockedElement(element);

    public int spawnPointIndex
    {
      get => movementData.spawnPointIndex;
      set => movementData.spawnPointIndex = value;
    }

    protected virtual void Awake()
    {
      movementData = new PlayerMovementData();
      isInLobby = EventChannelLocator.MainContainer.gameSettings.IsStayLobby;
      playerInputHandler = new PlayerInputHandler();

      Model = new PlayerModel(movementData);
    }

    protected virtual void Start()
    {
      if (string.IsNullOrEmpty(_characterName))
      {
        var pv = GetComponent<Photon.Pun.PhotonView>();
        var data = pv?.InstantiationData;
        if (data != null && data.Length > 0)
          InitializeCharacter((string)data[0]);
      }

      if (!string.IsNullOrEmpty(deathEffectAddress))
        _deathEffectPrefab = Core.AssetLoader.TryLoadAsset<GameObject>(deathEffectAddress);

      isInLobby = EventChannelLocator.MainContainer.gameSettings.IsStayLobby;

      if (!isInLobby)
        RuneEffectApplier.ApplyEquippedRunes(Model.Attributes);

      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
        return;

      if (isInLobby)
        isReadyPanel.SetActive(true);
      else
        isReadyPanel.SetActive(false);

      movementData.moveSpeed = Model.Attributes.GetMoveSpeed();

      if (rb == null) rb = gameObject.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = !isOnlyShow;
      }
      if (!isOnlyShow && rb != null)
        rb.useGravity = true;

      if (animator == null) animator = GetComponent<Animator>();
      sceneIndex = SceneManager.GetActiveScene().buildIndex;
      SetAndChangeHPUI();

      if (_luaBridge == null && !string.IsNullOrEmpty(_characterName))
      {
        _luaBridge = new HeroLuaBridge(_characterName);
        CharacterAssetLoader.LoadCharacterAssets(this, _characterName);
      }

      _luaBridge?.OnStart(this);
    }

    protected virtual void Update()
    {
      if (GamePauseManager.isPaused) return;
      if (isOnlyShow) return;
      if (Model.IsDead()) return;
      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject)
          && !EventChannelLocator.MainContainer.gameSettings.IsTest) return;
      if (EventChannelLocator.MainContainer.gameSettings.IsStayLobby) return;

      // 生命恢复（委托 Model）
      if (Model.TickHealthRecover(UnityEngine.Time.deltaTime))
        SetAndChangeHPUI();

      HandleInput();
    }

    protected virtual void FixedUpdate()
    {
      if (GamePauseManager.isPaused)
      {
        if (rb != null) rb.velocity = Vector3.zero;
        if (animator != null) animator.SetBool("isRun", false);
        return;
      }
      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject)) return;
      if (Model.IsDead()) return;
      MoveCharacter();
    }

    protected virtual void OnEnable()
    {
      localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();

      if (ServiceLocator.Get<PlayerManager>() != null)
        EventChannelLocator.MainContainer.playerQueryChannel.Raise(
            new PlayerQueryData(PlayerQueryType.RegisterPlayerObject) { playerObject = gameObject });

      int ownerActorNumber = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this);
      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
          new PlayerAttributeData(PlayerAttributeQueryType.RegisterAttribute, AttributeKeyConst.Main, Model.Attributes)
          { playerId = ownerActorNumber.ToString(), attributeName = AttributeKeyConst.Main }
      );
      EventChannelLocator.MainContainer.playerDamageEventChannel.RegisterListener(OnDamageReceived);
      EventChannelLocator.MainContainer.cardReceivedChannel.RegisterListener(OnApplicationCard);
      EventChannelLocator.MainContainer.skillQueryChannel.RegisterListener(OnSkillQuery);
      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected virtual void OnDisable()
    {
      if (ServiceLocator.Get<PlayerManager>() != null)
        EventChannelLocator.MainContainer.playerQueryChannel.Raise(
            new PlayerQueryData(PlayerQueryType.UnregisterPlayerObject) { playerObject = gameObject });

      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
          new PlayerAttributeData(PlayerAttributeQueryType.UnregisterAttribute)
          { playerId = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this).ToString(), attributeName = AttributeKeyConst.Main }
      );
      EventChannelLocator.MainContainer.playerDamageEventChannel.UnregisterListener(OnDamageReceived);
      EventChannelLocator.MainContainer.cardReceivedChannel.UnregisterListener(OnApplicationCard);
      EventChannelLocator.MainContainer.skillQueryChannel.UnregisterListener(OnSkillQuery);
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    protected virtual void HandleInput()
    {
      playerInputHandler.Update();
      float h = playerInputHandler.Horizontal;
      float v = playerInputHandler.Vertical;
      movementData.movementDirection = new Vector3(h, 0f, v).normalized;
    }

    protected virtual void MoveCharacter()
    {
      rb.angularVelocity = Vector3.zero;

      Vector3 moveVelocity = movementData.movementDirection * movementData.moveSpeed;
      movementData.isRun = movementData.movementDirection != Vector3.zero;
      animator.SetBool("isRun", movementData.isRun);

      moveVelocity.y = rb.velocity.y;
      rb.velocity = moveVelocity;

      if (movementData.movementDirection != Vector3.zero)
      {
        Quaternion targetRotation = Quaternion.LookRotation(movementData.movementDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation,
            movementData.rotationSpeed * UnityEngine.Time.fixedDeltaTime);
      }
    }

    protected virtual void OnDestroy()
    {
      var playerService = NetworkServiceLocator.PlayerService;
      if (playerService.IsOwnerOf(gameObject) && playerService.IsConnectedAndInRoom)
        ClearSpawnPointOccupation();

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
      if (!playerService.IsConnectedAndInRoom) return;

      object spawnPointObj = playerService.GetCustomProperty(PlayerPropertyKeys.SpawnPoint);
      if (spawnPointObj != null)
      {
        int spawnPointId = (int)spawnPointObj;
        var spawnService = ServiceLocator.Get<ISpawnPointService>();
        ISpawnPoint sp = spawnService?.GetSpawnPointById(spawnPointId);

        if (sp != null && (sp as MonoBehaviour) != null
            && sp.GetOccupiedByPlayer() == playerService.GetLocalActorNumber())
        {
          sp.ForceRelease();
        }
        playerService.SetCustomProperty(PlayerPropertyKeys.SpawnPoint, null);
      }
    }

    protected virtual void SetAndChangeHPUI()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest) return;
      if (sceneIndex > 1)
      {
        EventChannelLocator.MainContainer.hpChangedChannel.Raise(
            Model.Attributes.GetMaxHealth(), Model.Attributes.GetCurrentHealth());
        NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "NoticeOtherPlayerDamage", NetworkTarget.Others,
            ServiceLocator.Get<PlayerManager>().GetLocalPlayer().PlayerId.ToString(),
            Model.Attributes.GetMaxHealth(), Model.Attributes.GetCurrentHealth());
      }
    }

    protected virtual void OnDamageReceived(EventArgsBase args)
    {
      DamageEventArgs damageEventArgs = args as DamageEventArgs;
      if (damageEventArgs.damgeTarget != gameObject) return;
      if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject)) return;

      int damage = Mathf.CeilToInt(damageEventArgs.baseDamageValue);
      int finalDamage = Model.CalculateFinalDamage(damage);

      // 委托 Model 处理伤害
      var result = Model.ApplyDamage(finalDamage);
      if (!result.IsValid) return;

      SetAndChangeHPUI();

      if (result.Died)
        HandleDeath();
    }

    // ==================== 死亡处理 ====================

    protected virtual void HandleDeath()
    {
      movementData.movementDirection = Vector3.zero;
      if (rb != null)
      {
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
      }

      DisableColliders();
      DisableAttackComponents();

      if (_deathEffectPrefab != null)
      {
        GameObject effect = Instantiate(_deathEffectPrefab, transform.position, transform.rotation);
        Destroy(effect, 5f);
      }

      HideVisual();

      if (ServiceLocator.Get<TeamUIManager>() != null)
        ServiceLocator.Get<TeamUIManager>().HideLocalPlayerHP();

      if (ServiceLocator.Get<PlayerManager>() != null)
      {
        ServiceLocator.Get<PlayerManager>().UnregisterPlayerObject(gameObject);
        ServiceLocator.Get<PlayerManager>().SetPlayerDead(
            NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this).ToString());
      }

      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "NoticePlayerDeath", NetworkTarget.Others,
            NetworkServiceLocator.PlayerService.GetLocalActorNumber());
      }

      spectatorCameraController?.ActivateSpectator();
      _luaBridge?.OnDeath(this);
    }

    protected virtual void DisableColliders()
    {
      foreach (var col in GetComponentsInChildren<Collider>())
        col.enabled = false;
    }

    protected virtual void DisableAttackComponents()
    {
      foreach (var mb in GetComponentsInChildren<MonoBehaviour>())
      {
        if (mb is Controllers.Battle.AttackRangeBase)
          mb.enabled = false;
      }
    }

    protected virtual void HideVisual()
    {
      foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>())
        r.enabled = false;
      foreach (var canvas in GetComponentsInChildren<Canvas>())
        canvas.enabled = false;
    }

    public static void HandlePlayerDeath(int actorNumber)
    {
      string playerId = actorNumber.ToString();

      if (ServiceLocator.Get<PlayerManager>() != null)
      {
        ServiceLocator.Get<PlayerManager>().SetPlayerDead(playerId);

        foreach (var go in ServiceLocator.Get<PlayerManager>().ActivePlayerObjects)
        {
          if (go == null) continue;
          int ownerActor = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(go.transform);
          if (ownerActor == actorNumber)
          {
            ServiceLocator.Get<PlayerManager>().UnregisterPlayerObject(go);

            foreach (var col in go.GetComponentsInChildren<Collider>())
              col.enabled = false;
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
              r.enabled = false;
            foreach (var canvas in go.GetComponentsInChildren<Canvas>())
              canvas.enabled = false;

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
              rb.velocity = Vector3.zero;
              rb.isKinematic = true;
            }
            break;
          }
        }
      }

      var query = new PlayerAttributeData(PlayerAttributeQueryType.GetAttributeById)
      { playerId = playerId, attributeName = AttributeKeyConst.Main };
      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(query);
      query.attribute?.SetIsDead(true);
    }

    public static void HandleSyncPlayerElement(int actorNumber, int elementInt)
    {
      var query = new PlayerAttributeData(PlayerAttributeQueryType.GetAttributeById)
      { playerId = actorNumber.ToString(), attributeName = AttributeKeyConst.Main };
      EventChannelLocator.MainContainer.playerAttributeChannel.Raise(query);
      query.attribute?.SetCurrentElement((Element)elementInt);
    }

    protected virtual void OnApplicationQuit()
    {
      ClearSpawnPointOccupation();
    }

    protected virtual void SwitchElement(Element element)
    {
      Model.SetCurrentElement(element);
      SyncElementToAll(element);
      _luaBridge?.OnSwitchElement(this, element);
    }

    protected virtual void SyncElementToAll(Element element)
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest) return;

      NetworkServiceLocator.DomainRpcService?.InvokeRPC(
          "RPC_SyncPlayerElement", NetworkTarget.All,
          NetworkServiceLocator.PlayerService.GetLocalActorNumber(), (int)element);
    }

    protected virtual void UnlockElement(Element element)
    {
      if (_luaBridge != null && _luaBridge.OnUnlockElement(this, element))
        return;
      AddUnlockedElement(element);
      SwitchElement(element);
    }

    // ==================== ICardEffectContext 显式实现 ====================

    AttributePlayerBase ICardEffectContext.Attributes => Model.Attributes;
    PlayerMovementData ICardEffectContext.Movement => movementData;
    void ICardEffectContext.SwitchElement(Element element) => SwitchElement(element);
    void ICardEffectContext.UnlockElement(Element element) => UnlockElement(element);
    void ICardEffectContext.RefreshHPUI() => SetAndChangeHPUI();
    void ICardEffectContext.RaiseSkillQuery(SkillQueryData data)
        => EventChannelLocator.MainContainer.skillQueryChannel.Raise(data);

    protected virtual void OnApplicationCard(CardConfigSO card)
    {
      if (!EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        if (!NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject)) return;
      }

      if (card.Effects != null && card.Effects.Count > 0)
      {
        foreach (var effect in card.Effects)
        {
          if (effect != null)
            effect.Apply(this);
        }
      }
    }

    protected virtual void OnSkillQuery(SkillQueryData data)
    {
      _luaBridge?.OnSkillQuery(this, data);
    }

    protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      _luaBridge?.OnSceneLoaded(this, scene.buildIndex);
    }

    protected int GetMaxAttackCount() => Model.GetMaxAttackCount();

    // ==================== 对Lua桥接器的公开API ====================

    private string _characterName;

    public string GetCharacterName() => _characterName;
    public void SetCharacterName(string name) => _characterName = name;

    public AttributePlayerBase GetAttributeBase() => Model.Attributes;
    public Element GetCurrentElement() => Model.GetCurrentElement();
    public Rigidbody GetRigidbody() => rb;
    public Animator GetAnimator() => animator;
    public Vector3 GetPosition() => transform.position;
    public bool IsLocalPlayer() => NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject);
    public bool IsDead() => Model.IsDead();
    public int GetViewID() => NetworkServiceLocator.ObjectService.GetViewID(gameObject);
    public void SetAttributeConfig(PlayerAttributeConfigSO config) => playerAttributeConfig = config;

    public void BroadcastInitElementPool(int elementInt)
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest) return;
      NetworkServiceLocator.DomainRpcService?.InvokeRPC(
          "RPC_InitElementPool", NetworkTarget.Others, GetViewID(), elementInt);
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
      var data = info.photonView.InstantiationData;
      if (data != null && data.Length > 0)
        InitializeCharacter((string)data[0]);
    }

    public void InitializeCharacter(string characterName)
    {
      _characterName = characterName;

      if (playerAttributeConfig == null)
      {
        string configName = characterName;
        if (configName.EndsWith("Root"))
          configName = configName.Substring(0, configName.Length - 4);

        playerAttributeConfig = AssetLoader.TryLoadAsset<PlayerAttributeConfigSO>(
            $"Lobby_Config_PlayerConfig_{configName}Attr");
      }

      if (Model.Attributes == null)
      {
        var attr = new AttributePlayerBase(playerAttributeConfig);
        attr.SetMoveSpeed(movementData?.moveSpeed ?? 2.6f);
        Model.SetAttributes(attr);

        int ownerActorNumber = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this);
        EventChannelLocator.MainContainer.playerAttributeChannel.Raise(
            new PlayerAttributeData(PlayerAttributeQueryType.RegisterAttribute,
                AttributeKeyConst.Main, attr)
            { playerId = ownerActorNumber.ToString(), attributeName = AttributeKeyConst.Main }
        );
      }
    }

    public void HandleInitElementPool(int elementInt)
    {
      _luaBridge?.OnInitElementPool(this, elementInt);
    }
  }
}
