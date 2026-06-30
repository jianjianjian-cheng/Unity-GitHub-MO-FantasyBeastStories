using Domain.Event;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
using UnityEngine;

namespace Presentation.UI
{
    /// <summary>
    /// 任务弹窗 Screen — 任务激活时弹出，任务结束时关闭。
    ///
    /// 内部包含三个子控件：
    ///   TaskNotice      — 任务名称、倒计时、进度
    ///   TaskPreface     — 任务序言文字（浮现动画）
    ///   DirectionIndicator — 任务目标方向指示器
    ///
    /// 生命周期：
    ///   ShowNotice → Open() → 填充数据 → OnAfterOpen 子控件浮现
    ///   HideNotice → OnBeforeClose 子控件隐藏 → Close() → 弹窗收起
    /// </summary>
    public class TaskScreen : UIScreen
    {
        [Header("任务 UI 组件引用（留空自动查找子物体）")]
        [SerializeField] private TaskNotice taskNotice;
        [SerializeField] private TaskPreface taskPreface;
        [SerializeField] private DirectionIndicator directionIndicator;

        // 缓存 ShowNotice 事件数据，供 OnBeforeOpen 使用
        private TaskUIUpdateData _pendingData;

        // ──────────────────────────────────────────────
        //  UIScreen 生命周期
        // ──────────────────────────────────────────────

        protected override void Awake()
        {
            screenId = "TaskScreen";
            defaultLayer = Framework.UILayer.HUD;  // HUD 层，常驻显示
            closeOnEsc = false;           // 不允许按 Esc 关闭
            useMask = false;              // 不需要半透明遮罩

            base.Awake();   // 会 SetActive(false)，一开始不可见

            UIManager.Instance.RegisterScreen(this);

            // 自动查找子组件
            if (taskNotice == null)
                taskNotice = GetComponentInChildren<TaskNotice>(true);
            if (taskPreface == null)
                taskPreface = GetComponentInChildren<TaskPreface>(true);
            if (directionIndicator == null)
                directionIndicator = GetComponentInChildren<DirectionIndicator>(true);

            // 立即订阅（不依赖 OnEnable，因为一开始是 inactive）
            SubscribeEvents();
        }

        protected void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ──────────────────────────────────────────────
        //  Channel 订阅
        // ──────────────────────────────────────────────

        protected override void SubscribeEvents()
        {
            if (EventChannelLocator.MainContainer?.taskUIChannel != null)
                EventChannelLocator.MainContainer.taskUIChannel.RegisterListener(OnTaskUIEvent);
        }

        protected override void UnsubscribeEvents()
        {
            if (EventChannelLocator.MainContainer?.taskUIChannel != null)
                EventChannelLocator.MainContainer.taskUIChannel.UnregisterListener(OnTaskUIEvent);
        }

        // ──────────────────────────────────────────────
        //  事件处理
        // ──────────────────────────────────────────────

        private void OnTaskUIEvent(TaskUIUpdateData data)
        {
            switch (data.eventType)
            {
                case TaskUIEventType.ShowNotice:
                    HandleShowNotice(data);
                    break;

                case TaskUIEventType.HideNotice:
                    HandleHideNotice();
                    break;

                case TaskUIEventType.UpdateTime:
                    if (taskNotice != null)
                        taskNotice.UpDateTime(data.timeString);
                    break;

                case TaskUIEventType.UpdateProgress:
                    if (taskNotice != null)
                        taskNotice.Notice_Data($"{data.data}");
                    break;

                case TaskUIEventType.SetIndicator:
                    if (directionIndicator != null)
                        directionIndicator.SetTargetAndImage(data.targetPosition, data.taskId);
                    break;

                case TaskUIEventType.ClearIndicator:
                    if (directionIndicator != null)
                        directionIndicator.SetTargetName(null);
                    break;

                case TaskUIEventType.NoticeData:
                    if (taskNotice != null)
                        taskNotice.Notice_Data(data.data);
                    break;
            }
        }

        private void HandleShowNotice(TaskUIUpdateData data)
        {
            _pendingData = data;

            if (!IsOpen)
                Open();           // 第一次：打开弹窗
            else
                UpdateContent(data); // 已打开：直接更新内容
        }

        private void HandleHideNotice()
        {
            if (IsOpen)
                Close();
        }

        // ──────────────────────────────────────────────
        //  Open / Close 流程钩子
        // ──────────────────────────────────────────────

        /// <summary>打开弹窗前填充数据</summary>
        protected override void OnBeforeOpen()
        {
            base.OnBeforeOpen();
            UpdateContent(_pendingData);
        }

        /// <summary>打开弹窗后播子控件浮现动画</summary>
        protected override void OnAfterOpen()
        {
            base.OnAfterOpen();

            if (taskPreface != null)
                taskPreface.PlayTextAnimation(true);
        }

        /// <summary>关闭弹窗前先播子控件隐藏动画</summary>
        protected override void OnBeforeClose()
        {
            base.OnBeforeClose();

            if (taskNotice != null)
                taskNotice.PlaySlideAnimation(false);
            if (taskPreface != null)
                taskPreface.PlayTextAnimation(false);
            if (directionIndicator != null)
                directionIndicator.SetTargetName(null);
        }

        // ──────────────────────────────────────────────
        //  内部方法
        // ──────────────────────────────────────────────

        private void UpdateContent(TaskUIUpdateData data)
        {
            if (taskNotice != null)
                taskNotice.SetInfo(data.taskName, data.description, data.limitTime, data.requiredCount);
            if (taskPreface != null)
                taskPreface.SetText(data.description);
        }
    }
}