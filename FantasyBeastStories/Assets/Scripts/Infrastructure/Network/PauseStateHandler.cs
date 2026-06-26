using Domain.Event;
using Domain.Event.Channels.Game;
using UnityEngine;
using UnityEngine.AI;

namespace Infrastructure.Network
{
    /// <summary>
    /// 暂停状态处理器（Infrastructure 层）
    /// 监听 Application 层广播的暂停事件，直接操作 Unity 引擎组件
    /// 负责：冻结/恢复所有 Animator、NavMeshAgent、Rigidbody
    /// </summary>
    public class PauseStateHandler : MonoBehaviour
    {
        void OnEnable()
        {
            var pauseChannel = EventChannelLocator.MainContainer.pauseStateChannel;
            if (pauseChannel != null)
            {
                pauseChannel.RegisterListener(OnPauseStateChanged);
            }
        }

        void OnDisable()
        {
            var pauseChannel = EventChannelLocator.MainContainer.pauseStateChannel;
            if (pauseChannel != null)
            {
                pauseChannel.UnregisterListener(OnPauseStateChanged);
            }
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                FreezeAllAnimations();
                FreezeAllMovement();
            }
            else
            {
                ResumeAllAnimations();
                ResumeAllMovement();
            }
        }

        private void FreezeAllAnimations()
        {
            Animator[] allAnimators = FindObjectsOfType<Animator>();
            foreach (Animator animator in allAnimators)
            {
                animator.speed = 0f;
                animator.Update(0f);
            }
        }

        private void ResumeAllAnimations()
        {
            Animator[] allAnimators = FindObjectsOfType<Animator>();
            foreach (Animator animator in allAnimators)
            {
                animator.speed = 1f;
            }
        }

        private void FreezeAllMovement()
        {
            NavMeshAgent[] allAgents = FindObjectsOfType<NavMeshAgent>();
            foreach (NavMeshAgent agent in allAgents)
            {
                agent.isStopped = true;
            }

            Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody rb in allRigidbodies)
            {
                rb.isKinematic = true;
            }
        }

        private void ResumeAllMovement()
        {
            NavMeshAgent[] allAgents = FindObjectsOfType<NavMeshAgent>();
            foreach (NavMeshAgent agent in allAgents)
            {
                agent.isStopped = false;
            }

            Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody rb in allRigidbodies)
            {
                rb.isKinematic = false;
            }
        }
    }
}