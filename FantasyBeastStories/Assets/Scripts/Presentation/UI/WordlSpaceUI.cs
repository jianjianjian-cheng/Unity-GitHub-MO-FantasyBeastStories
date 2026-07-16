using System.Collections;
using System.Collections.Generic;
using Domain.Services;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Presentation.UI
{
  public class WordlSpaceUI : MonoBehaviour
  {
    [SerializeField] public Image isMineIcon;
    [SerializeField] public Text playerName;
    [SerializeField] public Text isReadyIcon;

    private int _ownerActorNumber = -1;

    void Start()
    {
      _ownerActorNumber = NetworkServiceLocator.ObjectService.GetOwnerActorNumber(this);

      // 确保事件订阅（防止 OnEnable 时服务未初始化导致漏订阅）
      if (NetworkServiceLocator.IsInitialized)
      {
        NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged -= OnPlayerPropertyChanged;
        NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged += OnPlayerPropertyChanged;
      }

      // 根据当前场景决定是否显示就绪文字（游戏场景中不显示，大厅中显示）
      bool isInLobby = SceneManager.GetActiveScene().buildIndex <= 1;
      if (isReadyIcon != null)
      {
        isReadyIcon.gameObject.SetActive(isInLobby);

        // 在大厅中初始化就绪文字（处理 UI 创建时玩家已是就绪状态的情况）
        if (isInLobby && _ownerActorNumber >= 0)
        {
          object readyValue = NetworkServiceLocator.PlayerService.GetPlayerCustomProperty(_ownerActorNumber, "PlayerReady");
          bool isReady = readyValue is bool b && b;
          isReadyIcon.text = isReady ? "已就绪" : "未就绪";
        }
      }
    }

    void Update()
    {
      if (NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
      {
        isMineIcon.gameObject.SetActive(true);
        playerName.text = " ";
      }
      else
      {
        isMineIcon.gameObject.SetActive(false);
        playerName.text = NetworkServiceLocator.ObjectService.GetOwnerNickname(this);
      }
      //名称一直朝向摄像机
      Vector3 direction = transform.position - Camera.main.transform.position;
      transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnEnable()
    {
      SceneManager.sceneLoaded += OnSceneLoaded;
      if (NetworkServiceLocator.IsInitialized)
        NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged += OnPlayerPropertyChanged;
    }

    void OnDisable()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      if (NetworkServiceLocator.IsInitialized)
        NetworkServiceLocator.PlayerService.OnPlayerPropertyChanged -= OnPlayerPropertyChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      if (scene.buildIndex > 1)
      {
        isReadyIcon.gameObject.SetActive(false);
        isMineIcon.gameObject.SetActive(false);
      }
      else if (scene.buildIndex == 1)
      {
        isReadyIcon.gameObject.SetActive(true);
      }
    }

    public void UpDatePlayerName(string name)
    {
      if (playerName == null) return;
      if (NetworkServiceLocator.PlayerService.IsOwnerOf(gameObject))
      {
        playerName.text = " ";
      }
      else
      {
        playerName.text = name;
      }
    }

    private void OnPlayerPropertyChanged(int actorNumber, string key, object value)
    {
      if (actorNumber != _ownerActorNumber)
        return;

      if (key == "PlayerName")
      {
        string newName = value as string ?? "";
        UpDatePlayerName(newName);
      }
      else if (key == "PlayerReady")
      {
        bool isReady = value is bool b && b;
        isReadyIcon.text = isReady ? "已就绪" : "未就绪";
      }
    }
  }
}