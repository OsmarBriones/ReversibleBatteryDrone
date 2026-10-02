# ReversibleBatteryDrone Architecture

This document describes the runtime structure, data flow, and design decisions for `ReversibleBatteryDrone`.

---

## High-Level Concept

1. **Trigger / Hook:** Intercepts `ItemToggle.Update` when the local player holds an `ItemDroneBattery` to drive a 3-state cycle (`Off` -> `Charge` -> `Drain` -> `Off`).
2. **Authority / Networking:** Input interception is local for responsiveness, while state is synchronized across the lobby via Photon RPC (`SetModeRPC`). Energy drain, battery charging, and life leeching execute host-only (`SemiFunc.IsMasterClientOrSingleplayer()`) for authoritative state mutations.
3. **Outcome:** The charge drone becomes bidirectional: charging items in yellow mode, and siphoning battery from items or leeching health from monsters/players into its battery in red mode.

---

## Main Data Flow

### 1. Initialization
- **`ItemDroneBattery_Start_Patch`** (Postfix on `ItemDroneBattery.Start`):
  - Automatically attaches the `ReversibleDroneController` MonoBehaviour component to the drone GameObject.
  - Caches original colors (yellow beam, yellow emission, light).

### 2. 3-State Sequential Interaction
- **`ItemToggle_Update_Patch`** (Prefix on `ItemToggle.Update`):
  - Checks if `physGrabObject.heldByLocalPlayer` and `SemiFunc.InputDown(InputKey.Interact)` (`E`).
  - Calls `ReversibleDroneController.HandleInteractPress()`:
    - **From Off (0) -> Charge (1)**: Calls `itemToggle.ToggleItem(true)`, sets mode to `Charge`, beam & LED yellow.
    - **From Charge (1) -> Drain (2)**: Keeps drone active, switches mode to `Drain`, beam & LED red.
    - **From Drain (2) -> Off (0)**: Calls `itemToggle.ToggleItem(false)`, resets mode to `Charge`.
  - Returns `false` to skip vanilla toggle logic.

### 3. Targeting
- **`ItemDroneBattery_CustomTargetingCondition_Patch`** (Prefix on `ItemDroneBattery.CustomTargetingCondition`):
  - When in `Drain` mode, replaces vanilla condition with `ReversibleDroneController.CustomTargetingCondition`:
    - Objects with `ItemBattery`: valid if target battery `> 0%` and drone battery `< 99%`.
    - Living Enemies: valid if `AllowMonsterDrain` is true, enemy not dead, and drone battery `< 99%`.
    - Living Players: valid if `AllowPlayerDrain` is true, player not dead, and drone battery `< 99%`.
  - In `Charge` mode, falls through to vanilla `SemiFunc.BatteryChargeCondition`.
- **Targeting Flags**: `ReversibleDroneController` dynamically sets `targetEnemies` and `targetPlayers` on `ItemDrone` during `Drain` mode, and clears them during `Charge` mode.

### 4. Transfer Execution
- **`ItemDroneBattery_Update_Patch`** (Prefix on `ItemDroneBattery.Update`):
  - When in `Drain` mode:
    - Checks `SemiFunc.IsMasterClientOrSingleplayer()`.
    - Applies `OverrideZeroGravity()`, `OverrideDrag(1f)`, `OverrideAngularDrag(10f)`.
    - When `itemDrone.magnetActive` is true, calls `controller.ExecuteDrain()`:
      - If attached to item with battery: calls `targetBattery.Drain(rate)` and `droneBattery.ChargeBattery(rate)` (`TargetBatteryDrainPercentPerSecond`).
      - If attached to enemy: deals configurable flat HP damage (`MonsterDamageFlatHpPerTick`) and grants battery percent (`DroneBatteryGainPercentPerTick`) every `LeechTickIntervalSeconds`.
      - If attached to player: deals configurable flat HP damage (`PlayerDamageFlatHpPerTick`) and grants battery percent (`DroneBatteryGainPercentPerTick`) every `LeechTickIntervalSeconds`.
      - Detaches via `itemDrone.MagnetActiveToggle(false)` once drone reaches full battery (`> 99%`) or target has no health/battery left.

---

## Key Design Decisions & Invariants

- **Zero Keybind Conflicts**: Operates entirely through the existing `InputKey.Interact` (`E`), preserving mod compatibility and player Muscle memory.
- **Visual Clarity**: Instant visual feedback via red emission map (`_EmissionColor`), red point light (`ItemLight`), and red laser beam (`LineBetweenTwoPoints`).
- **Graceful Detachment**: The drone releases its target as soon as it is fully charged (`> 99f`) or the target is drained, preventing wasted energy or endless latching.
- **Standalone Build**: Compiles into a single self-contained DLL (`ReversibleBatteryDrone.dll`) deployed to both Steam and r2modman debug plugins.
