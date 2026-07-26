using Core;
using Photon.Pun;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private string bossPrefabPath = "Enemies/Boss_Horror";

    void OnEnable()
    {
        EventChannelLocator.MainContainer.bossSpawnChannel.RegisterListener(OnBossSpawn);
    }

    void OnDisable()
    {
        EventChannelLocator.MainContainer.bossSpawnChannel.UnregisterListener(OnBossSpawn);
    }

    private void OnBossSpawn(string bossName)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Boss 只生成一次，不走对象池，直接用 Photon 原生网络实例化
        var savedPool = PhotonNetwork.PrefabPool;
        PhotonNetwork.PrefabPool = new DefaultPool();
        PhotonNetwork.InstantiateRoomObject(bossPrefabPath,
            spawnPoint.position, spawnPoint.rotation);
        PhotonNetwork.PrefabPool = savedPool;
    }
}
