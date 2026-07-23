using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Core;
using Controllers.Player;
using Core.Contracts;
using Core.Network;
using Managers;
using UnityEngine;

namespace Controllers.Character
{
    /// <summary>
    /// 观战摄像机控制器
    /// 当本地玩家死亡后激活，通过鼠标左/右键切换观察队友
    /// 挂载在玩家预制体的 VirtualCamera 所在 GameObject 上
    /// </summary>
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class SpectatorCameraController : MonoBehaviour
    {
        [SerializeField] private float deathDelay = 2f;       // 死亡后等待特效播放的时间
        [SerializeField] private float blendDuration = 1.5f;   // 摄像机切换 blend 时长

        private CinemachineVirtualCamera virtualCamera;
        private CinemachineBrain cinemachineBrain;
        private List<Transform> aliveTeammates = new List<Transform>();
        private int currentIndex = 0;
        private bool isActive = false;
        private int localActorNumber;

        void Awake()
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        void Start()
        {
            localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            // 获取或创建 CinemachineBrain 上的自定义 blend 定义
            cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>();
        }

        /// <summary>由 PlayerController.HandleDeath() 调用，激活观战模式</summary>
        public void ActivateSpectator()
        {
            StartCoroutine(DelayedActivateSpectator());
        }

        private IEnumerator DelayedActivateSpectator()
        {
            // 等待死亡特效播放完成
            yield return new WaitForSeconds(deathDelay);

            isActive = true;
            RefreshAliveTeammates();

            if (aliveTeammates.Count > 0)
            {
                currentIndex = Random.Range(0, aliveTeammates.Count);
                SetCameraTarget(aliveTeammates[currentIndex]);
            }
            else
            {
                HandleAllTeammatesDead();
            }
        }

        void Update()
        {
            if (!isActive) return;

            // 鼠标左键 → 上一个队友
            if (Input.GetMouseButtonDown(0))
            {
                CycleToTeammate(-1);
            }

            // 鼠标右键 → 下一个队友
            if (Input.GetMouseButtonDown(1))
            {
                CycleToTeammate(1);
            }

            // 检查当前观察目标是否已死亡或已移除
            CheckCurrentTargetAlive();
        }

        private void CycleToTeammate(int direction)
        {
            RefreshAliveTeammates();
            if (aliveTeammates.Count == 0)
            {
                HandleAllTeammatesDead();
                return;
            }

            currentIndex = (currentIndex + direction + aliveTeammates.Count) % aliveTeammates.Count;
            SetCameraTarget(aliveTeammates[currentIndex]);
        }

        private void SetCameraTarget(Transform target)
        {
            // 通过临时 blend 实现平滑切换
            if (cinemachineBrain != null)
            {
                cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Style.EaseInOut,
                    blendDuration
                );
            }

            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }

        private void RefreshAliveTeammates()
        {
            aliveTeammates = PlayerManager.instance?.GetAliveTeammateTransforms(localActorNumber)
                             ?? new List<Transform>();
        }

        /// <summary>每帧检查当前观察的队友是否还活着</summary>
        private void CheckCurrentTargetAlive()
        {
            RefreshAliveTeammates();
            if (aliveTeammates.Count == 0)
            {
                HandleAllTeammatesDead();
                return;
            }
            if (currentIndex >= aliveTeammates.Count ||
                aliveTeammates[currentIndex] == null)
            {
                currentIndex = Mathf.Clamp(currentIndex, 0, aliveTeammates.Count - 1);
                SetCameraTarget(aliveTeammates[currentIndex]);
            }
        }

        private void HandleAllTeammatesDead()
        {
            isActive = false;
            Debug.Log("[SpectatorCameraController] 所有队友已死亡，返回大厅");

            // 延迟2秒后返回大厅
            if (GameManager.instance != null)
            {
                GameManager.instance.TriggerLobbyTransition(2f);
            }
        }
    }
}
