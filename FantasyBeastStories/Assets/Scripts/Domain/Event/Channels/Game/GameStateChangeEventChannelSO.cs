using UnityEngine;
using Domain.Event.Channels.Base;

namespace Domain.Event.Channels.Game
{
    public enum GameState { Lobby, Playing, Paused, GameOver }

    [CreateAssetMenu(menuName = "Events/Game/State Change Channel")]
    public class GameStateChangeEventChannelSO : BaseEventChannelSO<GameState> { }
}
