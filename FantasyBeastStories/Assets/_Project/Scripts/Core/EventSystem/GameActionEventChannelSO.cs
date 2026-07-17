using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.General
{
    public enum GameActionType
    {
        QuitToLobby,
        QuitToMainMenu,
        SwitchCharacter,
        SetLocalReady,
        SyncAllPlayers,
        UpgradeAllConfirmed
    }

    [CreateAssetMenu(menuName = "Events/General/Game Action Event Channel")]
    public class GameActionEventChannelSO : BaseEventChannelSO<GameActionType>
    {
    }
}
