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
        [SerializeField] private float blendDuration = 1.5f;   // 摄像机平移时长

        private CinemachineVirtualCamera virtualCamera;
        private CinemachineBrain cinemachineBrain;
        private List<Transform> aliveTeammates = new List<Transform>();
        private int currentIndex = 0;
        private bool isActive = false;
        private int localActorNumber;

        private Coroutine _panCoroutine;
        private GameObject _panBridge;

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
            // 取消正在进行的平移
            if (_panCoroutine != null)
                StopCoroutine(_panCoroutine);

            // 清理上一轮的桥接对象
            if (_panBridge != null)
                Destroy(_panBridge);

            _panCoroutine = StartCoroutine(SmoothPanToTarget(target, blendDuration));
        }

        /// <summary>
        /// 使用桥接 GameObject 平滑平移摄像机：
        /// 创建一个位于当前 Follow 目标位置的桥接对象，
        /// 让虚拟摄像机跟随桥接对象，再将桥接对象平滑移动到新目标位置，
        /// 完成后将 Follow/LookAt 直接切换到新目标。
        /// </summary>
        private IEnumerator SmoothPanToTarget(Transform newTarget, float duration)
        {
            if (newTarget == null)
                yield break;

            // 记录起始位置（当前 Follow 目标位置或摄像机自身位置）
            Vector3 startPos = virtualCamera.Follow != null
                ? virtualCamera.Follow.position
                : transform.position;

            // 创建桥接对象
            _panBridge = new GameObject("SpectatorPanBridge");
            _panBridge.transform.position = startPos;

            // 虚拟摄像机跟随桥接对象，始终注视新目标
            virtualCamera.Follow = _panBridge.transform;
            virtualCamera.LookAt = newTarget;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (newTarget == null)
                    break;

                elapsed += UnityEngine.Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseInOutQuad 缓动曲线
                float eased = t < 0.5f
                    ? 2f * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                _panBridge.transform.position = Vector3.Lerp(startPos, newTarget.position, eased);
                yield return null;
            }

            // 平移完成，直接跟随新目标
            if (newTarget != null)
            {
                virtualCamera.Follow = newTarget;
                virtualCamera.LookAt = newTarget;
            }

            if (_panBridge != null)
            {
                Destroy(_panBridge);
                _panBridge = null;
            }

            _panCoroutine = null;
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

            // 停止正在进行的平移并清理桥接对象
            if (_panCoroutine != null)
            {
                StopCoroutine(_panCoroutine);
                _panCoroutine = null;
            }
            if (_panBridge != null)
            {
                Destroy(_panBridge);
                _panBridge = null;
            }

            Debug.Log("[SpectatorCameraController] 所有队友已死亡，返回大厅");

            // 延迟2秒后返回大厅
            if (GameManager.instance != null)
            {
                GameManager.instance.TriggerLobbyTransition(2f);
            }
        }

        void OnDestroy()
        {
            if (_panBridge != null)
                Destroy(_panBridge);
        }
    }
}
