using System.Collections;
using System.Collections.Generic;
using Manager;
using Photon.Pun;
using UI;
using Unity.VisualScripting;
using UnityEngine;

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
    private bool showZoneGizmos = true; // 是否显示范围

    private GameObject taskZone;

    [SerializeField]
    private Color gizmoColor = new Color(0, 1, 0, 0.3f); // Gizmo颜色

    [SerializeField]
    private TaskNotice taskNotice;

    [SerializeField]
    private DirectionIndicator _indicator;

    // 任务字典
    private Dictionary<string, TaskBase> tasks = new Dictionary<string, TaskBase>();

    private HashSet<int> reportedEnemies = new HashSet<int>();

    //倒计时相关变量
    private int countdownTime = 0;
    private int taskDuration;
    private Coroutine taskRoutine;

    // UI更新事件（本地客户端）
    public event System.Action<TaskBase> OnTaskUpdated;
    public event System.Action<TaskBase> OnTaskCompleted;

    private void OnEnable()
    {
        OnTaskCompleted += OnTaskCompletedFun;
    }

    private void OnDisable()
    {
        OnTaskCompleted -= OnTaskCompletedFun;
    }

    public void SetNotice(string name, string description, int limitTime , int requeredCount = 1)
    {
        photonView.RPC("RPC_SetNotice", RpcTarget.All, name, description, limitTime, requeredCount);
    }

    /// <summary>
    /// 开始任务倒计时,房主启用这个方法
    /// </summary>
    /// <returns></returns>
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
            //这里更新UI显示
            int min = Mathf.FloorToInt(countdownTime / 60);
            int sec = Mathf.FloorToInt(countdownTime % 60);
            time = "剩余时间：" + $"{min:D2}:{sec:D2}";
            photonView.RPC("RPC_UpdateAllPlayerTimeUI", RpcTarget.All, time);
        }
        //时间结束，任务失败
        Debug.LogWarning("任务失败");
        photonView.RPC("RPC_TaskFailed", RpcTarget.All);
        //执行UI相关逻辑
        yield break;
    }

    /// <summary>
    /// 更新剩余时间UI
    /// </summary>
    /// <param name="time">剩余时间</param>
    [PunRPC]
    private void RPC_UpdateAllPlayerTimeUI(string time)
    {
        taskNotice.UpDateTime(time);
    }

    /// <summary>
    /// 任务失败同步
    /// </summary>
    [PunRPC]
    private void RPC_TaskFailed()
    {
        if ((taskZone == null))
        {
            return;
        }
        Destroy(taskZone.gameObject);
        _indicator.SetTargetName(null);
        StartCoroutine(HideTaskNotice());
    }

    public IEnumerator HideTaskNotice()
    {
        taskNotice.PlaySlideAnimation(false);
        yield return new WaitForSeconds(1f);
        taskNotice.gameObject.SetActive(false);
    }

    [PunRPC]
    void RPC_SetNotice(string name, string description, int limitTime, int requeredCount)
    {
        taskNotice.gameObject.SetActive(true);
        taskNotice.SetInfo(name, description, limitTime, requeredCount);
    }

    //任务倒计时函数

    void Notice_Data(string data)
    {
        taskNotice.Notice_Data(data);
    }

    // 主机激活任务
    public void ActivateTask(TaskBase taskBase)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        switch (taskBase)
        {
            case KillTask killTask:
                // 同步到所有客户端
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
                // 同步到所有客户端
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

    /// <summary>
    /// 击杀类型任务
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="limitTime"></param>
    /// <param name="ZoneCenter"></param>
    /// <param name="requiredKills"></param>
    [PunRPC]
    void RPC_ActivateKillTask(string taskId, int limitTime, Vector3 ZoneCenter, int requiredKills)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            //开始任务倒计时
            StartCountdownTime(limitTime);
        }
        // 在相应位置生成任务区域
        taskZone = Instantiate(
            Resources.Load<GameObject>("TaskPrefab/" + taskId),
            ZoneCenter,
            Quaternion.identity
        );
        //使用字典是因为后续可能实现同时存在多个任务，暂时使用字典
        tasks.Clear();

        KillTask killTask = new KillTask(taskId, ZoneCenter, 7, requiredKills, limitTime);
        tasks[taskId] = killTask;

        Debug.Log($"任务{taskId}已激活，中心位置：{ZoneCenter}" + taskZone.name);
        _indicator.SetTargetAndImage(ZoneCenter, taskId);
    }

    /// <summary>
    /// 护送类型任务
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="limitTime"></param>
    /// <param name="ZoneCenter"></param>
    /// <param name="requiredEscorts"></param>
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
            //开始任务倒计时
            StartCountdownTime(limitTime);
        }
        // 在相应位置生成任务区域
        taskZone = Instantiate(
            Resources.Load<GameObject>("TaskPrefab/" + taskId),
            ZoneCenter,
            Quaternion.identity
        );
        //使用字典是因为后续可能实现同时存在多个任务，暂时使用字典
        tasks.Clear();
        EscortTask escortTask = new EscortTask(taskId, ZoneCenter, 4, requiredEscorts, limitTime);
        tasks[escortTask.TaskId] = escortTask;
        Debug.Log($"任务{taskId}已激活，中心位置：{ZoneCenter}" + taskZone.name);
        _indicator.SetTargetAndImage(ZoneCenter, taskId);
    }

    // 任何客户端完成击杀/运送时调用
    public void ReportCount(Vector3 killPosition, PhotonView enemyView)
    {
        // 只由 Master Client 处理
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
                //强转为KillTask
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
                    // 同步进度到所有客户端
                    photonView.RPC(
                        "RPC_UpdateProgress",
                        RpcTarget.All,
                        task.TaskId,
                        kt.CurrentKills,
                        task.IsCompleted
                    );

                    break; // 一个击杀只算一个任务，根据需求调整
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
                    // 同步进度到所有客户端
                    photonView.RPC(
                        "RPC_UpdateProgress",
                        RpcTarget.All,
                        task.TaskId,
                        escortTask.currentEscorts,
                        task.IsCompleted
                    );

                    break; // 一个运送只算一个任务，根据需求调整
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
        // 任务完成后的处理
        _indicator.SetTargetAndImage(task.ZoneCenter, null);
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
        // 奖励逻辑        
        StartCoroutine(HideTaskNotice());
        Destroy(taskZone.gameObject);
        if (taskRoutine != null)
        {
            StopCoroutine(taskRoutine);
        }
        taskZone = null;
        //奖励玩家
        MagicUpgradeManager.instance.isAllExCard = true;
        GamePlayingManager.instance.AddExperience(
            GamePlayingManager.instance.GetUpgradeExperience()
        );
    }

    // 在Scene视图中绘制任务区域
    void OnDrawGizmos()
    {
        if (!showZoneGizmos)
            return;

        // 绘制运行时的任务区域
        foreach (var task in tasks.Values)
        {
            if (task.IsCompleted)
                Gizmos.color = new Color(1, 1, 0, 0.3f); // 完成-黄色
            else
                Gizmos.color = gizmoColor; // 进行中-绿色

            Gizmos.DrawWireSphere(task.ZoneCenter, task.ZoneRadius);
            Gizmos.DrawSphere(task.ZoneCenter, 0.3f); // 中心点
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
