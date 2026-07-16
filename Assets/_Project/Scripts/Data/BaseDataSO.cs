using UnityEngine;

namespace KyberKlash.Data
{
    /// <summary>
    /// Base class for all data-driven ScriptableObjects in Kyber Clash.
    /// Provides common functionality and serialization patterns.
    /// </summary>
    public abstract class BaseDataSO : ScriptableObject
    {
        [SerializeField, TextArea(3, 5)] protected string description;

        public string Description => description;
        public virtual string DisplayName => name;
    }

    /// <summary>
    /// Event channel for decoupled communication between systems.
    /// Following SOAP pattern - allows multiple listeners without direct references.
    /// </summary>
    public abstract class GameEvent<T> : ScriptableObject
    {
        public event System.Action<T> OnRaised;

        public void Raise(T value)
        {
            OnRaised?.Invoke(value);
        }
    }

    /// <summary>
    /// Generic variable SO for data sharing without direct references.
    /// </summary>
    public abstract class GameVariable<T> : ScriptableObject
    {
        [SerializeField] protected T initialValue;
        [SerializeField] protected T runtimeValue;

        public T Value
        {
            get => runtimeValue;
            set => runtimeValue = value;
        }

        public T InitialValue => initialValue;

        public virtual void Reset() => runtimeValue = initialValue;

        public event System.Action<T> OnValueChanged;

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
                runtimeValue = initialValue;
        }
    }
}