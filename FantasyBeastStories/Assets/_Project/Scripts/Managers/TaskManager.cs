using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Channels.Combat;
using Core.Channels.Player;
using Core.Channels.Task;
using Controllers.Services;
using NetworkTarget = Controllers.Network.NetworkTarget;
using Controllers.Task;
using Controllers.Network;
using UI;
using Unity.VisualScripting;
using UnityEngine;

namespace Managers
{
    public class TaskManager : MonoBehaviour
    {
        #region 单例模式
        public static TaskManager instance;
        public static TaskManager Instance => instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        [Header("可视化设置")]
        [SerializeField]
        private bool showZoneGizmos = true;

        private GameObject taskZone;

        [SerializeField]
        private Color gizmoColor = new Color(0, 1, 0, 0.3f);

        private Dictionary<string, TaskBase> tasks = new Dictionary<string, TaskBase>();

        private HashSet<int> reportedEnemies = new HashSet<int>();

        private int countdownTime = 0;
        private int taskDuration;
        private Coroutine taskRoutine;

        public event System.Action<TaskBase> OnTaskUpdated;
        public event System.Action<TaskBase> OnTaskCompleted;

        private void OnEnable()
        {
            OnTaskCompleted += OnTaskCompletedFun;
            EventChannelLocator.MainContainer.taskNoticeChannel.RegisterListener(OnTaskNoticeReceived);
            EventChannelLocator.MainContainer.enemyReportChannel.RegisterListener(OnEnemyReported);
            EventChannelLocator.MainContainer.taskActivationChannel.RegisterListener(OnTaskActivationReceived);
        }

        private void OnDisable()
        {
            OnTaskCompleted -= OnTaskCompletedFun;
            EventChannelLocator.MainContainer.taskNoticeChannel.UnregisterListener(OnTaskNoticeReceived);
            EventChannelLocator.MainContainer.enemyReportChannel.UnregisterListener(OnEnemyReported);
            EventChannelLocator.MainContainer.taskActivationChannel.UnregisterListener(OnTaskActivationReceived);
        }

        private void OnEnemyReported(EnemyReportData data)
        {
            if (data == null) return;
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_ReportCount", NetworkTarget.MasterClient, data.position, data.networkViewID, (int)data.reportType);
        }

        private void OnTaskNoticeReceived(TaskNoticeData data)
        {
            if (data == null) return;
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_SetNotice", NetworkTarget.All, data.name, data.description, data.limitTime, data.requiredCount);
        }

        /// <summary>
        /// 收到 taskActivationChannel 事件 → 分发到 ActivateTask
        /// </summary>
        private void OnTaskActivationReceived(TaskBase task)
        {
            if (task == null) return;
            ActivateTask(task);
        }

        public void SetNotice(string name, string description, int limitTime, int requeredCount = 1)
        {
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_SetNotice", NetworkTarget.All, name, description, limitTime, requeredCount);
        }

        private void StartCountdownTime(int time)
        {
            countdownTime = time;

            if (taskRoutine != null)
            {
                StopCoroutine(taskRoutine);
            }

            taskRoutine = StartCoroutine(TaskRoutine());
        }

        IEnumerator TaskRoutine()
        {
            string time = "";
            while (countdownTime > 0)
            {
                yield return new WaitForSeconds(1f);
                countdownTime--;
                int min = Mathf.FloorToInt(countdownTime / 60);
                int sec = Mathf.FloorToInt(countdownTime % 60);
                time = "剩余时间：" + $"{min:D2}:{sec:D2}";
                NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_UpdateAllPlayerTimeUI", NetworkTarget.All, time);
            }
            Debug.LogWarning("任务失败");
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_TaskFailed", NetworkTarget.All);
            yield break;
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：更新所有玩家的任务时间 UI
        /// </summary>
        public static void HandleUpdateAllPlayerTimeUIRPC(string time)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.UpdateTime(time));
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：任务失败
        /// </summary>
        public static void HandleTaskFailedRPC()
        {
            if (Instance == null) return;
            if (Instance.taskZone == null) return;
            Destroy(Instance.taskZone.gameObject);
            Instance.taskZone = null;
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.ClearIndicator());
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.HideNotice());
        }

        void Notice_Data(string data)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.NoticeData(data));
        }

        public void ActivateTask(TaskBase taskBase)
        {
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;
            switch (taskBase)
            {
                case KillTask killTask:
                    NetworkServiceLocator.ObjectService.InvokeRPC(
                        ManagerRpcBridge.Instance,
                        "RPC_ActivateKillTask",
                        NetworkTarget.All,
                        killTask.TaskId,
                        killTask.limitTime,
                        killTask.ZoneCenter,
                        killTask.RequiredKills
                    );
                    break;
                case EscortTask escortTask:
                    NetworkServiceLocator.ObjectService.InvokeRPC(
                        ManagerRpcBridge.Instance,
                        "RPC_ActivateEscortTask",
                        NetworkTarget.All,
                        escortTask.TaskId,
                        escortTask.limitTime,
                        escortTask.ZoneCenter,
                        escortTask.requiredEscorts
                    );
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：激活击杀任务
        /// </summary>
        public static void HandleActivateKillTaskRPC(string taskId, int limitTime, Vector3 zoneCenter, int requiredKills)
        {
            if (Instance == null) return;

            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.StartCountdownTime(limitTime);
            }
            Instance.taskZone = AssetLoader.Instantiate(
                "TaskPrefab/" + taskId,
                zoneCenter,
                Quaternion.identity
            );
            Instance.tasks.Clear();

            KillTask killTask = new KillTask(taskId, zoneCenter, 7, requiredKills, limitTime);
            Instance.tasks[taskId] = killTask;

            Debug.Log($"任务{taskId}已激活，中心位置：{zoneCenter}" + Instance.taskZone.name);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(
                TaskUIUpdateData.SetIndicator(zoneCenter, taskId));
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：激活护送任务
        /// </summary>
        public static void HandleActivateEscortTaskRPC(
            string taskId,
            int limitTime,
            Vector3 zoneCenter,
            int requiredEscorts
        )
        {
            if (Instance == null) return;

            if (NetworkServiceLocator.PlayerService.IsMasterClient)
            {
                Instance.StartCountdownTime(limitTime);
            }
            Instance.taskZone = AssetLoader.Instantiate(
                "TaskPrefab/" + taskId,
                zoneCenter,
                Quaternion.identity
            );
            Instance.tasks.Clear();
            EscortTask escortTask = new EscortTask(taskId, zoneCenter, 4, requiredEscorts, limitTime);
            Instance.tasks[escortTask.TaskId] = escortTask;
            Debug.Log($"任务{taskId}已激活，中心位置：{zoneCenter}" + Instance.taskZone.name);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(
                TaskUIUpdateData.SetIndicator(zoneCenter, taskId));
        }

        public void ReportCount(Vector3 killPosition, int enemyViewID, int reportType)
        {
            NetworkServiceLocator.ObjectService.InvokeRPC(ManagerRpcBridge.Instance, "RPC_ReportCount", NetworkTarget.MasterClient, killPosition, enemyViewID, reportType);
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：上报击杀计数
        /// </summary>
        public static void HandleReportCountRPC(Vector3 killPosition, int enemyViewID, int reportTypeInt)
        {
            if (Instance == null) return;
            if (!NetworkServiceLocator.PlayerService.IsMasterClient)
                return;

            EnemyReportType reportType = (EnemyReportType)reportTypeInt;

            if (Instance.reportedEnemies.Contains(enemyViewID))
            {
                return;
            }

            foreach (var task in Instance.tasks.Values)
            {
                if (task.IsCompleted)
                    continue;

                if (task is KillTask killTask)
                {
                    if (reportType != EnemyReportType.Kill)
                        continue;

                    KillTask kt = task as KillTask;
                    if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
                    {
                        kt.CurrentKills++;
                        Instance.reportedEnemies.Add(enemyViewID);
                        Debug.LogWarning(
                            $"任务{task.TaskId}击 增加1," + $"当前击杀次数：{kt.CurrentKills}"
                        );
                        if (kt.CurrentKills >= kt.RequiredKills)
                        {
                            task.IsCompleted = true;
                        }
                        NetworkServiceLocator.ObjectService.InvokeRPC(
                            ManagerRpcBridge.Instance,
                            "RPC_UpdateProgress",
                            NetworkTarget.All,
                            task.TaskId,
                            kt.CurrentKills,
                            task.IsCompleted
                        );

                        break;
                    }
                }

                if (task is EscortTask escortTask)
                {
                    if (reportType != EnemyReportType.EscortArrive)
                        continue;

                    if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
                    {
                        escortTask.currentEscorts++;
                        Instance.reportedEnemies.Add(enemyViewID);
                        Debug.LogWarning(
                            $"任务{task.TaskId}击 增加1,"
                                + $"当前运输机器人：{escortTask.currentEscorts}"
                        );
                        if (escortTask.currentEscorts >= escortTask.requiredEscorts)
                        {
                            escortTask.IsCompleted = true;
                        }
                        NetworkServiceLocator.ObjectService.InvokeRPC(
                            ManagerRpcBridge.Instance,
                            "RPC_UpdateProgress",
                            NetworkTarget.All,
                            task.TaskId,
                            escortTask.currentEscorts,
                            task.IsCompleted
                        );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 由 ManagerRpcBridge 在收到 RPC 后调用：更新任务进度
        /// </summary>
        public static void HandleUpdateProgressRPC(string taskId, int count, bool completed)
        {
            if (Instance == null) return;

            if (Instance.tasks.TryGetValue(taskId, out var task))
            {
                switch (task)
                {
                    case KillTask killTask:
                        killTask.CurrentKills = count;
                        killTask.IsCompleted = completed;
                        Instance.OnTaskUpdated?.Invoke(task);
                        Instance.Notice_Data($"{count}/{killTask.RequiredKills}");
                        break;
                    case EscortTask escortTask:
                        escortTask.currentEscorts = count;
                        escortTask.IsCompleted = completed;
                        Instance.OnTaskUpdated?.Invoke(task);
                        Instance.Notice_Data($"{count}/{escortTask.requiredEscorts}");
                        break;
                }

                if (completed)
                    Instance.OnTaskCompleted?.Invoke(task);
            }
        }

        void OnTaskCompletedFun(TaskBase task)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.ClearIndicator());
            switch (task)
            {
                case KillTask killTask:
                    Notice_Data($"击杀任务完成！");
                    StartCoroutine(DelayReward(0.5f));
                    break;
                case EscortTask escortTask:
                    Notice_Data($"护送任务完成！");
                    StartCoroutine(DelayReward(12f));
                    break;
            }
        }

        IEnumerator DelayReward(float delay)
        {
            yield return new WaitForSeconds(delay);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.HideNotice());
            Destroy(taskZone.gameObject);
            if (taskRoutine != null)
            {
                StopCoroutine(taskRoutine);
            }
            taskZone = null;
            // 标记：本次卡牌选择必定为 3 张英雄专属强化
            if (MagicUpgradeManager.instance != null)
                MagicUpgradeManager.instance.isAllExCard = true;
            EventChannelLocator.MainContainer.magicUpgradeChannel.Raise(true);
            var expQuery = new SkillQueryData(SkillQueryType.GetUpgradeExperience);
            EventChannelLocator.MainContainer.skillQueryChannel.Raise(expQuery);
            EventChannelLocator.MainContainer.experienceChannel.Raise(expQuery.intValue);
        }

        void OnDrawGizmos()
        {
            if (!showZoneGizmos)
                return;

            foreach (var task in tasks.Values)
            {
                if (task.IsCompleted)
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                else
                    Gizmos.color = gizmoColor;

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