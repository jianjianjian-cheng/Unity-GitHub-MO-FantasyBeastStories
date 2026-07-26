using UnityEngine;

namespace Core
{
    /// <summary>
    /// 伤害数字显示事件参数
    /// 用途：允许 Domain 层请求显示伤害数字，而不直接依赖 Presentation 层的 DamageNum 组件
    /// Presentation 层监听此事件并负责实际的 UI 显示
    /// </summary>
    public class DamageDisplayEventArgs : EventArgsBase
    {
        public float damageValue;
        public Vector3 worldPosition;
        public bool isCritical;

        // ===== GC 优化：共享实例，避免每次 new 分配 =====
        private static DamageDisplayEventArgs _shared;

        public static DamageDisplayEventArgs GetShared(float damageValue, Vector3 worldPosition, bool isCritical)
        {
            if (_shared == null)
            {
                _shared = new DamageDisplayEventArgs(damageValue, worldPosition, isCritical);
            }
            else
            {
                _shared.damageValue = damageValue;
                _shared.worldPosition = worldPosition;
                _shared.isCritical = isCritical;
            }
            return _shared;
        }

        public DamageDisplayEventArgs(float damageValue, Vector3 worldPosition, bool isCritical)
        {
            this.damageValue = damageValue;
            this.worldPosition = worldPosition;
            this.isCritical = isCritical;
        }
    }
}