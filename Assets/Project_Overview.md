# Kyber Clash Technical Overview

Kyber Clash is a high-speed, data-driven 2.5D platform fighter built in Unity. It features a unique "Form" system where players can switch fighting styles at runtime, altering their moveset, movement properties, and special mechanics. The project utilizes a decoupled, state-machine-driven architecture and a robust data-driven combat system.

## 1. Project Description
Kyber Clash is designed for competitive local and online multiplayer, targeting fans of the platform fighter genre. The core pillars of the experience are **High-Mobility Combat**, **Dynamic Form Switching**, and **Technical Defensive Play (Parrying/Clashing)**. Players battle on floating stages where the goal is to knock opponents out of the "Blast Zones" by increasing their damage percentage to amplify knockback.

## 2. Gameplay Flow / User Loop
1.  **Boot & Initialization**: The `GameManager` (singleton) initializes and prepares global systems (Audio, UI, Stage management).
2.  **Match Setup**: Players select characters and fighting styles (Forms). In the current prototype, the `GameManager` auto-starts matches with configured `CharacterData` and `StageData`.
3.  **Spawning**: `GameManager` requests the `StageManager` for spawn points and instantiates `PlayerController` prefabs.
4.  **Combat Loop**: Players engage in combat using a mix of light, heavy, and special attacks. Success increases the opponent's "Meter" (damage percentage).
5.  **State Transitions**: Characters transition between `MovementStates` (Idle, Move, Jump, Air) and `CombatStates` (Attack, Parry, HitStun, Knockback).
6.  **Elimination**: When a player hits a "Blast Zone" (out of bounds), the `PlayerController` triggers a death event, and `GameManager` manages stock reduction and respawning.
7.  **Match End**: The match concludes when time expires or only one player has stocks remaining.

## 3. Architecture
The project follows a component-based architecture with a clear separation between data (ScriptableObjects), logic (Controllers), and state (StateMachine).

### Finite State Machine (FSM)
The core behavior of entities is governed by a generic State Machine.
*   `StateMachine`: A reusable component that manages state instances, transitions, and lifecycle updates (`Update`, `FixedUpdate`).
*   `State`: An abstract base class defining `Enter`, `Exit`, `UpdateLogic`, and `CheckTransitions`.
*   `Location:` `Assets/_Project/Scripts/Core`

### Central Management
*   `GameManager`: The "Brain" of the match. Handles match timers, stock counting, and player lifecycle.
*   `StageManager`: Manages the physical environment, including blast zones and spawn points.
*   `CameraManager`: Utilizes Cinemachine to track multiple players within the frame.
*   `Location:` `Assets/_Project/Scripts/Core`, `Assets/_Project/Scripts/Stage`, `Assets/_Project/Scripts/Player`

## 4. Game Systems & Domain Concepts

### Data-Driven Combat System
Combat is entirely driven by `AttackSO` assets, removing hardcoded logic from the scripts.
*   `PlayerCombat`: Handles hitbox generation, clash detection, and parry logic. It uses `IDamageable` to communicate with targets.
*   `AttackSO`: A ScriptableObject containing frame data, hitbox shapes, damage, knockback growth, and VFX/SFX references.
*   `DamageInfo`: A struct passed between entities during a hit, containing data about the attacker, victim, and the specific attack used.
*   `Location:` `Assets/_Project/Scripts/Combat`, `Assets/_Project/Scripts/Data`

### Character & Form System
Characters are composed of a base identity and a swappable "Form."
*   `CharacterData`: Defines base stats like weight class, gravity, and default animations.
*   `FormSO`: Modifies base stats (e.g., `moveSpeedMultiplier`) and provides unique attack overrides and special mechanics (e.g., `AcrobaticFlow`, `PrecisionParry`).
*   `PlayerMeter`: Manages both the damage percentage (health) and the energy used for specials or form changes.
*   `Location:` `Assets/_Project/Scripts/Data`, `Assets/_Project/Scripts/Player`

### Movement System
A physics-based platformer controller tuned for combat.
*   `PlayerController`: Orchestrates the `StateMachine` and handles low-level physics constraints like Z-axis locking.
*   `MovementStates`: Includes specialized states like `CoyoteTime`, `JumpBuffer`, and `DashState`.
*   `Location:` `Assets/_Project/Scripts/Player/States`

## 5. Scene Overview
*   **PrototypeArena**: The primary testing scene containing a `Stage_PrototypeArena` prefab with blast zones and spawn points.
*   **Loading/Flow**: The `GameManager` is persistent across scenes (`DontDestroyOnLoad`). Matches are typically reloaded by reloading the active scene or returning to a (planned) Main Menu.
*   `Location:` `Assets/_Project/Scenes`

## 6. UI System
The UI uses a hybrid approach with UGUI and `HUDManager`.
*   `HUDManager`: A singleton that manages the creation and updating of player-specific HUD elements.
*   `PlayerHUD`: A component attached to UI prefabs that binds to a `PlayerController` and updates damage percentages and meter bars via events.
*   `Location:` `Assets/_Project/Scripts/UI`

## 7. Asset & Data Model
*   **ScriptableObjects**: Heavily used for all content. `CharacterData`, `FormSO`, and `AttackSO` are the primary data containers.
*   **Prefabs**: Players and Stages are modular. The `Player` prefab is generic; its visual and logic components are initialized at runtime using `CharacterData`.
*   **VFX/SFX**: Managed through the `VFXPool` and `GameAudioManager`. VFX are typically spawned at hit points or attached to saber tips as `TrailRenderers`.
*   `Location:` `Assets/_Project/Data`, `Assets/_Project/Prefabs`

## 8. Notes, Caveats & Gotchas
*   **Z-Axis Locking**: The game is 2.5D. The `PlayerController` enforces `position.z = 0` every frame. Ensure any new environmental hazards or moving platforms also account for this constraint.
*   **Frame Data**: `AttackSO` uses 60fps as a baseline for frame counts. Logic converts these to seconds (e.g., `startupFrames / 60f`) for compatibility with variable framerates.
*   **Layer Collision**: The combat system relies on specific layers: `Hurtbox` for characters, `Clash` for sabers, and `Stage` for ground. Avoid putting combat logic on the `Default` layer.
*   **Form Overrides**: If a `FormSO` has a null attack slot, the system defaults to the `CharacterData` attack. Always ensure `CharacterData` has a valid fallback attack assigned.