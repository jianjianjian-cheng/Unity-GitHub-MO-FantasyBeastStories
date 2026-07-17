using Core;
using Controllers.Services;
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
        if (!NetworkServiceLocator.PlayerService.IsMasterClient) return;

        // 选择生成点
        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        NetworkServiceLocator.ObjectService.InstantiateRoomObject(bossPrefabPath,
            spawnPoint.position, spawnPoint.rotation);
    }
}