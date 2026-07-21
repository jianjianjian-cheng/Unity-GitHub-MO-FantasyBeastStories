using UnityEngine;

namespace Core
{
    /// <summary>
    /// 场景配置数据（ScriptableObject）
    /// 统一管理场景索引，避免代码中硬编码 magic number
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Scene Config")]
    public class SceneConfigSO : ScriptableObject
    {
        [Header("场景索引")]
        [Tooltip("主菜单场景索引")]
        public int mainMenuSceneIndex = 0;

        [Tooltip("大厅场景索引")]
        public int lobbySceneIndex = 1;

        [Tooltip("战斗场景索引")]
        public int battleSceneIndex = 2;
    }
}
