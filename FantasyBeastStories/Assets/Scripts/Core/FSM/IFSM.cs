using System;

namespace Core.FSM
{
    /// <summary>
    /// 状态机接口
    /// </summary>
    public interface IFSM<TState> where TState : struct, Enum
    {
        TState CurrentState { get; }
        void ChangeState(TState newState);
        void Update();
        void FixedUpdate();
    }

    /// <summary>
    /// 状态接口
    /// </summary>
    public interface IFSMState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }

    /// <summary>
    /// 泛型状态接口
    /// </summary>
    public interface IFSMState<T> : IFSMState
    {
        void SetContext(T context);
    }
}
