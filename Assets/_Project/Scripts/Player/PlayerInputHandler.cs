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

        // Double-tap timing (per player, per logical key) for recovery/strong/drop-through.
        private const float DoubleTapWindow = 0.28f;
        private bool[] upPrev = new bool[2];
        private bool[] downPrev = new bool[2];
        private bool[] leftPrev = new bool[2];
        private bool[] rightPrev = new bool[2];
        private float[] upTapTime = new float[2];
        private float[] downTapTime = new float[2];
        private float[] leftTapTime = new float[2];
        private float[] rightTapTime = new float[2];

        // --- AI override: when enabled, an AI brain writes this frame's input instead of
        //     reading the keyboard/gamepad. Used for single-player vs CPU matches. ---
        [System.Serializable]
        public struct AICommand
        {
            public Vector2 move;
            public bool jump;
            public bool lightAttack;
            public bool heavyAttack;
            public bool special;
            public bool parry;
            public bool dash;
            public bool grab;
            public bool recoverySpecial;
        }
        private bool aiOverrideActive;
        private AICommand aiCommand;

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
            // Only P1 uses the keyboard for double-tap chords (P2 strong attack is on a
            // dedicated key, see UpdateFallbackInputState). P1 layout: WASD move, arrow
            // keys for attack/double-tap recovery.
            bool up = keyboard != null && keyboard.upArrowKey.isPressed;
            bool down = keyboard != null && keyboard.downArrowKey.isPressed;
            bool left = keyboard != null && keyboard.leftArrowKey.isPressed;
            bool right = keyboard != null && keyboard.rightArrowKey.isPressed;

            float t = Time.time;

            if (TapEdge(ref upPrev[0], up))
            {
                if (t - upTapTime[0] < DoubleTapWindow) { recoverySpecialPressed = true; jumpPressed = false; }
                upTapTime[0] = t;
            }
            if (TapEdge(ref downPrev[0], down))
            {
                if (t - downTapTime[0] < DoubleTapWindow) dropThroughPressed = true;
                downTapTime[0] = t;
            }
            if (TapEdge(ref leftPrev[0], left))
            {
                if (t - leftTapTime[0] < DoubleTapWindow) { strongAttackPressed = true; strongAttackDir = Vector2.left; }
                leftTapTime[0] = t;
            }
            if (TapEdge(ref rightPrev[0], right))
            {
                if (t - rightTapTime[0] < DoubleTapWindow) { strongAttackPressed = true; strongAttackDir = Vector2.right; }
                rightTapTime[0] = t;
            }
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

            // Local action state for this frame (merged from keyboard + gamepad below).
            bool lightAttackHeld = false;
            bool heavyAttackHeld = false;
            bool specialHeld = false;
            bool dashHeld = false;
            bool grabHeld = false;
            bool tauntHeld = false;
            bool pauseHeld = false;

            Vector2 p1Move = Vector2.zero;
            Vector2 p2Move = Vector2.zero;
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if (keyboard != null)
            {
                // --- Player 1 (WASD move + arrow keys / J K L for actions) ---
                if (playerIndex == 0)
                {
                    if (keyboard.wKey.isPressed) p1Move.y += 1f;
                    if (keyboard.sKey.isPressed) p1Move.y -= 1f;
                    if (keyboard.aKey.isPressed) p1Move.x -= 1f;
                    if (keyboard.dKey.isPressed) p1Move.x += 1f;

                    jumpHeld = keyboard.upArrowKey.isPressed;
                    lightAttackHeld = keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed || keyboard.jKey.isPressed;
                    heavyAttackHeld = keyboard.kKey.isPressed;
                    specialHeld = keyboard.lKey.isPressed;
                    parryHeld = keyboard.downArrowKey.isPressed;
                    // P1 dash: left Ctrl / left Shift
                    dashHeld = keyboard.leftShiftKey.isPressed || keyboard.leftCtrlKey.isPressed;
                    grabHeld = keyboard.spaceKey.isPressed;
                    tauntHeld = keyboard.tKey.isPressed;
                    pauseHeld = keyboard.escapeKey.isPressed;
                }
                // --- Player 2 (numpad only, fully disjoint from P1's WASD/arrows/JKL) ---
                else
                {
                    if (keyboard.numpad8Key.isPressed) p2Move.y += 1f;   // up
                    if (keyboard.numpad2Key.isPressed) p2Move.y -= 1f;   // down
                    if (keyboard.numpad4Key.isPressed) p2Move.x -= 1f;   // left
                    if (keyboard.numpad6Key.isPressed) p2Move.x += 1f;   // right

                    jumpHeld = keyboard.numpad8Key.isPressed;            // numpad8 = jump
                    lightAttackHeld = keyboard.numpad1Key.isPressed;
                    heavyAttackHeld = keyboard.numpad3Key.isPressed;
                    specialHeld = keyboard.numpad5Key.isPressed;
                    parryHeld = keyboard.numpad7Key.isPressed;
                    dashHeld = keyboard.numpad0Key.isPressed;
                    grabHeld = keyboard.numpad9Key.isPressed;
                    tauntHeld = keyboard.numpadPeriodKey.isPressed;
                    pauseHeld = keyboard.escapeKey.isPressed;
                }
            }

            Vector2 gamepadMove = gamepad != null ? gamepad.leftStick.ReadValue() : Vector2.zero;

            // Choose the dominant move source for this player.
            Vector2 move = Vector2.zero;
            if (playerIndex == 0)
            {
                move = p1Move.sqrMagnitude > 0.01f ? p1Move.normalized : gamepadMove;
            }
            else
            {
                // P2: keyboard if present, otherwise a second gamepad (if plugged in).
                if (p2Move.sqrMagnitude > 0.01f)
                {
                    move = p2Move.normalized;
                }
                else if (gamepad != null)
                {
                    move = gamepadMove;
                    jumpHeld = jumpHeld || gamepad.buttonSouth.isPressed;
                    lightAttackHeld = lightAttackHeld || gamepad.buttonWest.isPressed;
                    heavyAttackHeld = heavyAttackHeld || gamepad.buttonNorth.isPressed;
                    specialHeld = specialHeld || gamepad.buttonEast.isPressed;
                    parryHeld = parryHeld || gamepad.leftShoulder.isPressed;
                    dashHeld = dashHeld || gamepad.rightShoulder.isPressed;
                    grabHeld = grabHeld || gamepad.rightTrigger.isPressed;
                }
            }

            moveInput = move;

            // Gamepad fills any action not already set (so a single gamepad can drive P1
            // if keyboard is absent).
            if (gamepad != null && playerIndex == 0)
            {
                jumpHeld = jumpHeld || gamepad.buttonSouth.isPressed;
                lightAttackHeld = lightAttackHeld || gamepad.buttonWest.isPressed;
                heavyAttackHeld = heavyAttackHeld || gamepad.buttonNorth.isPressed;
                specialHeld = specialHeld || gamepad.buttonEast.isPressed;
                parryHeld = parryHeld || gamepad.leftShoulder.isPressed;
                dashHeld = dashHeld || gamepad.rightShoulder.isPressed;
                grabHeld = grabHeld || gamepad.rightTrigger.isPressed;
            }

            jumpPressed = jumpHeld && !jumpHeldPrev;
            lightAttackPressed = lightAttackHeld && !lightAttackHeldPrev;
            heavyAttackPressed = heavyAttackHeld && !heavyAttackHeldPrev;
            specialPressed = specialHeld && !specialHeldPrev;
            parryPressed = parryHeld && !parryHeldPrev;
            dashPressed = dashHeld && !dashHeldPrev;
            grabPressed = grabHeld && !grabHeldPrev;
            tauntPressed = tauntHeld && !tauntHeldPrev;
            pausePressed = pauseHeld && !pauseHeldPrev;

            // P2: no double-tap chords; drop-through on numpad2 edge.
            if (playerIndex != 0)
            {
                recoverySpecialPressed = false;
                dropThroughPressed = keyboard != null && keyboard.numpad2Key.wasPressedThisFrame;
                strongAttackPressed = false;
                strongAttackDir = Vector2.zero;
            }
            else
            {
                // P1 double-tap chords (recovery/strong/drop-through).
                DetectDoubleTaps();
            }

            // --- AI override takes precedence over any hardware input. ---
            if (aiOverrideActive)
            {
                ApplyAICommand();
            }
        }

        public void EnableAIOverride(bool enabled)
        {
            aiOverrideActive = enabled;
            if (!enabled)
            {
                // Reset edge trackers so re-enabling doesn't fire stale presses.
                jumpHeldPrev = lightAttackHeldPrev = heavyAttackHeldPrev = false;
                specialHeldPrev = parryHeldPrev = dashHeldPrev = grabHeldPrev = false;
                moveInput = Vector2.zero;
            }
        }

        public void SetAICommand(AICommand cmd) => aiCommand = cmd;

        private void ApplyAICommand()
        {
            // Use locals for this frame's held state, then derive edge-triggered "pressed"
            // values against the previous-frame held flags (set at the top of UpdateInputState).
            bool jh = aiCommand.jump;
            bool lh = aiCommand.lightAttack;
            bool hh = aiCommand.heavyAttack;
            bool sh = aiCommand.special;
            bool ph = aiCommand.parry;
            bool dh = aiCommand.dash;
            bool gh = aiCommand.grab;

            moveInput = aiCommand.move;
            jumpHeld = jh;
            parryHeld = ph;

            jumpPressed = jh && !jumpHeldPrev;
            lightAttackPressed = lh && !lightAttackHeldPrev;
            heavyAttackPressed = hh && !heavyAttackHeldPrev;
            specialPressed = sh && !specialHeldPrev;
            parryPressed = ph && !parryHeldPrev;
            dashPressed = dh && !dashHeldPrev;
            grabPressed = gh && !grabHeldPrev;
            tauntPressed = false;
            pausePressed = false;

            recoverySpecialPressed = aiCommand.recoverySpecial && !jumpHeldPrev;
            dropThroughPressed = false;
            strongAttackPressed = false;
            strongAttackDir = Vector2.zero;
        }

        private static bool TapEdge(ref bool prevHeld, bool held)
        {
            bool edge = held && !prevHeld;
            prevHeld = held;
            return edge;
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
