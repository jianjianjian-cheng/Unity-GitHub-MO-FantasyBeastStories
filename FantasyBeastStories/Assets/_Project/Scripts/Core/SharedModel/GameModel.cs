using System.Collections.Generic;
using Core.Contracts;

namespace Core.SharedModel
{
    /// <summary>
    /// 游戏模型 — 纯 C# 类，不继承 MonoBehaviour，可独立单元测试。
    ///
    /// 持有：
    /// - 战斗状态标记 (isInBattle)
    /// - 生成点字典 (id → ISpawnPoint)
    /// - 生成点查找逻辑
    ///
    /// 外部依赖（SceneManager / EventChannelSO / NetworkServiceLocator / AudioManager / Coroutine）
    /// 由 Controller 处理，Model 只管理数据。
    /// </summary>
    public class GameModel
    {
        // ──────────────────────────────────
        //  状态
        // ──────────────────────────────────

        public bool IsInBattle { get; private set; }

        // ──────────────────────────────────
        //  生成点
        // ──────────────────────────────────

        private readonly Dictionary<int, ISpawnPoint> _spawnPointDict = new();

        public void SetIsInBattle(bool value) => IsInBattle = value;

        // ──────────────────────────────────
        //  生成点管理
        // ──────────────────────────────────

        public void ClearSpawnPoints() => _spawnPointDict.Clear();

        public void RegisterSpawnPoint(int id, ISpawnPoint spawnPoint)
        {
            _spawnPointDict[id] = spawnPoint;
        }

        public ISpawnPoint GetSpawnPointById(int id)
        {
            _spawnPointDict.TryGetValue(id, out ISpawnPoint sp);
            return sp;
        }

        public ISpawnPoint GetSpawnPointByPlayer(int actorNumber)
        {
            foreach (var sp in _spawnPointDict.Values)
            {
                if (sp.GetOccupiedByPlayer() == actorNumber)
                    return sp;
            }
            return null;
        }

        /// <summary>根据 ActorNumber 确定性分配生成点</summary>
        public ISpawnPoint GetSpawnPointForPlayer(int actorNumber)
        {
            if (_spawnPointDict.Count == 0) return null;

            var sortedIds = new List<int>(_spawnPointDict.Keys);
            sortedIds.Sort();

            int index = (actorNumber - 1) % sortedIds.Count;
            return _spawnPointDict[sortedIds[index]];
        }

        public int SpawnPointCount => _spawnPointDict.Count;
    }
}
