using Core;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 任务UI控制器 — 监听 taskUIChannel 并驱动 UI 组件
    /// 负责：任务通知弹窗、任务序言、方向指示器
    ///
    /// 如果 Inspector 未绑定组件，会在 Awake 时自动查找子物体。
    /// </summary>
    public class TaskUIController : MonoBehaviour
    {
        [Header("UI组件绑定（留空自动查找）")]
        [SerializeField] private TaskNotice taskNotice;
        [SerializeField] private TaskPreface taskPreface;
        [SerializeField] private DirectionIndicator directionIndicator;

        private void Awake()
        {
            // 没有手动绑定则自动在子物体中查找
            if (taskNotice == null)
                taskNotice = GetComponentInChildren<TaskNotice>(true);
            if (taskPreface == null)
                taskPreface = GetComponentInChildren<TaskPreface>(true);
            if (directionIndicator == null)
                directionIndicator = GetComponentInChildren<DirectionIndicator>(true);
        }

        private void OnEnable()
        {
            if (EventChannelLocator.MainContainer.taskUIChannel != null)
            {
                EventChannelLocator.MainContainer.taskUIChannel.RegisterListener(OnTaskUIEvent);
            }
        }

        private void OnDisable()
        {
            if (EventChannelLocator.MainContainer.taskUIChannel != null)
            {
                EventChannelLocator.MainContainer.taskUIChannel.UnregisterListener(OnTaskUIEvent);
            }
        }

        private void OnTaskUIEvent(TaskUIUpdateData data)
        {
            switch (data.eventType)
            {
                case TaskUIEventType.ShowNotice:
                    HandleShowNotice(data);
                    break;

                case TaskUIEventType.HideNotice:
                    HandleHideNotice(data);
                    break;

                case TaskUIEventType.UpdateTime:
                    HandleUpdateTime(data);
                    break;

                case TaskUIEventType.UpdateProgress:
                    HandleUpdateProgress(data);
                    break;

                case TaskUIEventType.SetIndicator:
                    HandleSetIndicator(data);
                    break;

                case TaskUIEventType.ClearIndicator:
                    HandleClearIndicator(data);
                    break;

                case TaskUIEventType.NoticeData:
                    HandleNoticeData(data);
                    break;
            }
        }

        private void HandleShowNotice(TaskUIUpdateData data)
        {
            if (taskNotice != null)
            {
                taskNotice.SetInfo(data.taskName, data.description, data.limitTime, data.requiredCount);
            }
            if (taskPreface != null)
            {
                taskPreface.SetText(data.description);
                taskPreface.PlayTextAnimation(true);
            }
        }

        private void HandleHideNotice(TaskUIUpdateData data)
        {
            if (taskNotice != null)
                taskNotice.PlaySlideAnimation(false);
            if (taskPreface != null)
                taskPreface.PlayTextAnimation(false);
            if (directionIndicator != null)
                directionIndicator.SetTargetName(null);
        }

        private void HandleUpdateTime(TaskUIUpdateData data)
        {
            if (taskNotice != null)
                taskNotice.UpDateTime(data.timeString);
        }

        private void HandleUpdateProgress(TaskUIUpdateData data)
        {
            if (taskNotice != null)
                taskNotice.Notice_Data($"{data.data}");
        }

        private void HandleSetIndicator(TaskUIUpdateData data)
        {
            if (directionIndicator != null)
                directionIndicator.SetTargetAndImage(data.targetPosition, data.taskId);
        }

        private void HandleClearIndicator(TaskUIUpdateData data)
        {
            if (directionIndicator != null)
                directionIndicator.SetTargetName(null);
        }

        private void HandleNoticeData(TaskUIUpdateData data)
        {
            if (taskNotice != null)
                taskNotice.Notice_Data(data.data);
        }
    }
}