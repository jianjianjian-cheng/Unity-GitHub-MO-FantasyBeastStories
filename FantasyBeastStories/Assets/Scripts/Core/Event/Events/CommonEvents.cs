using UnityEngine;

namespace Core.Event.Events
{
    /// <summary>
    /// 伤害事件
    /// </summary>
    [System.Serializable]
    public class DamageEvent : GameEventBase
    {
        public GameObject Target { get; }
        public GameObject Attacker { get; }
        public float Damage { get; }
        public bool IsCritical { get; }

        public DamageEvent(GameObject target, GameObject attacker, float damage, bool isCritical = false)
        {
            Target = target;
            Attacker = attacker;
            Damage = damage;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// 角色死亡事件
    /// </summary>
    [System.Serializable]
    public class DeathEvent : GameEventBase
    {
        public GameObject Character { get; }
        public GameObject Killer { get; }
        public int ExperienceReward { get; }

        public DeathEvent(GameObject character, GameObject killer = null, int experienceReward = 0)
        {
            Character = character;
            Killer = killer;
            ExperienceReward = experienceReward;
        }
    }

    /// <summary>
    /// 游戏开始事件
    /// </summary>
    public class GameStartEvent : GameEventBase
    {
        public int SceneIndex { get; }
        public GameStartEvent(int sceneIndex) => SceneIndex = sceneIndex;
    }

    /// <summary>
    /// 游戏暂停事件
    /// </summary>
    public class GamePauseEvent : GameEventBase
    {
        public bool IsPaused { get; }
        public GamePauseEvent(bool isPaused) => IsPaused = isPaused;
    }

    /// <summary>
    /// 玩家复活事件
    /// </summary>
    public class PlayerReviveEvent : GameEventBase
    {
        public GameObject Player { get; }
        public Vector3 RevivePosition { get; }

        public PlayerReviveEvent(GameObject player, Vector3 revivePosition)
        {
            Player = player;
            RevivePosition = revivePosition;
        }
    }

    /// <summary>
    /// 升级事件
    /// </summary>
    public class LevelUpEvent : GameEventBase
    {
        public GameObject Character { get; }
        public int NewLevel { get; }

        public LevelUpEvent(GameObject character, int newLevel)
        {
            Character = character;
            NewLevel = newLevel;
        }
    }

    /// <summary>
    /// 经验值获取事件
    /// </summary>
    public class ExperienceGainEvent : GameEventBase
    {
        public GameObject Player { get; }
        public int Amount { get; }
        public Vector3 Position { get; }

        public ExperienceGainEvent(GameObject player, int amount, Vector3 position)
        {
            Player = player;
            Amount = amount;
            Position = position;
        }
    }
}
