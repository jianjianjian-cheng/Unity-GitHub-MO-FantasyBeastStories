using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Combat
{
    public enum EnemySpawnType
    {
        Skeleton,
        Boss_Horror,
        TrainingDummy,
        Custom
    }

    [CreateAssetMenu(menuName = "Events/Combat/Enemy Spawn Event Channel")]
    public class EnemySpawnEventChannelSO : BaseEventChannelSO<EnemySpawnData>
    {
    }

    public class EnemySpawnData : EventArgsBase
    {
        public EnemySpawnType spawnType;
        public Vector3 position;
        public float difficultyCoefficient;
        public string customPoolName;

        public EnemySpawnData(EnemySpawnType spawnType, Vector3 position)
        {
            this.spawnType = spawnType;
            this.position = position;
        }

        public EnemySpawnData(EnemySpawnType spawnType, Vector3 position, float difficultyCoefficient)
        {
            this.spawnType = spawnType;
            this.position = position;
            this.difficultyCoefficient = difficultyCoefficient;
        }

        public EnemySpawnData(string customPoolName, Vector3 position)
        {
            this.spawnType = EnemySpawnType.Custom;
            this.customPoolName = customPoolName;
            this.position = position;
        }
    }
}
