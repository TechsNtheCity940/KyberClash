using System;
using System.Collections.Generic;
using UnityEngine;

namespace KyberKlash.Core
{
    /// <summary>
    /// Generic finite state machine implementation.
    /// Manages state transitions and updates for any component.
    /// </summary>
    public class StateMachine : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool debugLogTransitions = false;
        [SerializeField] private State currentState;
        [SerializeField] private State previousState;

        private readonly Dictionary<Type, State> stateInstances = new Dictionary<Type, State>();
        private State pendingState;
        private bool isTransitioning;

        /// <summary>
        /// Current active state
        /// </summary>
        public State CurrentState => currentState;

        /// <summary>
        /// Previous state before current
        /// </summary>
        public State PreviousState => previousState;

        /// <summary>
        /// Event raised when state changes
        /// </summary>
        public event Action<State, State> OnStateChanged;

        protected virtual void Awake()
        {
            InitializeStates();
        }

        protected virtual void Update()
        {
            if (currentState != null)
            {
                currentState.UpdateLogic(Time.deltaTime);
            }

            // Handle pending transition after logic update
            if (pendingState != null && !isTransitioning)
            {
                PerformTransition();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (currentState != null)
            {
                currentState.PhysicsUpdate(Time.fixedDeltaTime);
            }

            // Check transitions in fixed update for physics-dependent conditions
            if (currentState != null)
            {
                currentState.CheckTransitions();
            }
        }

        /// <summary>
        /// Initialize all state instances. Override in derived classes to register states.
        /// </summary>
        protected virtual void InitializeStates()
        {
            // Derived classes should populate stateInstances
        }

        /// <summary>
        /// Register a state with the state machine
        /// </summary>
        public void RegisterState<T>() where T : State, new()
        {
            var type = typeof(T);
            if (!stateInstances.ContainsKey(type))
            {
                var instance = new T();
                stateInstances[type] = instance;
            }
        }

        /// <summary>
        /// Register a state instance
        /// </summary>
        protected void RegisterState(State state)
        {
            var type = state.GetType();
            if (!stateInstances.ContainsKey(type))
            {
                stateInstances[type] = state;
            }
        }

        /// <summary>
        /// Start the state machine with an initial state
        /// </summary>
        public void StartStateMachine<T>() where T : State
        {
            RequestTransition<T>();
            // Force immediate transition on start
            PerformTransition();
        }

        /// <summary>
        /// Start with a specific state type
        /// </summary>
        public void StartStateMachine(Type stateType)
        {
            RequestTransition(stateType);
            PerformTransition();
        }

        /// <summary>
        /// Request a transition to a new state
        /// </summary>
        public void RequestTransition<T>() where T : State
        {
            RequestTransition(typeof(T));
        }

        /// <summary>
        /// Request a transition to a new state by type
        /// </summary>
        public void RequestTransition(Type stateType)
        {
            if (stateInstances.TryGetValue(stateType, out var newState))
            {
                if (currentState != newState)
                {
                    pendingState = newState;
                }
            }
            else
            {
                Debug.LogWarning($"[StateMachine] State {stateType.Name} not registered!");
            }
        }

        /// <summary>
        /// Perform the actual state transition
        /// </summary>
        private void PerformTransition()
        {
            if (pendingState == null || isTransitioning) return;

            isTransitioning = true;
            var oldState = currentState;

            // Exit current state
            currentState?.Exit();

            // Enter new state
            previousState = currentState;
            currentState = pendingState;
            currentState.Enter(this);

            // Raise event
            OnStateChanged?.Invoke(previousState, currentState);

            if (debugLogTransitions)
            {
                Debug.Log($"[StateMachine] {gameObject.name}: {previousState?.GetType().Name ?? "NULL"} -> {currentState.GetType().Name}");
            }

            pendingState = null;
            isTransitioning = false;
        }

        /// <summary>
        /// Get a state instance by type
        /// </summary>
        public T GetState<T>() where T : State
        {
            if (stateInstances.TryGetValue(typeof(T), out var state))
            {
                return state as T;
            }
            return null;
        }

        /// <summary>
        /// Check if currently in a specific state
        /// </summary>
        public bool IsInState<T>() where T : State
        {
            return currentState is T;
        }

        /// <summary>
        /// Check if current state matches type
        /// </summary>
        public bool IsInState(Type stateType)
        {
            return currentState != null && currentState.GetType() == stateType;
        }

        /// <summary>
        /// Force immediate transition (bypasses pending queue)
        /// </summary>
        public void ForceTransition<T>() where T : State
        {
            RequestTransition<T>();
            PerformTransition();
        }

        protected virtual void OnDestroy()
        {
            currentState?.Exit();
            stateInstances.Clear();
        }
    }
}