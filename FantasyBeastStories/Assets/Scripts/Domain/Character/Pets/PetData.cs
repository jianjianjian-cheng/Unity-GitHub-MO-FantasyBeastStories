using System;

namespace Domain.Character.Pets
{
    /// <summary>
    /// 宠物状态枚举（从 PetsBase 移出为命名空间级别）
    /// </summary>
    public enum PetState
    {
        Idle,
        Run,
        Attack,
        Die,
    }

    /// <summary>
    /// 宠物纯数据类
    /// 存储 PetsBase 的可序列化参数与运行时状态，不包含 Unity 对象引用
    ///
    /// 职责：
    /// - 持有攻击距离 / 移动速度 / 状态等数据
    /// - 供 PetsBase 及子类 Charmander 使用
    /// </summary>
    [Serializable]
    public class PetData
    {
        public float attackDistance = 3f;
        public float moveSpeed = 4f;
        public PetState currentState = PetState.Idle;

        public PetData() { }
    }
}