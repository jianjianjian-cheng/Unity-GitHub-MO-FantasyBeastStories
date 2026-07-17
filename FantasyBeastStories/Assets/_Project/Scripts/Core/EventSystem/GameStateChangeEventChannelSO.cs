using UnityEngine;
using Core.Channels.Base;

namespace Core.Channels.Game
{
    public enum GameState { Lobby, Playing, Paused, GameOver }

    [CreateAssetMenu(menuName = "Events/Game/State Change Channel")]
    public class GameStateChangeEventChannelSO : BaseEventChannelSO<GameState> { }
}
