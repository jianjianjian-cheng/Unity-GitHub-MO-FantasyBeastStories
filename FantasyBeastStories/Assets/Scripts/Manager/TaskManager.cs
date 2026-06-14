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
    private Dictionary<string, KillTask> tasks = new Dictionary<string, KillTask>();

    private HashSet<int> reportedEnemies = new HashSet<int>();

    //倒计时相关变量
    private int countdownTime = 0;
    private int taskDuration;
    private Coroutine taskRoutine;

    // UI更新事件（本地客户端）
    public event System.Action<KillTask> OnTaskUpdated;
    public event System.Action<KillTask> OnTaskCompleted;

    private void OnEnable()
    {
        OnTaskCompleted += OnTaskCompletedFun;
    }

    private void OnDisable()
    {
        OnTaskCompleted -= OnTaskCompletedFun;
    }

    public void SetNotice(string name, string description, int limitTime)
    {
        photonView.RPC("RPC_SetNotice", RpcTarget.All, name, description, limitTime);
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

        StartCoroutine(TaskRoutine());
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
    void RPC_SetNotice(string name, string description, int limitTime)
    {
        taskNotice.gameObject.SetActive(true);
        taskNotice.SetInfo(name, description, "", limitTime);
    }

    //任务倒计时函数

    void Notice_Data(string data)
    {
        taskNotice.Notice_Data(data);
    }

    // 主机激活任务
    public void ActivateTask(
        string taskId,
        Vector3 center,
        float radius,
        int requiredKills,
        int limitTime
    )
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        Debug.LogWarning(
            $"激活任务{taskId}，中心位置：{center}，半径：{radius}，击杀目标：{requiredKills}"
        );
        var task = new KillTask(taskId, center, radius, requiredKills);
        tasks[taskId] = task;
        if (PhotonNetwork.IsMasterClient)
        {
            //开始任务倒计时
            StartCountdownTime(limitTime);
        }
        // 同步到所有客户端
        photonView.RPC("RPC_ActivateTask", RpcTarget.All, taskId, center, radius, requiredKills);
    }

    [PunRPC]
    void RPC_ActivateTask(string taskId, Vector3 center, float radius, int requiredKills)
    {
        if (!tasks.ContainsKey(taskId))
        {
            tasks[taskId] = new KillTask(taskId, center, radius, requiredKills);
        }

        // 在相应位置生成任务区域
        taskZone = Instantiate(
            Resources.Load<GameObject>("TaskPrefab/" + taskId),
            center,
            Quaternion.identity
        );
        Debug.Log($"任务{taskId}已激活，中心位置：{center}" + taskZone.name);
        _indicator.SetTargetAndImage(center, taskId);
    }

    // 任何客户端杀死敌人时调用
    public void ReportKill(Vector3 killPosition, PhotonView enemyView)
    {
        // 只由 Master Client 处理
        photonView.RPC("RPC_ReportKill", RpcTarget.MasterClient, killPosition, enemyView.ViewID);
    }

    [PunRPC]
    void RPC_ReportKill(Vector3 killPosition, int enemyViewID)
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

            if (Vector3.Distance(killPosition, task.ZoneCenter) <= task.ZoneRadius)
            {
                task.CurrentKills++;
                reportedEnemies.Add(enemyViewID);
                Debug.LogWarning(
                    $"任务{task.TaskId}击 增加1," + $"当前击杀次数：{task.CurrentKills}"
                );
                if (task.CurrentKills >= task.RequiredKills)
                {
                    task.IsCompleted = true;
                }
                // 同步进度到所有客户端
                photonView.RPC(
                    "RPC_UpdateProgress",
                    RpcTarget.All,
                    task.TaskId,
                    task.CurrentKills,
                    task.IsCompleted
                );

                break; // 一个击杀只算一个任务，根据需求调整
            }
        }
    }

    [PunRPC]
    void RPC_UpdateProgress(string taskId, int kills, bool completed)
    {
        if (tasks.TryGetValue(taskId, out var task))
        {
            task.CurrentKills = kills;
            task.IsCompleted = completed;
            OnTaskUpdated?.Invoke(task);
            Notice_Data($"{kills}/{task.RequiredKills}");

            if (completed)
                OnTaskCompleted?.Invoke(task);
        }
    }

    void OnTaskCompletedFun(KillTask task)
    {
        // 任务完成后的处理
        Debug.LogWarning(
            $"任务{task.TaskId}已完成，击杀目标：{task.RequiredKills}，击杀次数：{task.CurrentKills}"
        );
        _indicator.SetTargetAndImage(task.ZoneCenter, null);
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

    // private void RPC_TaskCompleted(KillTask task)
    // {
    //     // 任务完成后的处理
    //     Debug.LogWarning(
    //         $"任务{task.TaskId}已完成，击杀目标：{task.RequiredKills}，击杀次数：{task.CurrentKills}"
    //     );
    //     Destroy(taskZone.gameObject);
    //     taskZone = null;
    //     //奖励玩家
    //     MagicUpgradeManager.instance.isAllExCard = true;
    //     GamePlayingManager.instance.AddExperience(
    //         GamePlayingManager.instance.GetUpgradeExperience()
    //     );
    // }

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

public class TaskConst
{
    public const string KillSacrifice = "KillSacrifice";
}
