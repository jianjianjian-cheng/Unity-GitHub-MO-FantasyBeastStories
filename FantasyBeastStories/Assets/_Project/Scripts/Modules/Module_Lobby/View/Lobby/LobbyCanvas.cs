using Core;
using Core.Channels.General;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Audio;

namespace UI.Lobby
{
  public class LobbyCanvas : MonoBehaviour
  {
      protected virtual void Start()
      {
          AudioManager.Instance.PlayBGM("bgm_main_menu");
      }

      protected virtual void OnEnable()
      {
          EventChannelLocator.MainContainer.roomJoinedChannel.RegisterListener(OnRoomJoined);
          SceneManager.sceneUnloaded += OnSceneUnloaded;
      }

      protected virtual void OnDisable()
      {
          if (EventChannelLocator.MainContainer != null)
              EventChannelLocator.MainContainer.roomJoinedChannel.UnregisterListener(OnRoomJoined);
          SceneManager.sceneUnloaded -= OnSceneUnloaded;
      }

      private void OnRoomJoined(RoomJoinedEventData data)
      {
          // 子 Widget 通过 OnEnable/OnDisable 自动管理事件订阅
          // 此处无需额外操作
      }

      private void OnSceneUnloaded(Scene scene)
      {
          // 场景卸载时自动销毁
      }
  }

}