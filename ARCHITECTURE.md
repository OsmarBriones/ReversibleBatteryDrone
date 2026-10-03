# ReversibleBatteryDrone Architecture

This document describes the runtime structure, data flow, and design decisions for `ReversibleBatteryDrone`.

---

## High-Level Concept

1. **Trigger / Hook:** Intercepts `ItemToggle.Update` when the local player holds an `ItemDroneBattery` to drive a 3-state cycle (`Off` -> `Charge` -> `Drain` -> `Off`).
2. **Authority / Networking:** Input interception is local for responsiveness, while state is synchronized across the lobby via Photon RPC (`SetModeRPC`). Energy drain, battery charging, and life leeching execute host-only (`SemiFunc.IsMasterClientOrSingleplayer()`) for authoritative state mutations.
3. **Outcome:** The charge drone becomes bidirectional: charging items in yellow mode, and siphoning battery from items or leeching health from monsters/players into its battery in red mode.

---

## Main Data Flow

### 1. Initialization & Networking Cache
- **`ItemDroneBattery_Start_Patch`** (Postfix on `ItemDroneBattery.Start`):
  - Automatically attaches the `ReversibleDroneController` MonoBehaviour component to the drone GameObject.
  - Calls `PhotonView.RefreshRpcMonoBehaviourCache()` so PUN 2 registers runtime-added RPCs (`SetModeRPC`) on both host and clients.
  - Caches original colors (yellow beam, yellow emission, light).
  - Also refreshed in `ReversibleDroneController.Awake()`.

### 2. 3-State Sequential Interaction & Empty Battery Handling
- **`ItemToggle_Update_Patch`** (Prefix on `ItemToggle.Update`):
  - Checks if `physGrabObject.heldByLocalPlayer` and `SemiFunc.InputDown(InputKey.Interact)` (`E`).
  - Calls `ReversibleDroneController.HandleInteractPress()`:
    - **When Drone Has Charge**:
      - **From Off (0) -> Charge (1)**: Calls `itemToggle.ToggleItem(true)`, sets mode to `Charge`, beam & LED yellow.
      - **From Charge (1) -> Drain (2)**: Keeps drone active, switches mode to `Drain`, beam & LED red.
      - **From Drain (2) -> Off (0)**: Calls `itemToggle.ToggleItem(false)`, resets mode to `Charge`.
    - **When Drone Is Empty (0% Battery)**:
      - **From Off (0) -> Drain (2)**: Immediately skips unusable Charge mode and turns ON in `Drain` mode (Red), allowing direct recharging.
      - **From Drain (2) -> Off (0)**: Shuts drone OFF.
  - Returns `false` to skip vanilla toggle logic.
- **Empty Battery Lifecyle & Patches**:
  - **`ItemDrone_StateOff_Patch`**: When toggled ON in Drain mode while empty, transitions `ItemDrone` from `State.Off` to `State.Searching`.
  - **`ItemDrone_StateSet_Patch`**: Intercepts `ItemDrone.StateSet` and suppresses transitions to `State.NoBattery` when `CurrentMode == DroneMode.Drain`.
  - **Virtual Floor (`0.001f`)**: While empty in Drain mode, a tiny floor (`0.001f`) is maintained in `ReversibleDroneController.Update()` so vanilla `SphereCheck()` and `TargetFindPlayer()` execute without early exit. Renders visually as 0 bars.

### 3. Targeting & Anti-Exploit
- **`ItemDrone_StateSearching_Patch`** (Prefix on `ItemDrone.StateSearching`):
  - In vanilla, `SphereCheck` requires `PhysGrabObjectCollider` on colliders, which enemies lack, making them impossible to detect.
  - When in `Drain` mode, intercepts `StateSearching` and delegates to `ReversibleDroneController.CustomStateSearching()`:
    - Scans a 3.5m radius for batteries, living monsters (`EnemyParent`), and living players (`PlayerAvatar`).
    - Excludes the player currently holding the drone so it can be held and aimed.
    - Connects to the closest valid target, establishing `magnetTarget` and a valid `rayHitPosition` with synchronized `colliderID` and `viewID` across all network clients (avoiding NRE in `NewRayHitPointLogic` and `DrawBeamLine`).
- **`ItemDrone_CheckTargetDeath_Patch`** (Prefix on `ItemDrone.CheckTargetDeath`):
  - Prevents vanilla `CheckTargetDeath` from prematurely turning off the drone if `enemyTarget.Spawned` is false in custom spawn setups.
- **Anti-Exploits**:
  - Items with `isUnchargable` cannot be drained.
  - Unpurchased shop items (`shopItem`) cannot be drained.

### 4. Transfer Execution & Visual Feedback
- **`ItemDroneBattery_Update_Patch`** (Prefix on `ItemDroneBattery.Update`):
  - When in `Drain` mode:
    - Checks `SemiFunc.IsMasterClientOrSingleplayer()`.
    - Applies `OverrideZeroGravity()`, `OverrideDrag(1f)`, `OverrideAngularDrag(10f)`.
    - When `itemDrone.magnetActive` is true, calls `controller.ExecuteDrain()`:
      - **Item Siphoning**: Direct conservative energy transfer utilizing native `itemBattery.ChargeBattery(target, rate)` and `targetBattery.Drain(rate)`.
      - **Dual HUD Visual Feedback**: Calls `targetBattery.OverrideBatteryShow(0.25f)` + `visualLogic.OverrideBatteryDrain(0.25f)` on the item and `itemBattery.OverrideBatteryShow(0.25f)` + `droneVisualLogic.OverrideBatteryCharge(0.25f)` on the drone so both HUDs and pulsing animations are simultaneously visible.
      - **Life Leeching**: Deals flat HP damage (`MonsterDamageFlatHpPerTick` / `PlayerDamageFlatHpPerTick`) and increments drone battery via `itemBattery.ChargeBattery()` on interval ticks.
      - Detaches via `itemDrone.MagnetActiveToggle(false)` once drone reaches full battery (`>= 99%`) or target has no health/battery left.

### 5. Monster Aggro & Chase Lifecycle
- **Attach Aggro**: When `FindDrainTarget()` locks onto an enemy target (`targetType == 2`), `NotifyEnemyOfPlayer(ep, isAttach: true)` is triggered, but only if the player is holding the drone or within `AlertEnemyDetectionRange` (default 6m) of the monster.
- **Detach Aggro**:
  - **Natural Full Battery Detach**: If the drone reaches 100% (`>= 99.5%`) battery, it detaches automatically. The `naturalFullChargeDetach` flag suppresses the alert so the enemy is NOT provoked.
  - **Distance Break Detach**: If the enemy moves away and breaks the tether (> 8m), `distanceBreakDetach` suppresses the alert.
  - **Player-Action Detach**: If a player manually grabs/pulls the drone off the monster or interacts with it (toggles off / switches mode) while within `AlertEnemyDetectionRange`, the monster detects the player and initiates direct chase.
- **Target Resolution**:
  - The responsible player is resolved with priority: (1) player currently holding/grabbing the drone, (2) drone owner who activated it (`droneOwner`) if within detection range, (3) nearest active, living player within detection range. If no player is within range, no alert is triggered.
- **Chase Initiation**: Calls native host-authoritative `Enemy.SetChaseTarget(targetPlayer)` which triggers vision detection, camera impact effects/stingers, and enters `EnemyState.ChaseBegin` / `EnemyState.Chase`.
- **Zero Impact on Vanilla Drones**: `ReversibleDroneController` and its hooks strictly operate on `ItemDroneBattery`. All other drones remain 100% vanilla.

---

## Key Design Decisions & Invariants

- **Zero Keybind Conflicts**: Operates entirely through the existing `InputKey.Interact` (`E`), preserving mod compatibility and player muscle memory.
- **Strict Energy Conservation**: No infinite battery loops, 1:1 battery transfer, shop items protected, and player stamina unaffected.
- **Visual Clarity**: Instant visual feedback via red emission map (`_EmissionColor`), red point light (`ItemLight`), and red laser beam (`LineBetweenTwoPoints`). Target items display floating battery HUD with drain animation.
- **Graceful Detachment**: The drone releases its target as soon as it is fully charged or the target is drained.
- **Standalone Build**: Compiles into a single self-contained DLL (`ReversibleBatteryDrone.dll`) deployed to both Steam and r2modman debug plugins.
