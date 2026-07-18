# Photon 联机框架架构总览

## 一、分层架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        启动引导层 (Bootstrap)                         │
│                                                                     │
│  InfrastructureRegistrar [BeforeSceneLoad]                           │
│    ├─ 设置 SendRate=30 / SerializationRate=30                         │
│    ├─ NetworkServiceLocator.Register(PhotonPlayerService,             │
│    │                                    PhotonObjectService)          │
│    ├─ GameServiceRegistrar.EnsureRegistered()  ← 早期兜底服务           │
│    ├─ ComponentFactory 注册 (ImpactCannon / CastNetwork)              │
│    ├─ PhotonCallbackBridge.EnsureExists()                            │
│    └─ 创建 3 个 RPC Bridge (App / Domain / Presentation)               │
│         └─ RegisterBridgeView(pv) → 延迟分配 ViewID                    │
└──────────────────────────┬──────────────────────────────────────────┘
                           │ 注册到
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    服务定位层 (Service Locator)                       │
│                                                                     │
│  NetworkServiceLocator (静态)                                        │
│    ├─ PlayerService     : INetworkPlayerService                      │
│    ├─ ObjectService      : INetworkObjectService                     │
│    ├─ ObjectPoolService  : IObjectPoolService                        │
│    ├─ GameActionService  : IGameActionService                        │
│    └─ DomainRpcService   : IDomainRpcService                        │
└──────────────────────────┬──────────────────────────────────────────┘
                           │ 被消费
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    网络基础设施层 (Infrastructure)                     │
│                                                                     │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────────┐ │
│  │ PhotonCallback  │  │    Launcher       │  │   3× RPC Bridge    │ │
│  │    Bridge       │  │  (MonoBehaviour    │  │  App / Domain /    │ │
│  │                 │  │   PunCallbacks)   │  │  Presentation      │ │
│  │ PUN回调转发 →    │  │                   │  │                    │ │
│  │ PlayerService   │  │ ┌───────────────┐ │  │ [PunRPC] 方法      │ │
│  │                 │  │ │SpawnPointMgr  │ │  │ 纯转发到 Domain     │ │
│  │ ViewID 分配      │  │ │(生成/角色切换) │ │  │                    │ │
│  │                 │  │ ├───────────────┤ │  │  InvokeRPC() →     │ │
│  │                 │  │ │NetworkScene   │ │  │  NetworkTargetMapper│ │
│  │                 │  │ │Flow(准备/场景) │ │  │  → photonView.RPC   │ │
│  │                 │  │ └───────────────┘ │  │                    │ │
│  └─────────────────┘  └──────────────────┘  └────────────────────┘ │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │
│  │PhotonPlayer  │  │PhotonObject  │  │  NetworkObjectPoolManager │   │
│  │  Service     │  │  Service     │  │  (IPunPrefabPool)         │   │
│  │              │  │              │  │  怪物/掉落物对象池          │   │
│  │ 玩家属性查询   │  │ 实例化/查找   │  │                          │   │
│  │ Dictionary缓存│  │ RPC 调用     │  │  HashSet 活跃对象追踪      │   │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘   │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │
│  │CastNetwork   │  │PhotonNetwork │  │  NetworkTargetMapper     │   │
│  │(弹幕/伤害同步)│  │  Adapter     │  │  (枚举映射工具)            │   │
│  └──────────────┘  └──────────────┘  └──────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                           │ 解耦
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    业务层 (Domain / Application)                      │
│                                                                     │
│  PlayerController │ EnemyBase │ SpiderBoss │ BallRobot_Blue         │
│  WizardBoy │ ExperienceManager │ TaskManager │ PowerUpManager        │
│  SyncedGameTimeManager │ LobbyCanvas │ GameManager │ ...             │
│                                                                     │
│  通过 NetworkServiceLocator / IDomainRpcService / EventChannel       │
│  访问网络功能，不直接依赖 Photon 类型                                  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 二、文件清单与职责

### 启动引导
| 文件 | 行数 | 职责 |
|---|---|---|
| `InfrastructureRegistrar.cs` | ~95 | `[BeforeSceneLoad]` 注册所有服务、创建 3 个 RPC Bridge、设置全局网络参数 |
| `GameServiceRegistrar.cs` | ~110 | Launcher 加载前注册早期兜底服务（EarlyObjectPoolService / EarlyGameActionService） |

### 服务定位
| 文件 | 行数 | 职责 |
|---|---|---|
| `NetworkServiceLocator.cs` | ~100 | 静态服务容器，持有 5 个网络服务接口实例 |

### 服务接口 (Controllers/Services/)
| 文件 | 接口 | 方法 |
|---|---|---|
| `INetworkPlayerService.cs` | `INetworkPlayerService` | 玩家属性查询/设置、事件通知 |
| `INetworkObjectService.cs` | `INetworkObjectService` | 实例化、ViewID 查找、RPC 调用 |
| `IDomainRpcService.cs` | `IDomainRpcService` | `InvokeRPC(methodName, target, params)` |
| `IObjectPoolService.cs` | `IObjectPoolService` | `GetInactiveObjectByName`、`ReturnToLobby` |
| `IGameActionService.cs` | `IGameActionService` | `QuitToMainMenu`、`SetLocalReady` |
| `ISpawnPoint.cs` | `ISpawnPoint` | 生成点状态接口 |

### 核心网络类 (Controllers/Network/)
| 文件 | 行数 | 类 | 职责 |
|---|---|---|---|
| `Launcher.cs` | ~370 | `Launcher : MonoBehaviourPunCallbacks` | 连接/房间/回调路由/UI门面，持有 SpawnPointManager + NetworkSceneFlow |
| `SpawnPointManager.cs` | ~330 | `SpawnPointManager` (纯C#) | 玩家生成、生成点分配、角色切换 |
| `NetworkSceneFlow.cs` | ~180 | `NetworkSceneFlow` (纯C#) | 准备系统、场景加载协调、返回大厅 |
| `PhotonCallbackBridge.cs` | ~95 | `PhotonCallbackBridge : MonoBehaviourPunCallbacks` | PUN 回调转发到 PlayerService + ViewID 分配 |
| `PhotonPlayerService.cs` | ~165 | `PhotonPlayerService : INetworkPlayerService` | 玩家属性查询（Dictionary 缓存） |
| `PhotonObjectService.cs` | ~67 | `PhotonObjectService : INetworkObjectService` | 实例化/RPC/ViewID 查找 |
| `CastNetwork.cs` | ~400 | `CastNetwork : MonoBehaviourPun` | 弹幕同步、伤害广播 |
| `NetworkObjectPoolManager.cs` | ~380 | `NetworkObjectPoolManager : MonoBehaviourPunCallbacks` | 网络对象池（IPunPrefabPool） |
| `DomainRpcBridge.cs` | ~220 | `DomainRpcBridge : MonoBehaviourPun` | Domain 层 [PunRPC] 转发 |
| `AppRpcBridge.cs` | ~175 | `AppRpcBridge : MonoBehaviourPun` | Application 层 [PunRPC] 转发 |
| `PresentationRpcBridge.cs` | ~55 | `PresentationRpcBridge : MonoBehaviourPun` | Presentation 层 [PunRPC] 转发 |
| `PlayerPropertyKeys.cs` | ~11 | `static` | CustomProperties 键名常量 |
| `NetworkTargetMapper.cs` | ~15 | `static` | NetworkTarget → RpcTarget 映射 |
| `PhotonNetworkAdapter.cs` | ~30 | `PhotonNetworkAdapter : NetworkIdentityBase` | PhotonView → INetworkIdentity 适配器 |
| `NetworkIdentityBase.cs` | ~15 | `abstract` | 网络身份抽象基类 |
| `PlayerStateSync.cs` | ~40 | `PlayerStateSync : MonoBehaviourPun` | SpriteRenderer.flipX 同步 |
| `LocalCameraActivator.cs` | ~45 | `LocalCameraActivator : MonoBehaviourPun` | 本地玩家相机激活 |
| `PauseStateHandler.cs` | ~95 | `PauseStateHandler : MonoBehaviour` | 暂停时冻结 Animator/NavMesh/Rigidbody |
| `INetworkFireballCaster.cs` | ~20 | `interface` | 弹幕发射接口 |
| `INetworkService.cs` | ~20 | `enum + interfaces` | NetworkTarget 枚举 + INetworkIdentity/INetworkRPC |

---

## 三、关键调用流程

### 流程 1：连接 → 生成玩家

```
Launcher.Start()
  └─ PhotonNetwork.ConnectUsingSettings()
       ↓ (Photon 服务器)
Launcher.OnConnectedToMaster()
  └─ PhotonNetwork.CreateRoom("Room_xxxx", MaxPlayers=4)
       ↓
Launcher.OnJoinedRoom()
  ├─ SpawnPointManager.EnsurePlayerManagerExists()
  └─ 延迟 0.5s
       └─ SpawnPointManager.CreatedOrJoinedRoom()
            ├─ GameManager.FindSpawnPoints()
            ├─ EnsurePlayerManagerExists()
            └─ SpawnPlayer()
                 ├─ 按 ActorNumber 确定性分配生成点
                 │   (不同玩家必然得到不同生成点)
                 ├─ sp.SetOccupied(true, actorNumber)
                 ├─ PhotonNetwork.Instantiate(character)
                 └─ SetCustomProperties({SpawnPoint: sp.Id})
```

### 流程 2：准备 → 加载游戏场景

```
LobbyCanvas.OnStartClicked()  (UI 按钮点击)
  └─ gameActionChannel.Raise(SetLocalReady)
       ↓
GameManager 接收事件
  └─ NetworkServiceLocator.GameActionService.SetLocalReady(true)
       ↓ (Launcher 门面委托)
NetworkSceneFlow.SetLocalReady(true)
  ├─ SetCustomProperties({PlayerReady: true})
  ├─ NotifyPropertyChanged(...)  ← 本地 UI 即时更新
  └─ CheckAllPlayersReady()
       └─ 所有玩家就绪?
            └─ isRoomLoading = true
                 └─ 延迟 2s → Loading.Show()
                      └─ MasterClient: PhotonNetwork.LoadLevel(2)
                           ↓ (场景同步到所有客户端)
Launcher.OnSceneLoaded(buildIndex > 1)
  ├─ _sceneFlow.ResetForLobby()  ← 重置标志（防泄漏）
  └─ _spawnPointManager.CreatedOrJoinedRoom()
```

### 流程 3：RPC 调用

```
Domain 层代码 (如 SyncedGameTimeManager)
  └─ NetworkServiceLocator.DomainRpcService.InvokeRPC(
        "RPC_SyncStartTime", NetworkTarget.Others)
       ↓
DomainRpcBridge.InvokeRPC(methodName, target, params)
  └─ photonView.RPC(methodName, NetworkTargetMapper.ToRpcTarget(target), params)
       │  NetworkTarget.Others → RpcTarget.Others
       ↓ (Photon 网络传输)
DomainRpcBridge 上的 [PunRPC] 方法 (目标客户端执行)
  └─ SyncedGameTimeManager.HandleSyncStartTime()
```

### 流程 4：伤害同步

```
ImpactCannon.OnTriggerEnter(other)
  └─ (本地玩家的弹幕命中敌人)
CastNetwork.BroadcastDamage(enemyObj, damage, ...)
  ├─ enemyView = enemyObj.GetComponent<PhotonView>()
  └─ photonView.RPC("RPC_DealDamage", RpcTarget.All,
         enemyView.ViewID, damage, ...)
       ↓ (所有客户端执行)
[PunRPC] RPC_DealDamage(enemyViewID, damage, ...)
  ├─ enemyView = PhotonView.Find(enemyViewID)
  ├─ DamageEventArgs.GetShared(element, attacker, target, damage, ...)
  └─ damageEventChannel.Raise(args)
       ↓ (ScriptableObject 事件通道)
EnemyBase.OnDamageReceived(args)
  ├─ if (args.damgeTarget != gameObject) return  ← 去重
  └─ TakeDamage(args)
       └─ attribute.TakeDamage(finalDamageValue)
```

---

## 四、设计要点

### 1. 分层 RPC Bridge 模式
```
Domain 层 (业务逻辑)
  ↓ 通过 IDomainRpcService.InvokeRPC()
DomainRpcBridge (持有 [PunRPC] 方法，纯转发)
  ↓ photonView.RPC()
Photon 网络传输
  ↓
DomainRpcBridge (目标客户端，[PunRPC] 执行)
  ↓ 调用 Domain 层的 Handle* 静态方法
Domain 层 (执行业务逻辑)
```
**好处**：业务代码不直接依赖 `PhotonView` / `PhotonNetwork`，可通过接口测试。

### 2. Launcher 门面模式
```
外部调用方                          Launcher 内部委托
─────────────────────────────────────────────────────
IGameActionService.SetLocalReady  →  NetworkSceneFlow.SetLocalReady
IGameActionService.QuitToMainMenu →  Launcher 自身 (连接逻辑)
IObjectPoolService.ReturnToLobby  →  NetworkSceneFlow.ReturnToLobby
IObjectPoolService.GetInactive    →  Launcher 自身 (场景层级搜索)
Launcher.instance.SwitchCharacter →  SpawnPointManager.SwitchCharacter
```
**好处**：外部代码零改动，Launcher 从 958 行降至 ~370 行。

### 3. 服务启动顺序
```
BeforeSceneLoad:
  InfrastructureRegistrar
    ├─ 注册 PhotonPlayerService + PhotonObjectService
    ├─ GameServiceRegistrar (早期兜底 IObjectPoolService/IGameActionService)
    ├─ 创建 3 个 RPC Bridge (ViewID=0，待分配)
    └─ 创建 PhotonCallbackBridge

大厅场景加载:
  Launcher.Awake()
    ├─ 覆盖注册 IObjectPoolService + IGameActionService (真实实现)
    └─ 创建 SpawnPointManager + NetworkSceneFlow

OnJoinedRoom:
  PhotonCallbackBridge
    └─ AllocateBridgeViewIDs()  ← 为 3 个 Bridge 分配 ViewID
```

### 4. 事件驱动解耦
```
伤害系统:    RPC → damageEventChannel → EnemyBase
暂停系统:    RPC → pauseStateChannel → PauseStateHandler
UI 更新:    PlayerService.OnPlayerPropertyChanged → WordlSpaceUI
```
ScriptableObject 事件通道将 Photon 回调与业务逻辑完全解耦。
