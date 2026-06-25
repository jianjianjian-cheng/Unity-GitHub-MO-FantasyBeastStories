using UnityEngine;

namespace Domain.Event
{
    [CreateAssetMenu(menuName = "Events/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        public bool IsTest;
        public bool IsPaused;
        public bool IsStayLobby;
        public bool IsOpenUI;
    }
}
