using System.Collections;
using System.Collections.Generic;
using Core;
using Core.SharedModel;
using Core.Channels.Combat;
using Core.Contracts;
using Core.Network;
using Core.Channels.Task;
using Core.Channels.Player;
using Controllers.Task;
using Controllers.Network;
using UI;
using UnityEngine;
using Controllers.Task;

namespace Controllers.Task
{
    /// <summary>
    /// 任务控制器 — 薄层 MonoBehaviour，持有 TaskModel 实例。
    ///
    /// 职责：
    /// - 生命周期管理（单例）
    /// - RPC 网络同步
    /// - 协程（倒计时）
    /// - GameObject 管理（taskZone）
    /// - Gizmos 绘制
    /// - 外部依赖（AssetLoader / EventChannelSO / MagicUpgradeManager）
    /// - 业务逻辑委托给 TaskModel
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        #region 单例模式
        
        

        /// <summary>任务模型实例（纯 C#，可单测）</summary>
        public TaskModel Model { get; private set; }
        #endregion

        [Header("可视化设置")]
        [SerializeField] private bool showZoneGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);

        private GameObject _taskZone;
        private Coroutine _taskRoutine;

        void Awake()
        {
                  ServiceLocator.Register(this);
            Model = new TaskModel();

            Model.OnTaskCompleted += OnTaskCompletedFun;
        }

        private void OnEnable()
        {
            EventChannelLocator.MainContainer.taskNoticeChannel.RegisterListener(OnTaskNoticeReceived);
            EventChannelLocator.MainContainer.enemyReportChannel.RegisterListener(OnEnemyReported);
            EventChannelLocator.MainContainer.taskActivationChannel.RegisterListener(OnTaskActivationReceived);
        }

        private void OnDisable()
        {
            EventChannelLocator.MainContainer.taskNoticeChannel.UnregisterListener(OnTaskNoticeReceived);
            EventChannelLocator.MainContainer.enemyReportChannel.UnregisterListener(OnEnemyReported);
            EventChannelLocator.MainContainer.taskActivationChannel.UnregisterListener(OnTaskActivationReceived);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister<TaskManager>();
        }

        // ──────────────────────────────────
        //  事件监听
        // ──────────────────────────────────

        private void OnEnemyReported(EnemyReportData data)
        {
            if (data == null) return;
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_ReportCount",
                NetworkTarget.MasterClient, data.position, data.networkViewID, (int)data.reportType);
        }

        private void OnTaskNoticeReceived(TaskNoticeData data)
        {
            if (data == null) return;
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_SetNotice",
                NetworkTarget.All, data.name, data.description, data.limitTime, data.requiredCount);
        }

        private void OnTaskActivationReceived(TaskBase task)
        {
            if (task == null) return;
            ActivateTask(task);
        }

        // ──────────────────────────────────
        //  公共方法
        // ──────────────────────────────────

        public void SetNotice(string name, string description, int limitTime, int requeredCount = 1)
        {
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_SetNotice",
                NetworkTarget.All, name, description, limitTime, requeredCount);
        }

        public void ActivateTask(TaskBase taskBase)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            switch (taskBase)
            {
                case KillTask killTask:
                    NetworkServiceLocator.ObjectService.InvokeRPC(
                        ManagerRpcBridge.Instance, "RPC_ActivateKillTask",
                        NetworkTarget.All,
                        killTask.TaskId, killTask.limitTime, killTask.ZoneCenter, killTask.RequiredKills);
                    break;
                case EscortTask escortTask:
                    NetworkServiceLocator.ObjectService.InvokeRPC(
                        ManagerRpcBridge.Instance, "RPC_ActivateEscortTask",
                        NetworkTarget.All,
                        escortTask.TaskId, escortTask.limitTime, escortTask.ZoneCenter, escortTask.requiredEscorts);
                    break;
            }
        }

        public void ReportCount(Vector3 killPosition, int enemyViewID, int reportType)
        {
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_ReportCount",
                NetworkTarget.MasterClient, killPosition, enemyViewID, reportType);
        }

        // ──────────────────────────────────
        //  倒计时协程
        // ──────────────────────────────────

        private void StartCountdownTime(int time)
        {
            if (_taskRoutine != null)
                StopCoroutine(_taskRoutine);

            _taskRoutine = StartCoroutine(TaskRoutine(time));
        }

        IEnumerator TaskRoutine(int countdownTime)
        {
            while (countdownTime > 0)
            {
                yield return new WaitForSeconds(1f);
                countdownTime--;
                int min = Mathf.FloorToInt(countdownTime / 60);
                int sec = Mathf.FloorToInt(countdownTime % 60);
                string time = "剩余时间：" + $"{min:D2}:{sec:D2}";
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_UpdateAllPlayerTimeUI",
                    NetworkTarget.All, time);
            }

            Debug.LogWarning("任务失败");
            NetworkServiceLocator.ObjectService.InvokeRPC(
                ManagerRpcBridge.Instance, "RPC_TaskFailed", NetworkTarget.All);
        }

        // ──────────────────────────────────
        //  静态 RPC Handler（供 ManagerRpcBridge 调用）
        // ──────────────────────────────────

        public static void HandleUpdateAllPlayerTimeUIRPC(string time)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.UpdateTime(time));
        }

        public static void HandleTaskFailedRPC()
        {
            if (!ServiceLocator.TryGet<TaskManager>(out var inst)) return;
            if (inst._taskZone == null) return;

            Destroy(inst._taskZone);
            inst._taskZone = null;
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.ClearIndicator());
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.HideNotice());
        }

        public static void HandleActivateKillTaskRPC(string taskId, int limitTime, Vector3 zoneCenter, int requiredKills)
        {
            if (!ServiceLocator.TryGet<TaskManager>(out var inst)) return;

            if (NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.StartCountdownTime(limitTime);

            inst._taskZone = AssetLoader.Instantiate("Level1_TaskPrefab_" + taskId, zoneCenter, Quaternion.identity);

            inst.Model.ClearTasks();
            var killTask = new KillTask(taskId, zoneCenter, 7, requiredKills, limitTime);
            inst.Model.AddTask(taskId, killTask);

            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.SetIndicator(zoneCenter, taskId));
        }

        public static void HandleActivateEscortTaskRPC(string taskId, int limitTime, Vector3 zoneCenter, int requiredEscorts)
        {
            if (!ServiceLocator.TryGet<TaskManager>(out var inst)) return;

            if (NetworkServiceLocator.PlayerService.IsMasterClient)
                inst.StartCountdownTime(limitTime);

            inst._taskZone = AssetLoader.Instantiate("Level1_TaskPrefab_" + taskId, zoneCenter, Quaternion.identity);

            inst.Model.ClearTasks();
            var escortTask = new EscortTask(taskId, zoneCenter, 4, requiredEscorts, limitTime);
            inst.Model.AddTask(escortTask.TaskId, escortTask);

            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.SetIndicator(zoneCenter, taskId));
        }

        public static void HandleReportCountRPC(Vector3 killPosition, int enemyViewID, int reportTypeInt)
        {
            if (!ServiceLocator.TryGet<TaskManager>(out var inst)) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            EnemyReportType reportType = (EnemyReportType)reportTypeInt;

            var update = inst.Model.ReportKill(killPosition, enemyViewID, reportType);
            if (update != null)
            {
                NetworkServiceLocator.ObjectService.InvokeRPC(
                    ManagerRpcBridge.Instance, "RPC_UpdateProgress",
                    NetworkTarget.All, update.TaskId, update.Count, update.IsCompleted);
            }
        }

        public static void HandleUpdateProgressRPC(string taskId, int count, bool completed)
        {
            if (!ServiceLocator.TryGet<TaskManager>(out var inst)) return;

            bool wasCompleted = inst.Model.UpdateProgress(taskId, count, completed);

            if (wasCompleted)
            {
                inst.NoticeData($"{count}/{count}");
            }
            else
            {
                if (inst.Model.TryGetTask(taskId, out var task))
                {
                    switch (task)
                    {
                        case KillTask kt:
                            inst.NoticeData($"{count}/{kt.RequiredKills}");
                            break;
                        case EscortTask et:
                            inst.NoticeData($"{count}/{et.requiredEscorts}");
                            break;
                    }
                }
            }
        }

        // ──────────────────────────────────
        //  任务完成回调
        // ──────────────────────────────────

        private void OnTaskCompletedFun(TaskBase task)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.ClearIndicator());

            switch (task)
            {
                case KillTask:
                    NoticeData("击杀任务完成！");
                    StartCoroutine(DelayReward(0.5f));
                    break;
                case EscortTask:
                    NoticeData("护送任务完成！");
                    StartCoroutine(DelayReward(12f));
                    break;
            }
        }

        private void NoticeData(string data)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.NoticeData(data));
        }

        IEnumerator DelayReward(float delay)
        {
            yield return new WaitForSeconds(delay);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.HideNotice());

            if (_taskZone != null)
                Destroy(_taskZone);
            _taskZone = null;

            if (_taskRoutine != null)
                StopCoroutine(_taskRoutine);

            if (ServiceLocator.Get<MagicUpgradeManager>() != null)
                ServiceLocator.Get<MagicUpgradeManager>().isAllExCard = true;

            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);

            var expQuery = new SkillQueryData(SkillQueryType.GetUpgradeExperience);
            EventChannelLocator.MainContainer.skillQueryChannel.Raise(expQuery);
            EventChannelLocator.MainContainer.experienceChannel.Raise(expQuery.intValue);
        }

        // ──────────────────────────────────
        //  Gizmos
        // ──────────────────────────────────

        void OnDrawGizmos()
        {
            if (!showZoneGizmos || Model == null) return;

            foreach (var task in Model.GetAllTasks())
            {
                Gizmos.color = task.IsCompleted
                    ? new Color(1, 1, 0, 0.3f)
                    : gizmoColor;

                Gizmos.DrawWireSphere(task.ZoneCenter, task.ZoneRadius);
                Gizmos.DrawSphere(task.ZoneCenter, 0.3f);
            }
        }
    }

    public enum TaskType
    {
        Kill,
        Escort,
    }

    public class TaskConst
    {
        public const string KillSacrifice = "KillSacrifice";
    }
}
