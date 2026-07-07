using System.Collections;
using System.Collections.Generic;
using Application;
using Presentation.UI.Framework.Base;
using Presentation.UI.Framework.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务面板 — 展示所有任务及其进度。
///
/// 功能：
/// - 打开时从 QuestTaskInventory 读取进度
/// - 动态生成任务项（ScrollView 列表）
/// - 播放平滑过渡动画（进度条 + 数字）
/// - 已完成的直接显示完成图标和满进度，跳过动画
/// </summary>
public class MassionPanel : UIScreen
{
    [Header("任务面板")]
    [SerializeField] private GameObject taskItemPrefab;      // 任务项预制体
    [SerializeField] private Transform contentParent;        // ScrollView Content
    [SerializeField] private ScrollRect scrollRect;          // ScrollRect（用于刷新布局）

    [Header("动画设置")]
    [SerializeField] private float staggerDelay = 0.15f;     // 每个任务的延迟间隔
    [SerializeField] private float animateDuration = 0.8f;   // 单个任务动画时长

    // 运行时生成的任务项列表
    private List<MassionItem> taskItems = new List<MassionItem>();

    // ──────────────────────────────────────────────
    //  UIScreen 生命周期
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
        screenId = "MassionPanel";
        base.Awake();
        UIManager.Instance.RegisterScreen(this);
    }

    /// <summary>打开面板前构建任务列表</summary>
    protected override void OnBeforeOpen()
    {
        base.OnBeforeOpen();
        BuildTaskList();
    }

    /// <summary>关闭面板后清空任务列表</summary>
    protected override void OnAfterClose()
    {
        base.OnAfterClose();
        ClearTaskList();
    }

    // ──────────────────────────────────────────────
    //  动态生成任务列表
    // ──────────────────────────────────────────────

    private void BuildTaskList()
    {
        ClearTaskList();

        // 1. 结算：合并本局进度 → 存档 → 返回 oldProgress + delta
        var settleResult = QuestTaskManager.Instance.SettlePendingProgress();
        Debug.Log($"[MassionPanel] 任务结算完成：{settleResult.delta.Count} 个任务有新增进度");

        // 2. 从 Resources 加载任务数据库
        var database = Resources.Load<QuestTaskDatabaseSO>("QuestTask/QuestTaskDatabase");
        if (database == null || database.allTasks == null || database.allTasks.Count == 0)
        {
            Debug.LogWarning("[MassionPanel] 未找到 QuestTaskDatabase 或数据为空");
            return;
        }

        // 3. 构建任务列表，用 settleResult 驱动显示
        foreach (var taskData in database.allTasks)
        {
            if (taskData == null) continue;

            // 实例化预制体
            var go = Instantiate(taskItemPrefab, contentParent);
            go.name = $"TaskItem_{taskData.taskId}_{taskData.taskDescription}";

            var item = go.GetComponent<MassionItem>();
            if (item == null)
            {
                Debug.LogWarning($"[MassionPanel] 预制体缺少 MassionItem 组件");
                Destroy(go);
                continue;
            }

            // 设置任务数据（初始显示 0 进度）
            item.Setup(taskData);

            // 读取结算结果
            int oldVal = settleResult.oldProgress.ContainsKey(taskData.taskId)
                ? settleResult.oldProgress[taskData.taskId] : 0;
            int deltaVal = settleResult.delta.ContainsKey(taskData.taskId)
                ? settleResult.delta[taskData.taskId] : 0;
            int newVal = oldVal + deltaVal;

            Debug.Log($"[MassionPanel] 任务[{taskData.taskId}] {taskData.taskDescription}: 旧进度={oldVal}, 新增={deltaVal}, 当前={newVal}, 目标={taskData.targetCount}");

            // 如果已完成，直接设置最终状态（跳过动画）
            if (newVal >= taskData.targetCount)
            {
                item.SetFinalProgress(newVal);
            }
            else if (oldVal > 0)
            {
                // 有旧进度：先设到旧值，后面的动画会从 old → new
                item.SetInitialProgress(oldVal);
            }
            // 无任何进度：保持 Setup 后的 0 状态

            taskItems.Add(item);
        }

        // 4. 刷新 ScrollRect 布局
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // 5. 启动平滑结算动画（未完成的任务从 old → old+delta）
        if (gameObject.activeInHierarchy)
            StartCoroutine(PlaySettleAnimation(settleResult));
    }

    // ──────────────────────────────────────────────
    //  平滑结算动画
    // ──────────────────────────────────────────────

    /// <summary>
    /// 依次播放每个任务的进度条平滑过渡动画。
    /// 已完成的任务跳过，未完成的从 oldProgress 播放到 oldProgress + delta。
    /// </summary>
    private IEnumerator PlaySettleAnimation(QuestSettleResult settleResult)
    {
        // 等待一帧确保布局已刷新
        yield return null;

        for (int i = 0; i < taskItems.Count; i++)
        {
            var item = taskItems[i];
            if (item == null || item.TaskData == null) continue;

            int taskId = item.TaskId;
            int oldVal = settleResult.oldProgress.ContainsKey(taskId)
                ? settleResult.oldProgress[taskId] : 0;
            int deltaVal = settleResult.delta.ContainsKey(taskId)
                ? settleResult.delta[taskId] : 0;
            int newVal = oldVal + deltaVal;

            // 已完成或无新增进度的跳过动画
            if (newVal >= item.TargetCount || deltaVal <= 0)
                continue;

            // 播放平滑过渡动画（item 内部已通过 SetInitialProgress 设为 oldVal，
            // 此处从 currentCount(=oldVal) 动画到 newVal）
            item.AnimateToTarget(newVal, animateDuration);

            // 每个任务之间有 stagger 延迟
            yield return new WaitForSeconds(staggerDelay);
        }
    }

    // ──────────────────────────────────────────────
    //  清理
    // ──────────────────────────────────────────────

    private void ClearTaskList()
    {
        foreach (var item in taskItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        taskItems.Clear();
    }
}