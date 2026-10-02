# Tasks: Reversible Battery Drone (001-reversible-drain-core)

## Phase 1: Setup & Configuration

- [x] **T001**: Configure BepInEx configuration bindings in `ConfigurationController.cs` (`DrainRateBattery`, `DrainDamageMonsters`, `DrainDamagePlayers`, `BatteryGainFromHealth`, `AllowPlayerDrain`, `AllowMonsterDrain`, `TickInterval`).

## Phase 2: Core Domain Logic

- [x] **T002**: Create `DroneMode.cs` enum defining `Charge` and `Drain` modes.
- [x] **T003**: Create `ReversibleDroneController.cs` MonoBehaviour managing 3-state tracking, visual styling (red beam, light, and emission), target condition evaluation, and life leech / battery drain logic.

## Phase 3: Harmony Patches

- [x] **T004**: Implement `ItemDroneBattery_Start_Patch.cs` to attach `ReversibleDroneController` to the battery drone GameObject.
- [x] **T005**: Implement `ItemToggle_Update_Patch.cs` to intercept `InputKey.Interact` when held by local player to drive the 3-state cycle (Off -> Charge -> Drain -> Off).
- [x] **T006**: Implement `ItemDroneBattery_CustomTargetingCondition_Patch.cs` to allow targeting items, monsters, and players in Drain mode.
- [x] **T007**: Implement `ItemDroneBattery_Update_Patch.cs` to execute draining and life leeching on the attached target.
- [x] **T008**: Implement dynamic targeting flags (`targetEnemies`, `targetPlayers`) in `ReversibleDroneController` when in Drain mode.

## Phase 4: Quality Gate & Documentation

- [x] **T009**: Update `ARCHITECTURE.md` and `README.md` with features and configuration instructions.
- [x] **T010**: Verify build with `dotnet build` with zero warnings and errors.
