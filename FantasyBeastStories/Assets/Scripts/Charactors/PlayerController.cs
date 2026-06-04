using System.Collections;
using System.Collections.Generic;
using Atttibute;
using CardData;
using Cinemachine;
using Events;
using Manager;
using Other;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Charactors
{
    public class PlayerController : MonoBehaviourPun
    {
        public int spawnPointIndex;

        [SerializeField]
        protected bool isOnlyShow = false; // 是否只为显示角色而不用于其他操作

        [SerializeField]
        protected GameObject virtualCamera; // 虚拟摄像机组件

        [Header("移动设置")]
        protected float moveSpeed = 2.6f; // 移动速度

        [SerializeField]
        protected Rigidbody rb; // 物理组件

        [SerializeField]
        protected Animator animator; // 动画组件

        [Header("旋转设置")]
        [SerializeField]
        protected float rotationSpeed = 6f; // 旋转速度
        protected AttributePlayerBase attributePlayerBase; // 玩家属性组件
        protected Vector3 movement; // 移动方向
        protected bool isRun; // 是否正在运行

        [SerializeField]
        protected bool isInLobby; // 是否在大厅场景

        [SerializeField]
        protected GameObject isReadyPanel; // 准备界面
        int localActorNumber; // 本地玩家ActorNumber
        int sceneIndex; // 场景索引

        protected virtual void Awake()
        {
            isInLobby = GameManager.isStayLobby;
            attributePlayerBase = new AttributePlayerBase(35, 5, 500, moveSpeed, 1.1f, 0.5f);
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
            moveSpeed = attributePlayerBase.GetMoveSpeed();
            // 获取或添加Rigidbody组件
            if (rb == null)
            {
                rb = gameObject.GetComponent<Rigidbody>();
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
            if (isOnlyShow)
            {
                return; // 如果只显示角色，不处理输入
            }
            if (!photonView.IsMine && photonView != null && GameManager.isTest == false)
            {
                return; // 只处理本地玩家的输入和动画
            }
            if (GameManager.isStayLobby)
            {
                return; // 如果在大厅场景，不处理输入
            }
            HandleInput();
        }

        protected virtual void FixedUpdate()
        {
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
                PlayerManager.instance.RegisterPlayerObject(gameObject);

            if (EventManager.instance != null)
            {
                EventManager.instance.RegisterAttributePlayerBase(
                    localActorNumber,
                    EventNames.PlayerAttribute_Main,
                    attributePlayerBase
                );
                EventManager.instance.RegisterEventComplex(
                    EventNames.DamageReceiverPlayer,
                    OnDamageReceived
                );
            }
            else
            {
                //创建一个新的EventManager实例并注册事件
                GameObject eventManagerObj = new GameObject("EventManager");
                EventManager eventManager = eventManagerObj.AddComponent<EventManager>();
                eventManager.RegisterAttributePlayerBase(
                    localActorNumber,
                    EventNames.PlayerAttribute_Main,
                    attributePlayerBase
                );
                eventManager.RegisterEventComplex(
                    EventNames.DamageReceiverPlayer,
                    OnDamageReceived
                );
                EventManager.instance = eventManager;
            }
        }

        protected virtual void OnDisable()
        {
            // 从PlayerManager注销玩家GameObject
            if (PlayerManager.instance != null)
                PlayerManager.instance.UnregisterPlayerObject(gameObject);

            EventManager.instance.UnRegisterAttributePlayerBase(
                localActorNumber,
                EventNames.PlayerAttribute_Main
            );
            EventManager.instance.UnRegisterEventComplex(
                EventNames.DamageReceiverPlayer,
                OnDamageReceived
            );
        }

        protected virtual void HandleInput()
        {
            // 获取水平输入（A/D或左右箭头）
            float horizontal = Input.GetAxis("Horizontal");
            // 获取垂直输入（W/S或上下箭头）
            float vertical = Input.GetAxis("Vertical");
            // 计算移动方向（基于世界坐标）
            movement = new Vector3(horizontal, 0f, vertical).normalized;
        }

        protected virtual void MoveCharacter()
        {
            // 计算移动向量
            Vector3 moveVelocity = movement * moveSpeed;
            isRun = movement != Vector3.zero;
            animator.SetBool("isRun", isRun);
            // 保持Y轴速度不变（重力影响）
            moveVelocity.y = rb.velocity.y;

            // 应用速度
            rb.velocity = moveVelocity;

            // ===== 新增：角色朝向逻辑 =====
            // 只有在移动时才改变朝向
            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                // 如果模型“面朝 +X”，要再转 90 度
                targetRotation *= Quaternion.Euler(0f, 0f, 0f);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
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
            if (EventManager.instance != null)
            {
                EventManager.instance.UnRegisterAttributePlayerBase(
                    localActorNumber,
                    EventNames.PlayerAttribute_Main
                );
                Debug.Log($"[PlayerController] OnDestroy - 注销属性组件");
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
            if (sceneIndex > 1)
            {
                EventManager.instance.TriggerFloatEvent(
                    EventNames.HPChanged,
                    attributePlayerBase.GetMaxHealth(),
                    attributePlayerBase.GetCurrentHealth()
                );
            }
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
            attributePlayerBase.Damage(finalDamage);
            // 触发HP变化事件
            SetAndChangeHPUI();
            // 通知其他玩家我受到了伤害
            if (GameManager.isTest)
                return;
            photonView.RPC(
                "NoticeOtherPlayerDamage",
                RpcTarget.Others,
                PlayerManager.instance.GetLocalPlayer().PlayerId.ToString(),
                attributePlayerBase.GetMaxHealth(),
                attributePlayerBase.GetCurrentHealth()
            );
        }

        //根据防御伤害计算最终伤害
        protected virtual int CalculateFinalDamage(int damage)
        {
            return damage - (int)attributePlayerBase.GetDefensePower();
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
            TeamUIManager.instance.SetOtherPlayerSlider_HP(playerId, MaxHP, CurrentHP);
        }

        // 当玩家断开连接时
        protected virtual void OnApplicationQuit()
        {
            ClearSpawnPointOccupation();
        }

        protected virtual void OnApplicationCard(CardConfigBase card)
        {
            if (!GameManager.isTest)
            {
                if (!photonView.IsMine)
                {
                    return;
                }
            }
            Debug.LogWarning("应用了卡牌效果：" + card.Name + ":" + card.Content + card.Value);

            switch (card.Name)
            {
                case "锋利的短剑":
                    attributePlayerBase.AddAttackPower(8);
                    break;
            }
        }
    }
}
