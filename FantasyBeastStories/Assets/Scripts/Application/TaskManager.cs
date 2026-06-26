using System.Collections;
using System.Collections.Generic;
using Domain.Event;
using Domain.Event.Channels.Combat;
using Domain.Event.Channels.Player;
using Domain.Event.Channels.Task;
using Domain.Task;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

namespace Application
{
    public class TaskManager : MonoBehaviourPun
    {
        #region 单例模式
        public static TaskManager instance;

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
        }

        private void OnDisable()
        {
            OnTaskCompleted -= OnTaskCompletedFun;
            EventChannelLocator.MainContainer.taskNoticeChannel.UnregisterListener(OnTaskNoticeReceived);
            EventChannelLocator.MainContainer.enemyReportChannel.UnregisterListener(OnEnemyReported);
        }

        private void OnEnemyReported(EnemyReportData data)
        {
            if (data == null) return;
            photonView.RPC("RPC_ReportCount", RpcTarget.MasterClient, data.position, data.photonViewID);
        }

        private void OnTaskNoticeReceived(TaskNoticeData data)
        {
            if (data == null) return;
            photonView.RPC("RPC_SetNotice", RpcTarget.All, data.name, data.description, data.limitTime, data.requiredCount);
        }

        public void SetNotice(string name, string description, int limitTime, int requeredCount = 1)
        {
            photonView.RPC("RPC_SetNotice", RpcTarget.All, name, description, limitTime, requeredCount);
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
                photonView.RPC("RPC_UpdateAllPlayerTimeUI", RpcTarget.All, time);
            }
            Debug.LogWarning("任务失败");
            photonView.RPC("RPC_TaskFailed", RpcTarget.All);
            yield break;
        }

        [PunRPC]
        private void RPC_UpdateAllPlayerTimeUI(string time)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.UpdateTime(time));
        }

        [PunRPC]
        private void RPC_TaskFailed()
        {
            if ((taskZone == null))
            {
                return;
            }
            Destroy(taskZone.gameObject);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.ClearIndicator());
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.HideNotice());
        }

        [PunRPC]
        void RPC_SetNotice(string name, string description, int limitTime, int requeredCount)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(
                TaskUIUpdateData.ShowNotice(name, description, limitTime, requeredCount));
        }

        void Notice_Data(string data)
        {
            EventChannelLocator.MainContainer.taskUIChannel.Raise(TaskUIUpdateData.NoticeData(data));
        }

        public void ActivateTask(TaskBase taskBase)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;
            switch (taskBase)
            {
                case KillTask killTask:
                    photonView.RPC(
                        "RPC_ActivateKillTask",
                        RpcTarget.All,
                        killTask.TaskId,
                        killTask.limitTime,
                        killTask.ZoneCenter,
                        killTask.RequiredKills
                    );
                    break;
                case EscortTask escortTask:
                    photonView.RPC(
                        "RPC_ActivateEscortTask",
                        RpcTarget.All,
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

        [PunRPC]
        void RPC_ActivateKillTask(string taskId, int limitTime, Vector3 ZoneCenter, int requiredKills)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCountdownTime(limitTime);
            }
            taskZone = Instantiate(
                Resources.Load<GameObject>("TaskPrefab/" + taskId),
                ZoneCenter,
                Quaternion.identity
            );
            tasks.Clear();

            KillTask killTask = new KillTask(taskId, ZoneCenter, 7, requiredKills, limitTime);
            tasks[taskId] = killTask;

            Debug.Log($"任务{taskId}已激活，中心位置：{ZoneCenter}" + taskZone.name);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(
                TaskUIUpdateData.SetIndicator(ZoneCenter, taskId));
        }

        [PunRPC]
        void RPC_ActivateEscortTask(
            string taskId,
            int limitTime,
            Vector3 ZoneCenter,
            int requiredEscorts
        )
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCountdownTime(limitTime);
            }
            taskZone = Instantiate(
                Resources.Load<GameObject>("TaskPrefab/" + taskId),
                ZoneCenter,
                Quaternion.identity
            );
            tasks.Clear();
            EscortTask escortTask = new EscortTask(taskId, ZoneCenter, 4, requiredEscorts, limitTime);
            tasks[escortTask.TaskId] = escortTask;
            Debug.Log($"任务{taskId}已激活，中心位置：{ZoneCenter}" + taskZone.name);
            EventChannelLocator.MainContainer.taskUIChannel.Raise(
                TaskUIUpdateData.SetIndicator(ZoneCenter, taskId));
        }

        public void ReportCount(Vector3 killPosition, PhotonView enemyView)
        {
            photonView.RPC("RPC_ReportCount", RpcTarget.MasterClient, killPosition, enemyView.ViewID);
        }

        [PunRPC]
        void RPC_ReportCount(Vector3 killPosition, int enemyViewID)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (reportedEnemies.Contains(enemyViewID))
            {
                return;
            }

            foreach (var task in tasks.Values)
            {
                if (task.IsCompleted)
                    continue;

                if (task is KillTask killTask)
                {
                    KillTask kt = task as KillTask;
                    if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
                    {
                        kt.CurrentKills++;
                        reportedEnemies.Add(enemyViewID);
                        Debug.LogWarning(
                            $"任务{task.TaskId}击 增加1," + $"当前击杀次数：{kt.CurrentKills}"
                        );
                        if (kt.CurrentKills >= kt.RequiredKills)
                        {
                            task.IsCompleted = true;
                        }
                        photonView.RPC(
                            "RPC_UpdateProgress",
                            RpcTarget.All,
                            task.TaskId,
                            kt.CurrentKills,
                            task.IsCompleted
                        );

                        break;
                    }
                }

                if (task is EscortTask escortTask)
                {
                    if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
                    {
                        escortTask.currentEscorts++;
                        reportedEnemies.Add(enemyViewID);
                        Debug.LogWarning(
                            $"任务{task.TaskId}击 增加1,"
                                + $"当前运输机器人：{escortTask.currentEscorts}"
                        );
                        if (escortTask.currentEscorts >= escortTask.requiredEscorts)
                        {
                            escortTask.IsCompleted = true;
                        }
                        photonView.RPC(
                            "RPC_UpdateProgress",
                            RpcTarget.All,
                            task.TaskId,
                            escortTask.currentEscorts,
                            task.IsCompleted
                        );

                        break;
                    }
                }
            }
        }

        [PunRPC]
        void RPC_UpdateProgress(string taskId, int count, bool completed)
        {
            if (tasks.TryGetValue(taskId, out var task))
            {
                switch (task)
                {
                    case KillTask killTask:
                        killTask.CurrentKills = count;
                        killTask.IsCompleted = completed;
                        OnTaskUpdated?.Invoke(task);
                        Notice_Data($"{count}/{killTask.RequiredKills}");
                        break;
                    case EscortTask escortTask:
                        escortTask.currentEscorts = count;
                        escortTask.IsCompleted = completed;
                        OnTaskUpdated?.Invoke(task);
                        Notice_Data($"{count}/{escortTask.requiredEscorts}");
                        break;
                }

                if (completed)
                    OnTaskCompleted?.Invoke(task);
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
