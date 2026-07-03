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

        // 从 Resources 加载任务数据库
        var database = Resources.Load<QuestTaskDatabaseSO>("QuestTask/QuestTaskDatabase");
        if (database == null || database.allTasks == null || database.allTasks.Count == 0)
        {
            Debug.LogWarning("[MassionPanel] 未找到 QuestTaskDatabase 或数据为空");
            return;
        }

        // 读取当前进度（从 QuestTaskManager，由 SaveManager 统一管理）
        var progressMap = QuestTaskManager.Instance.GetAllProgress();

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

            // 读取该任务的累计进度
            int savedProgress = progressMap.ContainsKey(taskData.taskId) ? progressMap[taskData.taskId] : 0;

            // 如果已完成，直接设置最终状态（跳过动画）
            if (savedProgress >= taskData.targetCount)
            {
                item.SetFinalProgress(savedProgress);
            }

            taskItems.Add(item);
        }

        // 刷新 ScrollRect 布局
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        // 启动平滑结算动画（未完成的任务从 0 → savedProgress）
        if (gameObject.activeInHierarchy)
            StartCoroutine(PlaySettleAnimation(progressMap));
    }

    // ──────────────────────────────────────────────
    //  平滑结算动画
    // ──────────────────────────────────────────────

    /// <summary>
    /// 依次播放每个任务的进度条平滑过渡动画。
    /// 已完成的任务跳过，未完成的从 0 播放到 savedProgress。
    /// </summary>
    private IEnumerator PlaySettleAnimation(Dictionary<int, int> progressMap)
    {
        // 等待一帧确保布局已刷新
        yield return null;

        for (int i = 0; i < taskItems.Count; i++)
        {
            var item = taskItems[i];
            if (item == null || item.TaskData == null) continue;

            int savedProgress = progressMap.ContainsKey(item.TaskId) ? progressMap[item.TaskId] : 0;

            // 已完成或无进度的跳过动画
            if (savedProgress <= 0 || savedProgress >= item.TargetCount)
                continue;

            // 播放平滑过渡动画
            item.AnimateToTarget(savedProgress, animateDuration);

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