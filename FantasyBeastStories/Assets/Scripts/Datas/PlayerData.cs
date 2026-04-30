using System;
using Photon.Pun;
using UnityEngine;

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

    public static PlayerData FromPhotonPlayer(Photon.Realtime.Player player)
    {
        if (player == null)
        {
            Debug.LogError("[PlayerData] FromPhotonPlayer: player is null!");
            return null;
        }

        string userId = player.UserId;

        // 关键修复：UserId 可能为 null
        if (string.IsNullOrEmpty(userId))
        {
            userId = "Actor_" + player.ActorNumber;
            Debug.LogWarning($"[PlayerData] UserId 为空，使用 ActorNumber: {userId}");
        }

        string nickName = player.NickName;
        if (string.IsNullOrEmpty(nickName))
        {
            nickName = "Player_" + player.ActorNumber;
        }

        return new PlayerData(userId, nickName);
    }
}