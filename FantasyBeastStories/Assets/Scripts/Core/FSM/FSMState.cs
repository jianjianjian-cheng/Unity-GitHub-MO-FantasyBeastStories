using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.FSM
{
    /// <summary>
    /// 状态基类
    /// </summary>
    public abstract class FSMState<TState> : IFSMState where TState : struct, Enum
    {
        protected TState StateKey { get; private set; }

        public void SetStateKey(TState key)
        {
            StateKey = key;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }

    /// <summary>
    /// 泛型状态基类
    /// </summary>
    public abstract class FSMState<TState, TContext> : IFSMState<TContext> where TState : struct, Enum
    {
        protected TContext Context { get; private set; }
        protected TState StateKey { get; private set; }

        public void SetContext(TContext context)
        {
            Context = context;
        }

        public void SetStateKey(TState key)
        {
            StateKey = key;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }

    /// <summary>
    /// 状态机管理器
    /// </summary>
    public class FSMManager<TState> : IFSM<TState> where TState : struct, Enum
    {
        private readonly Dictionary<TState, IFSMState> _states = new Dictionary<TState, IFSMState>();
        private IFSMState _currentState;
        private TState _currentStateKey;

        public TState CurrentState => _currentStateKey;

        public void AddState(TState key, IFSMState state)
        {
            _states[key] = state;
            
            if (state is FSMState<TState> fsmState)
            {
                fsmState.SetStateKey(key);
            }
        }

        public void SetInitialState(TState key)
        {
            if (_states.TryGetValue(key, out var state))
            {
                _currentStateKey = key;
                _currentState = state;
                _currentState.Enter();
            }
        }

        public void ChangeState(TState newState)
        {
            if (_currentStateKey.Equals(newState)) return;

            if (!_states.TryGetValue(newState, out var state))
            {
                Debug.LogWarning($"FSM: State {newState} not found!");
                return;
            }

            _currentState?.Exit();
            _currentStateKey = newState;
            _currentState = state;
            _currentState.Enter();
        }

        public void Update()
        {
            _currentState?.Update();
        }

        public void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        public IFSMState GetState(TState key)
        {
            return _states.TryGetValue(key, out var state) ? state : null;
        }

        public bool HasState(TState key) => _states.ContainsKey(key);
    }

    /// <summary>
    /// MonoBehaviour状态机 - 适用于需要挂载在GameObject上的状态机
    /// </summary>
    public abstract class MonoFSM<TState> : MonoBehaviour where TState : struct, Enum
    {
        protected FSMManager<TState> _fsm = new FSMManager<TState>();
        
        public TState CurrentState => _fsm.CurrentState;

        protected abstract void RegisterStates();
        protected abstract TState GetInitialState();

        protected virtual void Awake()
        {
            RegisterStates();
            _fsm.SetInitialState(GetInitialState());
        }

        private void Update()
        {
            _fsm.Update();
        }

        private void FixedUpdate()
        {
            _fsm.FixedUpdate();
        }

        public void ChangeState(TState newState)
        {
            _fsm.ChangeState(newState);
        }

        protected void AddState(TState key, IFSMState state)
        {
            _fsm.AddState(key, state);
        }

        // 提供对底层FSM的访问
        protected IFSM<TState> FSM => _fsm;
    }
}
