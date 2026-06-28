using UnityEngine;

namespace Domain.Event
{
    [CreateAssetMenu(menuName = "Events/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("配置项（Inspector 设置）")]
        public bool IsTest;
        public bool IsStayLobby;
    }
}