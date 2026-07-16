using UnityEngine;

namespace KyberKlash.Core
{
    /// <summary>
    /// Base class for all states in the finite state machine.
    /// Each state encapsulates specific behavior and transition logic.
    /// </summary>
    public abstract class State
    {
        protected StateMachine stateMachine;
        protected bool isActive;

        /// <summary>
        /// Called when the state machine enters this state
        /// </summary>
        public virtual void Enter(StateMachine machine)
        {
            stateMachine = machine;
            isActive = true;
            OnEnter();
        }

        /// <summary>
        /// Called when the state machine exits this state
        /// </summary>
        public virtual void Exit()
        {
            isActive = false;
            OnExit();
            stateMachine = null;
        }

        /// <summary>
        /// Called every frame for logic updates
        /// </summary>
        public virtual void UpdateLogic(float deltaTime)
        {
            if (!isActive) return;
            OnUpdateLogic(deltaTime);
        }

        /// <summary>
        /// Called every fixed frame for physics updates
        /// </summary>
        public virtual void PhysicsUpdate(float fixedDeltaTime)
        {
            if (!isActive) return;
            OnPhysicsUpdate(fixedDeltaTime);
        }

        /// <summary>
        /// Called to check if transitions should occur
        /// </summary>
        public virtual void CheckTransitions()
        {
            if (!isActive) return;
            OnCheckTransitions();
        }

        /// <summary>
        /// Override in derived classes for enter logic
        /// </summary>
        protected virtual void OnEnter() { }

        /// <summary>
        /// Override in derived classes for exit logic
        /// </summary>
        protected virtual void OnExit() { }

        /// <summary>
        /// Override in derived classes for logic update
        /// </summary>
        protected virtual void OnUpdateLogic(float deltaTime) { }

        /// <summary>
        /// Override in derived classes for physics update
        /// </summary>
        protected virtual void OnPhysicsUpdate(float fixedDeltaTime) { }

        /// <summary>
        /// Override in derived classes for transition checks
        /// </summary>
        protected virtual void OnCheckTransitions() { }

        /// <summary>
        /// Request a transition to another state
        /// </summary>
        protected void RequestTransition<T>() where T : State
        {
            stateMachine?.RequestTransition<T>();
        }

        /// <summary>
        /// Request a transition to a state by type
        /// </summary>
        protected void RequestTransition(System.Type stateType)
        {
            stateMachine?.RequestTransition(stateType);
        }
    }

    /// <summary>
    /// Generic state base for states that need access to a specific owner type
    /// </summary>
    public abstract class State<TOwner> : State where TOwner : Component
    {
        protected TOwner owner;

        public override void Enter(StateMachine machine)
        {
            base.Enter(machine);
            owner = machine.GetComponent<TOwner>();
            OnEnterOwner(owner);
        }

        protected virtual void OnEnterOwner(TOwner owner) { }

        public override void Exit()
        {
            OnExitOwner(owner);
            owner = null;
            base.Exit();
        }

        protected virtual void OnExitOwner(TOwner owner) { }
    }
}