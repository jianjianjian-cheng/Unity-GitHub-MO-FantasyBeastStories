using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Controllers.Combat
{
    /// <summary>
    /// 统一管理所有碰撞体触发器的单例管理器。
    /// 负责：碰撞体生命周期管理、玩家-敌人交互、对象池回收、伤害事件传递。
    /// </summary>
    public class ColliderTriggerManager : MonoBehaviour
    {
        public static ColliderTriggerManager Instance { get; private set; }

        [Header("运行时生成的碰撞体")]
        [SerializeField]
        private Transform runtimeColliderParent;

        [Header("敌人触发器预置物")]
        [SerializeField]
        private GameObject enemyTriggerPrefab;

        /// <summary>
        /// 触发器信息
        /// </summary>
        public class TriggerInfo
        {
            public GameObject triggerGameObject;
            public ColliderTriggerInfoSO triggerInfoSO;
            public int playerViewID;
            public float lifeTime;
            public float damage;
            public TriggerType triggerType;
            public Element element;
            public float scale;
            public DamageEventArgs damageEventArgs;
        }

        // 碰撞体ID -> 触发器信息
        private readonly Dictionary<int, TriggerInfo> triggerMap = new Dictionary<int, TriggerInfo>();

        // 碰撞体ID -> 存活倒计时
        private readonly Dictionary<int, float> lifeTimeMap = new Dictionary<int, float>();

        // 已被注册/标记为"使用过的"碰撞体，用于销毁时校验
        private readonly HashSet<int> destroyedTriggers = new HashSet<int>();

        private float timer = 0f;
        private bool isBatchUpdating = false;
        private bool isLateUpdating = false;

        #region 碰撞体预制体注册表
        [System.Serializable]
        public struct ColliderPrefabEntry
        {
            public string weaponType;
            public GameObject prefab;
            public float lifeTime;
            public float scale;
        }

        [Header("碰撞体预制体配置")]
        [SerializeField]
        private List<ColliderPrefabEntry> colliderPrefabs;

        private Dictionary<string, ColliderPrefabEntry> prefabRegistry;
        #endregion

        [Header("武器被动数值")]
        public string WeaponType;
        public float weaponPassiveVal1;
        public float weaponPassiveVal2;

        #region 生命周期
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitPrefabRegistry();
                PreallocateTriggers(32);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (isBatchUpdating || isLateUpdating)
                return;
            // 每帧更新所有存活倒计时，但不直接处理回收
            List<int> keys = new List<int>(lifeTimeMap.Keys);
            foreach (int id in keys)
            {
                if (lifeTimeMap.ContainsKey(id))
                {
                    lifeTimeMap[id] -= UnityEngine.Time.deltaTime;
                }
            }
        }

        void LateUpdate()
        {
            if (isBatchUpdating || isLateUpdating)
                return;
            isLateUpdating = true;
            try
            {
                List<int> keys = new List<int>(lifeTimeMap.Keys);
                foreach (int id in keys)
                {
                    if (lifeTimeMap.ContainsKey(id) && lifeTimeMap[id] <= 0)
                    {
                        TryRecycleTrigger(id);
                    }
                }
            }
            finally
            {
                isLateUpdating = false;
            }
        }

        void InitPrefabRegistry()
        {
            prefabRegistry = new Dictionary<string, ColliderPrefabEntry>();
            if (colliderPrefabs == null)
                return;
            foreach (var entry in colliderPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.weaponType) && entry.prefab != null)
                {
                    prefabRegistry[entry.weaponType] = entry;
                }
            }
        }

        void PreallocateTriggers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject go = InstantiateTriggerGameObject();
                go.SetActive(false);
            }
        }

        GameObject InstantiateTriggerGameObject()
        {
            GameObject go = new GameObject($"DynamicTrigger_{triggerMap.Count}");
            go.layer = LayerMask.NameToLayer("DynamicCollider");
            go.SetActive(false);
            go.transform.SetParent(runtimeColliderParent);
            return go;
        }
        #endregion

        #region 碰撞体管理（核心）
        /// <summary>
        /// 注册一个碰撞体，传入触发器信息。返回唯一碰撞体ID。
        /// </summary>
        public int RegisterTrigger(ColliderTriggerInfoSO info, int playerViewID)
        {
            // 查询是否存在可用的空闲碰撞体
            int? reuseID = FindInactiveTrigger();
            GameObject colliderGO;
            int triggerID;
            if (reuseID.HasValue)
            {
                triggerID = reuseID.Value;
                colliderGO = triggerMap[triggerID].triggerGameObject;
                colliderGO.SetActive(true);
                // 清理旧组件
                ClearOldColliderComponents(colliderGO);
            }
            else
            {
                colliderGO = InstantiateTriggerGameObject();
                triggerID = colliderGO.GetInstanceID();
            }

            // 组装触发器信息
            TriggerInfo tInfo = new TriggerInfo
            {
                triggerGameObject = colliderGO,
                triggerInfoSO = info,
                playerViewID = playerViewID,
                lifeTime = info.lifeTime,
                damage = info.damage,
                triggerType = info.triggerType,
                element = info.element,
                scale = info.scale,
            };

            // 装配组件
            AttachTriggerComponents(colliderGO, tInfo);

            triggerMap[triggerID] = tInfo;
            lifeTimeMap[triggerID] = info.lifeTime;
            destroyedTriggers.Remove(triggerID);

            Debug.Log(
                $"[ColliderTriggerManager] 注册碰撞体 ID={triggerID}, 玩家={playerViewID}, 类型={info.triggerType}"
            );
            return triggerID;
        }

        /// <summary>
        /// 批量注册碰撞体。
        /// </summary>
        public List<int> RegisterTriggers(
            ColliderTriggerInfoSO info,
            int playerViewID,
            int count
        )
        {
            List<int> ids = new List<int>(count);
            isBatchUpdating = true;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int id = RegisterTrigger(info, playerViewID);
                    ids.Add(id);
                }
            }
            finally
            {
                isBatchUpdating = false;
            }
            return ids;
        }

        /// <summary>
        /// 回收指定ID的碰撞体。
        /// </summary>
        public void RecycleTrigger(int triggerID)
        {
            TryRecycleTrigger(triggerID);
        }

        /// <summary>
        /// 批量回收碰撞体。
        /// </summary>
        public void RecycleTriggers(List<int> triggerIDs)
        {
            isBatchUpdating = true;
            try
            {
                foreach (int id in triggerIDs)
                {
                    TryRecycleTrigger(id);
                }
            }
            finally
            {
                isBatchUpdating = false;
            }
        }

        /// <summary>
        /// 根据触发器GameObject回收。
        /// </summary>
        public void RecycleTriggerByGameObject(GameObject triggerGO)
        {
            if (triggerGO == null)
                return;
            int id = triggerGO.GetInstanceID();
            if (triggerMap.ContainsKey(id))
            {
                TryRecycleTrigger(id);
            }
        }

        /// <summary>
        /// 获取触发器信息。
        /// </summary>
        public TriggerInfo GetTriggerInfo(int triggerID)
        {
            if (triggerMap.TryGetValue(triggerID, out TriggerInfo info))
            {
                return info;
            }
            return null;
        }

        /// <summary>
        /// 根据碰撞体查找其触发器信息。
        /// </summary>
        public TriggerInfo GetTriggerInfoByCollider(Collider collider)
        {
            if (collider == null)
                return null;
            int id = collider.gameObject.GetInstanceID();
            if (triggerMap.TryGetValue(id, out TriggerInfo info))
            {
                return info;
            }
            return null;
        }

        /// <summary>
        /// 获取所有活跃的触发器信息。
        /// </summary>
        public List<TriggerInfo> GetActiveTriggers()
        {
            return triggerMap.Values.Where(t => t.triggerGameObject != null && t.triggerGameObject.activeSelf).ToList();
        }

        /// <summary>
        /// 判断指定碰撞体是否已经被处理过（避免重复销毁/回收）。
        /// </summary>
        public bool IsTriggerDestroyed(int triggerID)
        {
            return destroyedTriggers.Contains(triggerID);
        }

        /// <summary>
        /// 标记碰撞体已被处理过。
        /// </summary>
        public void MarkTriggerDestroyed(int triggerID)
        {
            destroyedTriggers.Add(triggerID);
        }

        /// <summary>
        /// 根据武器类型获取碰撞体预制体信息。
        /// </summary>
        public ColliderPrefabEntry? GetColliderPrefab(string weaponType)
        {
            if (prefabRegistry != null && prefabRegistry.TryGetValue(weaponType, out ColliderPrefabEntry entry))
            {
                return entry;
            }
            return null;
        }

        /// <summary>
        /// 注册碰撞体预制体。
        /// </summary>
        public void RegisterColliderPrefab(ColliderPrefabEntry entry)
        {
            if (prefabRegistry == null)
                InitPrefabRegistry();
            if (!string.IsNullOrEmpty(entry.weaponType) && entry.prefab != null)
            {
                prefabRegistry[entry.weaponType] = entry;
            }
        }

        /// <summary>
        /// 注册碰撞体并获取Info，用于获取triggerInfo。
        /// </summary>
        public TriggerInfo GetOrCreateTriggerInfo(ColliderTriggerInfoSO info, int playerViewID)
        {
            int id = RegisterTrigger(info, playerViewID);
            return triggerMap[id];
        }
        #endregion

        #region 内部方法
        int? FindInactiveTrigger()
        {
            foreach (var pair in triggerMap)
            {
                if (pair.Value.triggerGameObject != null && !pair.Value.triggerGameObject.activeSelf)
                {
                    return pair.Key;
                }
            }
            return null;
        }

        void ClearOldColliderComponents(GameObject go)
        {
            // 移除旧的 Trigger 脚本和 Collider
            var oldTriggers = go.GetComponents<ColliderTriggerBase>();
            foreach (var t in oldTriggers)
            {
                Destroy(t);
            }
            var oldColliders = go.GetComponents<Collider>();
            foreach (var c in oldColliders)
            {
                Destroy(c);
            }
            var oldBuffers = go.GetComponents<AttackBuffer>();
            foreach (var b in oldBuffers)
            {
                Destroy(b);
            }
        }

        void AttachTriggerComponents(GameObject go, TriggerInfo info)
        {
            // 1. 附加碰撞体
            if (info.triggerInfoSO.triggerType == TriggerType.EnemyAttack)
            {
                // 如果是敌人攻击触发器，附加特殊处理
                // 这里可以扩展为多种敌人攻击类型
                SphereCollider sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = info.scale;
            }
            else if (info.triggerInfoSO.triggerType == TriggerType.Bullet)
            {
                CapsuleCollider cc = go.AddComponent<CapsuleCollider>();
                cc.isTrigger = true;
                cc.radius = info.scale * 0.5f;
                cc.height = info.scale;
                cc.direction = 2; // Z轴方向
            }
            else if (info.triggerInfoSO.triggerType == TriggerType.Custom)
            {
                if (info.triggerInfoSO is ColliderTriggerInfoSO customInfo && customInfo.colliderType == ColliderType.Sphere)
                {
                    SphereCollider sc = go.AddComponent<SphereCollider>();
                    sc.isTrigger = true;
                    sc.radius = info.scale;
                }
                else
                {
                    BoxCollider bc = go.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    bc.size = new Vector3(info.scale, info.scale, info.scale);
                }
            }
            else if (info.triggerInfoSO.triggerType == TriggerType.Roll)
            {
                SphereCollider sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = info.scale;
            }
            else if (info.triggerInfoSO.triggerType == TriggerType.EnemyAttack)
            {
                SphereCollider sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = info.scale;
            }
            else
            {
                BoxCollider bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(info.scale, info.scale, info.scale);
            }

            // 2. 附加触发器逻辑脚本
            if (info.triggerInfoSO.triggerType == TriggerType.Roll)
            {
                EnemyRollTrigger trigger = go.AddComponent<EnemyRollTrigger>();
                trigger.Setup(info);
            }
            else
            {
                ColliderTriggerBase trigger = go.AddComponent<ColliderTriggerBase>();
                trigger.Setup(info);
            }
            //3.AttackBuffer
            AttackBuffer attackBuffer = go.AddComponent<AttackBuffer>();
        }

        void TryRecycleTrigger(int triggerID)
        {
            if (triggerMap.ContainsKey(triggerID))
            {
                GameObject go = triggerMap[triggerID].triggerGameObject;
                if (go != null)
                {
                    go.SetActive(false);
                }
                triggerMap.Remove(triggerID);
                lifeTimeMap.Remove(triggerID);
                destroyedTriggers.Add(triggerID);
            }
        }
        #endregion

        #region 回调事件
        /// <summary>
        /// 当碰撞体与玩家交互时回调，用于处理伤害等。
        /// </summary>
        public void OnTriggerInteractPlayer(int triggerID, GameObject playerGO)
        {
            if (triggerMap.TryGetValue(triggerID, out TriggerInfo info))
            {
                // 处理伤害
                // 这里可以扩展为更复杂的伤害计算逻辑
                Debug.Log($"[ColliderTriggerManager] 触发器 {triggerID} 与玩家交互");
            }
        }
        #endregion

        #region 清理
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion
    }
}