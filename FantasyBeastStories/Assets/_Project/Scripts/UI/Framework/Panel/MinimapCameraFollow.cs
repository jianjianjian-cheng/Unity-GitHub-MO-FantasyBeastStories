using Controllers.Player;
using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [SerializeField] private float height = 50f;
    private Transform _player;

    void LateUpdate()
    {
        if (_player == null)
        {
            // 通过 PlayerManager 获取本地玩家
            var players = PlayerManager.instance?.ActivePlayerObjects;
            if (players != null && players.Count > 0)
                _player = players[0].transform;
            return;
        }
        // 只跟随 XZ 位置，不跟随旋转
        transform.position = new Vector3(_player.position.x, height, _player.position.z);
    }
}