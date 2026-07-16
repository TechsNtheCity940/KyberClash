# Kyber Clash - Unity Lightsaber Platform Fighter

A 3D platform fighter inspired by Super Smash Bros. mechanics combined with Star Wars-style lightsaber dueling. Built with modern Unity architecture (2026 best practices).

## 🎮 Game Vision

**Core Fantasy**: Responsive momentum-based movement, satisfying lightsaber combat (attacks + timed parries), damage/knockback system leading to ring-outs, a buildable/spendable "Resonance" meter for enhanced moves, customizable character Forms (fighting styles), and dynamic stages with hazards.

## 🏗️ Architecture Overview

### Design Principles
- **Data-Driven**: Everything configurable via ScriptableObjects (AttackSO, FormSO, CharacterData, StageData)
- **SOAP Pattern**: ScriptableObject Architecture Pattern - events, variables, and data containers for decoupling
- **Finite State Machine**: Clean OOP state machine with base `State` class and individual state classes
- **Event-Driven**: C# events + UnityEvents for communication, avoiding tight coupling
- **Netcode Ready**: Components designed to easily extend `NetworkBehaviour` for multiplayer
- **Object Pooling**: VFX/particles pooled from the start
- **Testable**: Modular components with clear interfaces

### Folder Structure
```
Assets/_Project/
├── Scripts/
│   ├── Core/           # StateMachine, GameManager, Base classes
│   ├── Player/         # PlayerController, InputHandler, States, Camera, Meter
│   ├── Combat/         # PlayerCombat, Damageable, IDamageable
│   ├── Data/           # All ScriptableObjects (AttackSO, FormSO, CharacterData, etc.)
│   ├── Stage/          # StageManager, HazardZone, SpawnPoint
│   ├── UI/             # PlayerHUD, HUDManager
│   ├── VFX/            # LightsaberVFX, VFXPool, HitImpactVFX
│   └── Utilities/      # TestDataCreator, helpers
├── Data/               # Created ScriptableObject assets
├── Prefabs/            # Player, Stage, VFX prefabs
├── Scenes/             # PrototypeArena, MainMenu
├── Materials/          # Saber materials, blast zone materials
├── VFX/                # Particle systems
├── Animations/         # Animation controllers
├── Audio/              # SFX, Music
├── UI/                 # UI Toolkit/Canvas assets
└── Settings/           # Input Actions, URP assets
```

## 🎯 Core Systems

### 1. Data Layer (ScriptableObjects)

**AttackSO** - Completely data-driven attack definition:
- Frame data (startup/active/recovery)
- Damage, knockback, angle, growth
- Hitbox shape/size/offset (Sphere, Box, Capsule, SaberArc)
- Meter costs/gains
- Parry interaction (perfect parry window, rewards)
- Visual/audio references

**FormSO** - Fighting style that modifies character:
- Movement modifiers (speed, jump, air control, gravity)
- Combat modifiers (damage, knockback, attack speed, meter)
- Unique attack overrides per slot
- Unique mechanic type (CounterStance, PrecisionParry, PerfectDeflection, etc.)
- Visual identity (saber color, gradient, VFX)

**CharacterData** - Base character definition:
- Base stats (speed, jump, weight, gravity)
- Default form and available forms
- Default attacks per slot
- Visual/audio references

**StageData** - Stage configuration:
- Blast zones (ring-out boundaries)
- Hazards with timing
- Music, visuals

### 2. State Machine

```
State (abstract)
├── State<T> (generic for owner access)
    └── PlayerState (base for all player states)
        ├── Movement States: Idle, Move, Jump, Air, Dash, AirDash
        └── Combat States: Attack, Parry, HitStun, Knockback, Dead, Respawn
```

Each state has: `Enter()`, `Exit()`, `UpdateLogic()`, `PhysicsUpdate()`, `CheckTransitions()`

### 3. Player Controller

**Movement Features**:
- Momentum-based 3D platformer movement
- Coyote time + jump buffering
- Ground/air acceleration curves
- Dash (ground + air) with cooldowns
- Wall jump support
- Fast fall
- Form-based stat modifiers

**Components**:
- `PlayerInputHandler` - New Input System wrapper
- `PlayerCombat` - Attack/parry/hitbox logic
- `PlayerMeter` - Resonance/Force meter
- `LightsaberVFX` - Visual saber effect

### 4. Combat System

**Attack Execution**:
1. State requests attack → `PlayerCombat.StartAttack(AttackSO)`
2. `AttackState` tracks frames
3. At startup frames → `ActivateHitbox()`
4. Overlap check (Sphere/Box/Capsule/SaberArc)
5. Process hits → `IDamageable.TakeDamage()`
6. At active frames end → `DeactivateHitbox()`
7. Recovery frames → transition

**Parry System**:
- Timed window (configurable per form)
- Perfect parry (first frames) → meter reward, riposte opportunity
- Normal parry → block stun, pushback
- Form-specific mechanics (Makashi riposte, Soresu deflection, etc.)

**Clash Detection**:
- Overlap check for other active hitboxes
- Both attacks get hit pause
- Clash VFX/SFX

### 5. Damage & Knockback

**DamageInfo** struct carries:
- Attacker, victim, damage amount
- Knockback vector, hit point/normal
- Attack data reference
- Perfect parry/counter flags

**Knockback Formula**:
```
finalKnockback = baseKnockback + (targetDamage% * knockbackGrowth)
finalKnockback = Clamp(min, max) * weightMultiplier * formMultiplier
```

**Ring-out**: StageManager checks blast zones

### 6. Meter (Resonance/Force)

- Builds from: hits dealt, hits taken, perfect parries, whiffs, passive regen
- Spends on: specials, enhanced attacks, form mechanics
- NetworkVariable-ready

### 7. Stage & Hazards

**HazardZone** component:
- Trigger-based damage
- Warning → Active → Cooldown cycle
- Random or timed activation
- Visual/audio feedback

### 8. Input System

**Actions**: Move, Jump, LightAttack, HeavyAttack, Special, Parry, Dash, Taunt, Pause
- Gamepad + Keyboard bindings
- Local multiplayer via device binding
- Edge detection for presses

### 9. Camera (Cinemachine)

- Target group for multiple players
- Dynamic zoom to fit all players
- Screen shake via Impulse
- Focus on specific player (KO, respawn)

### 10. VFX System

**LightsaberVFX**:
- LineRenderer for blade
- TrailRenderer for motion
- Gradient color from FormSO
- Ignition/extinguish animation
- Swing sound based on tip velocity
- Clash flash effect

**VFXPool**: Object pooling for particles

## 🚀 Getting Started

### Prerequisites
- Unity 6 (or 2022.3 LTS)
- Packages (auto-installed via manifest.json):
  - Input System
  - Netcode for GameObjects
  - Unity Transport
  - Cinemachine
  - Universal Render Pipeline
  - Animation Rigging
  - VFX Graph
  - Timeline
  - TextMeshPro
  - Addressables

### Setup
1. Open project in Unity
2. Run `Kyber Clash > Create Test Data Assets` (Editor menu)
3. Open `Assets/_Project/Scenes/PrototypeArena.unity`
4. Enter Play Mode

### Controls (Default)
| Action | Gamepad | Keyboard |
|--------|---------|----------|
| Move | Left Stick | WASD |
| Jump | A (South) | Space |
| Light Attack | X (West) | J |
| Heavy Attack | Y (North) | K |
| Special | B (East) | L |
| Parry | Right Trigger | Left Shift |
| Dash | Left Trigger | Left Ctrl |
| Taunt | Select | T |
| Pause | Start | Escape |

## 🔧 Extending the Game

### Adding a New Attack
1. Create `AttackSO` asset in `Assets/_Project/Data/Attacks/`
2. Configure frame data, damage, knockback, hitbox
3. Assign to CharacterData default attacks or FormSO override

### Adding a New Form
1. Create `FormSO` asset in `Assets/_Project/Data/Forms/`
2. Set movement/combat modifiers
3. Choose mechanic type or create custom
4. Override specific attacks if needed
5. Add to CharacterData.availableForms

### Adding a New Character
1. Create `CharacterData` asset in `Assets/_Project/Data/Characters/`
2. Set base stats, weight class
3. Assign default form + available forms
4. Assign default attacks
5. Set visual/audio references

### Adding a New Stage
1. Create `StageData` asset in `Assets/_Project/Data/Stages/`
2. Set blast zones
3. Add hazards with timing
4. Create stage prefab with SpawnPoints
5. Assign to StageManager

### Adding a New Hazard Type
1. Extend `HazardZone` or create new component
2. Add to StageData.hazards array
3. Configure warning/active/cooldown timing
4. Add VFX/SFX

### Multiplayer (Netcode)
All player components are designed for easy conversion:
- `PlayerController` → inherit `NetworkBehaviour`
- `PlayerMeter.CurrentValue` → `NetworkVariable<float>`
- `PlayerCombat` RPCs for attack/parry
- `Damageable.TakeDamage` → `ServerRpc`
- State machine runs on server, replicates to clients

## 📝 Key Classes Reference

| Class | Purpose | Location |
|-------|---------|----------|
| `StateMachine` | Generic FSM | `Core/StateMachine.cs` |
| `PlayerState` | Base player state | `Player/States/PlayerState.cs` |
| `PlayerController` | Main player logic | `Player/PlayerController.cs` |
| `PlayerCombat` | Attack/hitbox/parry | `Combat/PlayerCombat.cs` |
| `PlayerMeter` | Resonance meter | `Player/PlayerMeter.cs` |
| `PlayerInputHandler` | Input System wrapper | `Player/PlayerInputHandler.cs` |
| `Damageable` | Health/damage component | `Combat/Damageable.cs` |
| `StageManager` | Stage/hazard management | `Stage/StageManager.cs` |
| `HazardZone` | Damage area | `Combat/Damageable.cs` |
| `CameraManager` | Cinemachine wrapper | `Player/CameraManager.cs` |
| `LightsaberVFX` | Saber visual | `VFX/LightsaberVFX.cs` |
| `VFXPool` | Object pooling | `VFX/LightsaberVFX.cs` |
| `PlayerHUD` | Damage/meter UI | `UI/PlayerHUD.cs` |
| `GameManager` | Match logic | `Core/GameManager.cs` |

## 🎨 Visual Style Guide

- **Saber Colors**: Form-based (Blue/Green/Cyan/Red/Purple/Yellow/White)
- **VFX**: Stylized, not realistic - glowing, particle trails
- **Hit Effects**: Brief pause, particles, screen shake, directional indicators
- **UI**: Clean, readable, color-coded by damage/meter level

## 📦 Build Pipeline

- Addressables for content delivery
- URP for rendering
- VFX Graph for effects
- Timeline for cinematics

## 🧪 Testing

- `TestDataCreator` creates baseline assets
- PrototypeArena scene for manual testing
- Unit tests for damage/knockback formulas (to add)
- Integration tests for state transitions (to add)

## 🗺️ Roadmap / Next Steps

1. **Animation System**: Animation events, root motion, Animancer integration
2. **Online Multiplayer**: Netcode implementation, rollback, lobby
3. **Advanced Combat**: Throws, ledge mechanics, shield, dodge
4. **Stage Hazards**: More types, stage transitions, environmental interaction
5. **UI Polish**: UI Toolkit, menus, character select, replay system
6. **AI/Bots**: Behavior trees for single-player
7. **Content Pipeline**: Addressables build, asset bundles
8. **Performance**: Burst/Jobs for physics queries, pooling optimization

## 📄 License

Proprietary - Kyber Clash Project

---

*Built with Unity 6 • Modern Architecture • Data-Driven Design*