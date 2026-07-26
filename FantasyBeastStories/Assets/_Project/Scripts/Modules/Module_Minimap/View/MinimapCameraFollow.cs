using Controllers.Player;
using UnityEngine;
using Managers;

public class MinimapCameraFollow : MonoBehaviour
{
    [SerializeField] private float height = 50f;
    private Transform _player;

    void LateUpdate()
    {
        if (_player == null)
        {
            var players = ServiceLocator.Get<PlayerManager>()?.ActivePlayerObjects;
            if (players == null || players.Count == 0) return;

            foreach (var go in players)
            {
                if (go == null) continue;
                var pc = go.GetComponent<Controllers.Character.PlayerController>();
                if (pc != null && pc.IsLocalPlayer())
                {
                    _player = go.transform;
                    break;
                }
            }

            if (_player == null) return;
        }
        // 只跟随 XZ 位置，不跟随旋转
        transform.position = new Vector3(_player.position.x, height, _player.position.z);
    }
}