using System;
using UnityEngine;

namespace Domain.Player
{
    [Serializable]
    public class PlayerData
    {
        public string PlayerId { get; private set; }
        public string PlayerName { get; private set; }

        public PlayerData(string playerId, string playerName)
        {
            // 防止 null 的终极处理
            PlayerId = string.IsNullOrEmpty(playerId) ? "unknown_" + Guid.NewGuid().ToString().Substring(0, 8) : playerId;
            PlayerName = string.IsNullOrEmpty(playerName) ? "未命名玩家" : playerName;
        }
    }
}