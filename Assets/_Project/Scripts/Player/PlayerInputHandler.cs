using UnityEngine;
using UnityEngine.InputSystem;
using KyberKlash.Data;

namespace KyberKlash.Player
{
    /// <summary>
    /// Central input handler using Unity's new Input System.
    /// Provides clean, normalized input values to the state machine.
    ///
    /// Control scheme (Smash-style):
    ///   WASD            - Move
    ///   Up Arrow        - Jump (tap again in air = double jump)
    ///   Double-tap Up   - Up Special (recovery / launch back to stage)
    ///   Left/Right Arrow- Directional light attack
    ///   Double-tap L/R  - Strong (heavy) attack
    ///   Down Arrow      - Block / shield (hold)
    ///   Double-tap Down - Drop through soft platform
    ///   Double-tap A/D  - Dash (toward / away)
    ///   Space           - Grab / Throw
    ///   J / K / L / I / Shift - legacy parity keys (still bound)
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActionsAsset;

        [Header("Player Index (for local multiplayer)")]
        [SerializeField] private int playerIndex = 0;

        // Input actions
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction lightAttackAction;
        private InputAction heavyAttackAction;
        private InputAction specialAction;
        private InputAction parryAction;
        private InputAction dashAction;
        private InputAction grabAction;
        private InputAction tauntAction;
        private InputAction pauseAction;
        private bool usingFallbackInput;

        // Input state
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool lightAttackPressed;
        private bool heavyAttackPressed;
        private bool specialPressed;
        private bool parryPressed;
        private bool parryHeld;
        private bool dashPressed;
        private bool grabPressed;
        private bool tauntPressed;
        private bool pausePressed;

        // Double-tap derived actions
        private bool recoverySpecialPressed;
        private bool dropThroughPressed;
        private bool strongAttackPressed;
        private Vector2 strongAttackDir;

        // Previous frame state for edge detection
        private bool jumpHeldPrev;
        private bool lightAttackHeldPrev;
        private bool heavyAttackHeldPrev;
        private bool specialHeldPrev;
        private bool parryHeldPrev;
        private bool dashHeldPrev;
        private bool grabHeldPrev;
        private bool tauntHeldPrev;
        private bool pauseHeldPrev;

        // Double-tap timing (per logical key)
        private const float DoubleTapWindow = 0.28f;
        private bool upPrev, downPrev, leftPrev, rightPrev, aPrev, dPrev;
        private float upTapTime, downTapTime, leftTapTime, rightTapTime, aTapTime, dTapTime;

        /// <summary>Current movement input (normalized)</summary>
        public Vector2 MoveInput => moveInput;
        public bool JumpPressed => jumpPressed;
        public bool JumpHeld => jumpHeld;
        public bool LightAttackPressed => lightAttackPressed;
        public bool HeavyAttackPressed => heavyAttackPressed;
        public bool SpecialPressed => specialPressed;
        public bool ParryPressed => parryPressed;
        public bool ParryHeld => parryHeld;
        public bool DashPressed => dashPressed;
        public bool GrabPressed => grabPressed;
        public bool TauntPressed => tauntPressed;
        public bool PausePressed => pausePressed;

        /// <summary>Double-tap Up -> Up Special recovery</summary>
        public bool RecoverySpecialPressed => recoverySpecialPressed;
        /// <summary>Double-tap Down -> drop through platform</summary>
        public bool DropThroughPressed => dropThroughPressed;
        /// <summary>Double-tap Left/Right -> strong (heavy) attack</summary>
        public bool StrongAttackPressed => strongAttackPressed;
        /// <summary>Direction of the most recent strong attack double-tap (-1 left, +1 right)</summary>
        public Vector2 StrongAttackDir => strongAttackDir;

        private void Awake()
        {
            SetupInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Update()
        {
            UpdateInputState();
        }

        private void SetupInputActions()
        {
            if (inputActionsAsset == null)
            {
                usingFallbackInput = true;
                Debug.LogWarning($"[PlayerInputHandler] No InputActionAsset assigned for player {playerIndex}; using keyboard/gamepad fallback controls.", this);
                return;
            }

            var actionMap = inputActionsAsset.FindActionMap("Player");
            if (actionMap == null)
            {
                usingFallbackInput = true;
                Debug.LogWarning($"[PlayerInputHandler] 'Player' action map not found; using keyboard/gamepad fallback controls.", this);
                return;
            }

            moveAction = actionMap.FindAction("Move");
            jumpAction = actionMap.FindAction("Jump");
            lightAttackAction = actionMap.FindAction("LightAttack");
            heavyAttackAction = actionMap.FindAction("HeavyAttack");
            specialAction = actionMap.FindAction("Special");
            parryAction = actionMap.FindAction("Parry");
            dashAction = actionMap.FindAction("Dash");
            grabAction = actionMap.FindAction("Grab");
            tauntAction = actionMap.FindAction("Taunt");
            pauseAction = actionMap.FindAction("Pause");

            if (playerIndex > 0)
            {
                var devices = InputSystem.devices;
                if (playerIndex < devices.Count)
                {
                    actionMap.devices = new[] { devices[playerIndex] };
                }
            }
        }

        private void EnableInputActions()
        {
            moveAction?.Enable();
            jumpAction?.Enable();
            lightAttackAction?.Enable();
            heavyAttackAction?.Enable();
            specialAction?.Enable();
            parryAction?.Enable();
            dashAction?.Enable();
            grabAction?.Enable();
            tauntAction?.Enable();
            pauseAction?.Enable();
        }

        private void DisableInputActions()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            lightAttackAction?.Disable();
            heavyAttackAction?.Disable();
            specialAction?.Disable();
            parryAction?.Disable();
            dashAction?.Disable();
            grabAction?.Disable();
            tauntAction?.Disable();
            pauseAction?.Disable();
        }

        private void UpdateInputState()
        {
            if (usingFallbackInput)
            {
                UpdateFallbackInputState();
                return;
            }

            jumpHeldPrev = jumpHeld;
            lightAttackHeldPrev = lightAttackPressed;
            heavyAttackHeldPrev = heavyAttackPressed;
            specialHeldPrev = specialPressed;
            parryHeldPrev = parryHeld;
            dashHeldPrev = dashPressed;
            tauntHeldPrev = tauntPressed;
            pauseHeldPrev = pausePressed;

            moveInput = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            jumpHeld = jumpAction?.ReadValue<float>() > 0.5f;
            bool lightAttackHeld = lightAttackAction?.ReadValue<float>() > 0.5f;
            bool heavyAttackHeld = heavyAttackAction?.ReadValue<float>() > 0.5f;
            bool specialHeld = specialAction?.ReadValue<float>() > 0.5f;
            parryHeld = parryAction?.ReadValue<float>() > 0.5f;
            bool dashHeld = dashAction?.ReadValue<float>() > 0.5f;
            bool grabHeld = grabAction?.ReadValue<float>() > 0.5f;
            bool tauntHeld = tauntAction?.ReadValue<float>() > 0.5f;
            bool pauseHeld = pauseAction?.ReadValue<float>() > 0.5f;

            jumpPressed = jumpHeld && !jumpHeldPrev;
            lightAttackPressed = lightAttackHeld && !lightAttackHeldPrev;
            heavyAttackPressed = heavyAttackHeld && !heavyAttackHeldPrev;
            specialPressed = specialHeld && !specialHeldPrev;
            parryPressed = parryHeld && !parryHeldPrev;
            dashPressed = dashHeld && !dashHeldPrev;
            grabPressed = grabHeld && !grabHeldPrev;
            tauntPressed = tauntHeld && !tauntHeldPrev;
            pausePressed = pauseHeld && !pauseHeldPrev;

            // Double-tap detection on the arrow / A-D keys (precise timing)
            DetectDoubleTaps();
        }

        private void DetectDoubleTaps()
        {
            recoverySpecialPressed = false;
            dropThroughPressed = false;
            strongAttackPressed = false;
            strongAttackDir = Vector2.zero;

            var keyboard = Keyboard.current;
            bool up = keyboard != null && keyboard.upArrowKey.isPressed;
            bool down = keyboard != null && keyboard.downArrowKey.isPressed;
            bool left = keyboard != null && keyboard.leftArrowKey.isPressed;
            bool right = keyboard != null && keyboard.rightArrowKey.isPressed;
            bool aKey = keyboard != null && keyboard.aKey.isPressed;
            bool dKey = keyboard != null && keyboard.dKey.isPressed;

            float t = Time.time;

            if (TapEdge(ref upPrev, up))
            {
                if (t - upTapTime < DoubleTapWindow) { recoverySpecialPressed = true; jumpPressed = false; }
                upTapTime = t;
            }
            if (TapEdge(ref downPrev, down))
            {
                if (t - downTapTime < DoubleTapWindow) dropThroughPressed = true;
                downTapTime = t;
            }
            if (TapEdge(ref leftPrev, left))
            {
                if (t - leftTapTime < DoubleTapWindow) { strongAttackPressed = true; strongAttackDir = Vector2.left; }
                leftTapTime = t;
            }
            if (TapEdge(ref rightPrev, right))
            {
                if (t - rightTapTime < DoubleTapWindow) { strongAttackPressed = true; strongAttackDir = Vector2.right; }
                rightTapTime = t;
            }
            if (TapEdge(ref aPrev, aKey))
            {
                if (t - aTapTime < DoubleTapWindow) { dashPressed = true; }
                aTapTime = t;
            }
            if (TapEdge(ref dPrev, dKey))
            {
                if (t - dTapTime < DoubleTapWindow) { dashPressed = true; }
                dTapTime = t;
            }
        }

        private static bool TapEdge(ref bool prevHeld, bool held)
        {
            bool edge = held && !prevHeld;
            prevHeld = held;
            return edge;
        }

        private void UpdateFallbackInputState()
        {
            jumpHeldPrev = jumpHeld;
            lightAttackHeldPrev = lightAttackPressed;
            heavyAttackHeldPrev = heavyAttackPressed;
            specialHeldPrev = specialPressed;
            parryHeldPrev = parryHeld;
            dashHeldPrev = dashPressed;
            tauntHeldPrev = tauntPressed;
            pauseHeldPrev = pausePressed;

            Vector2 keyboardMove = Vector2.zero;
            var keyboard = Keyboard.current;
            bool up = false, down = false, left = false, right = false, aKey = false, dKey = false;
            if (keyboard != null && playerIndex == 0)
            {
                if (keyboard.aKey.isPressed) { keyboardMove.x -= 1f; aKey = true; }
                if (keyboard.dKey.isPressed) { keyboardMove.x += 1f; dKey = true; }
                if (keyboard.sKey.isPressed) { keyboardMove.y -= 1f; down = true; }
                if (keyboard.wKey.isPressed) { keyboardMove.y += 1f; up = true; }
                if (keyboard.leftArrowKey.isPressed) { keyboardMove.x -= 1f; left = true; }
                if (keyboard.rightArrowKey.isPressed) { keyboardMove.x += 1f; right = true; }
                if (keyboard.downArrowKey.isPressed) { keyboardMove.y -= 1f; down = true; }
                if (keyboard.upArrowKey.isPressed) { keyboardMove.y += 1f; up = true; }
            }

            Vector2 gamepadMove = Vector2.zero;
            var gamepad = Gamepad.current;
            if (gamepad != null)
                gamepadMove = gamepad.leftStick.ReadValue();

            moveInput = gamepadMove.sqrMagnitude > keyboardMove.sqrMagnitude ? gamepadMove : keyboardMove.normalized;

            jumpHeld = (keyboard != null && up) || (gamepad != null && gamepad.buttonSouth.isPressed);
            bool lightAttackHeld = (keyboard != null && (left || right || keyboard.jKey.isPressed)) || (gamepad != null && gamepad.buttonWest.isPressed);
            bool heavyAttackHeld = (keyboard != null && keyboard.kKey.isPressed) || (gamepad != null && gamepad.buttonNorth.isPressed);
            bool specialHeld = (keyboard != null && keyboard.lKey.isPressed) || (gamepad != null && gamepad.buttonEast.isPressed);
            parryHeld = (keyboard != null && down) || (gamepad != null && gamepad.leftShoulder.isPressed);
            bool dashHeld = (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)) || (gamepad != null && gamepad.rightShoulder.isPressed);
            bool grabHeld = (keyboard != null && keyboard.spaceKey.isPressed) || (gamepad != null && gamepad.rightTrigger.isPressed);
            bool tauntHeld = keyboard != null && keyboard.tKey.isPressed;
            bool pauseHeld = (keyboard != null && keyboard.escapeKey.isPressed) || (gamepad != null && gamepad.startButton.isPressed);

            jumpPressed = jumpHeld && !jumpHeldPrev;
            lightAttackPressed = lightAttackHeld && !lightAttackHeldPrev;
            heavyAttackPressed = heavyAttackHeld && !heavyAttackHeldPrev;
            specialPressed = specialHeld && !specialHeldPrev;
            parryPressed = parryHeld && !parryHeldPrev;
            dashPressed = dashHeld && !dashHeldPrev;
            grabPressed = grabHeld && !grabHeldPrev;
            tauntPressed = tauntHeld && !tauntHeldPrev;
            pausePressed = pauseHeld && !pauseHeldPrev;

            // Double-tap timing uses the same key edges
            float t = Time.time;
            recoverySpecialPressed = false;
            dropThroughPressed = false;
            strongAttackPressed = false;
            strongAttackDir = Vector2.zero;

            if (TapEdge(ref upPrev, up)) { if (t - upTapTime < DoubleTapWindow) { recoverySpecialPressed = true; jumpPressed = false; } upTapTime = t; }
            if (TapEdge(ref downPrev, down)) { if (t - downTapTime < DoubleTapWindow) dropThroughPressed = true; downTapTime = t; }
            if (TapEdge(ref leftPrev, left)) { if (t - leftTapTime < DoubleTapWindow) { strongAttackPressed = true; strongAttackDir = Vector2.left; } leftTapTime = t; }
            if (TapEdge(ref rightPrev, right)) { if (t - rightTapTime < DoubleTapWindow) { strongAttackPressed = true; strongAttackDir = Vector2.right; } rightTapTime = t; }
            if (TapEdge(ref aPrev, aKey)) { if (t - aTapTime < DoubleTapWindow) dashPressed = true; aTapTime = t; }
            if (TapEdge(ref dPrev, dKey)) { if (t - dTapTime < DoubleTapWindow) dashPressed = true; dTapTime = t; }
        }

        public void Vibrate(float lowFrequency, float highFrequency, float duration)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
                StartCoroutine(VibrationRoutine(duration));
            }
        }

        private System.Collections.IEnumerator VibrationRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }

        public void SetPlayerIndex(int index)
        {
            playerIndex = index;
            if (inputActionsAsset != null)
            {
                var actionMap = inputActionsAsset.FindActionMap("Player");
                var devices = InputSystem.devices;
                if (index < devices.Count)
                {
                    actionMap.devices = new[] { devices[index] };
                }
            }
        }
    }
}
