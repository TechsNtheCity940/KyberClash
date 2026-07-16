using UnityEngine;
using UnityEngine.Events;

namespace KyberKlash.Data
{
    /// <summary>
    /// Float event channel for decoupled communication
    /// </summary>
    [CreateAssetMenu(fileName = "FloatEvent", menuName = "Kyber Clash/Events/Float Event")]
    public class FloatGameEvent : GameEvent<float> { }

    /// <summary>
    /// Int event channel
    /// </summary>
    [CreateAssetMenu(fileName = "IntEvent", menuName = "Kyber Clash/Events/Int Event")]
    public class IntGameEvent : GameEvent<int> { }

    /// <summary>
    /// Bool event channel
    /// </summary>
    [CreateAssetMenu(fileName = "BoolEvent", menuName = "Kyber Clash/Events/Bool Event")]
    public class BoolGameEvent : GameEvent<bool> { }

    /// <summary>
    /// Vector3 event channel
    /// </summary>
    [CreateAssetMenu(fileName = "Vector3Event", menuName = "Kyber Clash/Events/Vector3 Event")]
    public class Vector3GameEvent : GameEvent<Vector3> { }

    /// <summary>
    /// String event channel
    /// </summary>
    [CreateAssetMenu(fileName = "StringEvent", menuName = "Kyber Clash/Events/String Event")]
    public class StringGameEvent : GameEvent<string> { }

    /// <summary>
    /// Generic GameObject event channel
    /// </summary>
    [CreateAssetMenu(fileName = "GameObjectEvent", menuName = "Kyber Clash/Events/GameObject Event")]
    public class GameObjectGameEvent : GameEvent<GameObject> { }

    /// <summary>
    /// CharacterData event channel
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDataEvent", menuName = "Kyber Clash/Events/CharacterData Event")]
    public class CharacterDataGameEvent : GameEvent<CharacterData> { }

    /// <summary>
    /// AttackSO event channel
    /// </summary>
    [CreateAssetMenu(fileName = "AttackSOEvent", menuName = "Kyber Clash/Events/AttackSO Event")]
    public class AttackSOGameEvent : GameEvent<AttackSO> { }

    /// <summary>
    /// FormSO event channel
    /// </summary>
    [CreateAssetMenu(fileName = "FormSOEvent", menuName = "Kyber Clash/Events/FormSO Event")]
    public class FormSOGameEvent : GameEvent<FormSO> { }

    /// <summary>
    /// Damage info struct for events
    /// </summary>
    [System.Serializable]
    public struct DamageInfo
    {
        public GameObject attacker;
        public GameObject victim;
        public float damage;
        public Vector3 knockback;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public AttackSO attackData;
        public bool isPerfectParry;
        public bool isCounter;
    }

    /// <summary>
    /// Damage event channel
    /// </summary>
    [CreateAssetMenu(fileName = "DamageEvent", menuName = "Kyber Clash/Events/Damage Event")]
    public class DamageGameEvent : GameEvent<DamageInfo> { }

    /// <summary>
    /// Player state change event
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStateEvent", menuName = "Kyber Clash/Events/Player State Event")]
    public class PlayerStateGameEvent : GameEvent<PlayerStateInfo> { }

    [System.Serializable]
    public struct PlayerStateInfo
    {
        public GameObject player;
        public PlayerStateType previousState;
        public PlayerStateType newState;
    }

    public enum PlayerStateType
    {
        Idle,
        GroundMove,
        Air,
        Jump,
        Dash,
        Attack,
        HeavyAttack,
        AerialAttack,
        Special,
        Parry,
        Block,
        HitStun,
        Knockback,
        Dead,
        Respawn
    }

    /// <summary>
    /// Meter change event
    /// </summary>
    [CreateAssetMenu(fileName = "MeterChangeEvent", menuName = "Kyber Clash/Events/Meter Change Event")]
    public class MeterChangeGameEvent : GameEvent<MeterChangeInfo> { }

    [System.Serializable]
    public struct MeterChangeInfo
    {
        public GameObject player;
        public float previousValue;
        public float newValue;
        public float maxValue;
        public MeterChangeReason reason;
    }

    public enum MeterChangeReason
    {
        HitDealt,
        HitTaken,
        PerfectParry,
        Whiff,
        PassiveRegen,
        SpecialUsed,
        EnhancedAttackUsed,
        FormMechanic
    }

    /// <summary>
    /// Float variable SO
    /// </summary>
    [CreateAssetMenu(fileName = "FloatVariable", menuName = "Kyber Clash/Variables/Float Variable")]
    public class FloatGameVariable : GameVariable<float> { }

    /// <summary>
    /// Int variable SO
    /// </summary>
    [CreateAssetMenu(fileName = "IntVariable", menuName = "Kyber Clash/Variables/Int Variable")]
    public class IntGameVariable : GameVariable<int> { }

    /// <summary>
    /// Bool variable SO
    /// </summary>
    [CreateAssetMenu(fileName = "BoolVariable", menuName = "Kyber Clash/Variables/Bool Variable")]
    public class BoolGameVariable : GameVariable<bool> { }

    /// <summary>
    /// Vector3 variable SO
    /// </summary>
    [CreateAssetMenu(fileName = "Vector3Variable", menuName = "Kyber Clash/Variables/Vector3 Variable")]
    public class Vector3GameVariable : GameVariable<Vector3> { }

    /// <summary>
    /// CharacterData variable SO
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDataVariable", menuName = "Kyber Clash/Variables/CharacterData Variable")]
    public class CharacterDataGameVariable : GameVariable<CharacterData> { }

    /// <summary>
    /// FormSO variable SO
    /// </summary>
    [CreateAssetMenu(fileName = "FormSOVariable", menuName = "Kyber Clash/Variables/FormSO Variable")]
    public class FormSOGameVariable : GameVariable<FormSO> { }
}