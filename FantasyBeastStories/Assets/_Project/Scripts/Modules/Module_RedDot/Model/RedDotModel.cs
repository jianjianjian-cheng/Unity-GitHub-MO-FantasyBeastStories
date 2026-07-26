using System.Collections.Generic;

namespace UI.RedDot
{
    /// <summary>
    /// 红点数据模型：管理节点激活状态与父子聚合。
    /// 叶子节点由 Controller 设置，父节点自动聚合（任一子节点 active → 父 active）。
    /// </summary>
    public class RedDotModel
    {
        /// <summary>节点激活状态</summary>
        private readonly Dictionary<string, bool> _nodeStates = new();

        /// <summary>子节点 → 父节点映射（用于向上聚合）</summary>
        private readonly Dictionary<string, string> _parentOf = new();

        /// <summary>父节点 → 子节点列表映射（用于聚合计算）</summary>
        private readonly Dictionary<string, List<string>> _childrenOf = new();

        /// <summary>
        /// 注册父子关系。子节点的状态变化会向上传播到父节点。
        /// </summary>
        public void RegisterChild(string parent, string child)
        {
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(child))
                return;

            _parentOf[child] = parent;

            if (!_childrenOf.TryGetValue(parent, out var children))
            {
                children = new List<string>();
                _childrenOf[parent] = children;
            }

            if (!children.Contains(child))
                children.Add(child);
        }

        /// <summary>查询节点是否激活</summary>
        public bool IsActive(string key)
        {
            return _nodeStates.TryGetValue(key, out var active) && active;
        }

        /// <summary>
        /// 设置叶子节点状态，并自动向上聚合传播。
        /// 返回所有发生变化的节点（含自身和受影响的祖先），供 Controller 广播。
        /// </summary>
        public List<string> SetActive(string key, bool active)
        {
            var changed = new List<string>();

            if (!_nodeStates.TryGetValue(key, out var oldVal) || oldVal != active)
            {
                _nodeStates[key] = active;
                changed.Add(key);
            }

            // 向上聚合
            PropagateUp(key, changed);
            return changed;
        }

        private void PropagateUp(string childKey, List<string> changed)
        {
            string current = childKey;

            while (_parentOf.TryGetValue(current, out var parent))
            {
                bool parentActive = ComputeParentActive(parent);

                if (_nodeStates.TryGetValue(parent, out var oldVal) && oldVal == parentActive)
                    break; // 父节点状态未变，祖先也不会变，提前终止

                _nodeStates[parent] = parentActive;
                changed.Add(parent);
                current = parent;
            }
        }

        private bool ComputeParentActive(string parent)
        {
            if (!_childrenOf.TryGetValue(parent, out var children))
                return false;

            for (int i = 0; i < children.Count; i++)
            {
                if (_nodeStates.TryGetValue(children[i], out var active) && active)
                    return true;
            }

            return false;
        }
    }
}
