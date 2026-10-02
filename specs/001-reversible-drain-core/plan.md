# Implementation Plan: Reversible Battery Drone (001-reversible-drain-core)

**Branch**: `001-reversible-drain-core` | **Date**: 2026-10-02 | **Spec**: specs/001-reversible-drain-core/spec.md

## Summary

Adds bidirectional functionality to the vanilla charge drone (`ItemDroneBattery`) in R.E.P.O. using a sequential 3-state cycle on the standard `Interact` key (`E`):
- **State 0 (Off)**: Drone is folded and inactive.
- **State 1 (Charge - Yellow)**: 1st tap turns drone ON in vanilla mode, charging low-battery items.
- **State 2 (Drain/Absorb - Red)**: 2nd tap changes beam and LED emission color to RED. In this mode, the drone absorbs energy from any item with a battery. Furthermore, if connected to monsters or other players, it siphons health (small configurable damage per tick) and converts it into drone battery charge!
- **State 3 (Off)**: 3rd tap shuts off the drone.

## Technical Context

**Language/Version**: C# 12 / `.NET Framework 4.8` (`net48`)
**Primary Dependencies**:
- BepInEx 5.4.x / HarmonyX 2.12.x
- Unity 2022.3.x assemblies (`UnityEngine.CoreModule`, `UnityEngine.PhysicsModule`)
- `Assembly-CSharp.dll` (Game binary)
- `RepoAPI` (`external/RepoAPI` submodule, modular `<Compile Include>`)
**Target Platform**: Windows 64-bit / R.E.P.O.
**Project Type**: BepInEx Game Mod (`.dll`)
**Performance Goals**: Zero runtime GC allocations in per-frame/Update loops; lightweight lifecycle hooks.
**Constraints**:
- Host-only spawn authority (`SemiFunc.IsMasterClientOrSingleplayer()`) for health and battery state mutations.
- Strict modern C# standard (zero Hungarian / underscore / `s_` prefixes).
- Zero compiler warnings (`TreatWarningsAsErrors = true`).
- Scoping: `internal` by default, only `BaseUnityPlugin` entry point is `public`.

## Constitution Check

- [x] **I. RepoKit Alignment**: Complies with `REPO_MODS_WORKSPACE.md` and `REPO_MODS_METHODOLOGY.md`.
- [x] **II. Host-Only Authority**: Uses `SemiFunc.RunIsLevel()` and `SemiFunc.IsMasterClientOrSingleplayer()` for game-state mutations.
- [x] **III. Modern C# & Zero Legacy Prefixes**: No leading underscores, clean naming.
- [x] **IV. Shared Code Hygiene (RepoAPI)**: Modular integration via `Directory.Build.props` and submodule.
- [x] **V. 3-Tier Testing**: Tier 3 in-game smoke testing via Steam and r2modman Debug profile.

## Architecture & Data Flow

```
Player presses 'Interact' (E) while holding drone
  └─ ItemToggle_Update_Patch (Prefix on ItemToggle.Update)
       ├─ If State == Off: Turn ON, SetMode(Charge), beam/LED = Yellow
       ├─ If State == Charge: Keep ON, SetMode(Drain), beam/LED = Red
       └─ If State == Drain: Turn OFF, Reset to Off

Drone Targeting
  └─ ItemDrone.StateSearching
       ├─ If Charge: targetValuables = true, targetEnemies = false, targetPlayers = false
       └─ If Drain: targetValuables = true, targetEnemies = true, targetPlayers = config.AllowPlayerDrain

Target Validation
  └─ ItemDroneBattery_CustomTargetingCondition_Patch
       ├─ If Charge: vanilla BatteryChargeCondition (target battery < 100%)
       └─ If Drain: valid if (battery > 0% OR living enemy OR living player) AND drone battery < 100%

Energy & Life Transfer
  └─ ItemDroneBattery_Update_Patch
       ├─ If Charge: vanilla charge transfer
       └─ If Drain:
            ├─ Item with Battery: Drain target battery, charge drone battery
            ├─ Enemy: Hurt enemy (config damage), charge drone battery (config gain)
            └─ Player: Hurt player (config damage), charge drone battery (config gain)
```

## Harmony Hooks

1. `ItemDroneBattery.Start`: Attach `ReversibleDroneController` component.
2. `ItemToggle.Update`: Intercept `SemiFunc.InputDown(InputKey.Interact)` for 3-state cycle when held.
3. `ItemDroneBattery.CustomTargetingCondition`: Intercept targeting condition when in Drain mode.
4. `ItemDroneBattery.Update`: Intercept update logic to execute draining/leeching when in Drain mode.
5. `ItemDrone.StateSearching`: Adjust targeting flags (`targetEnemies`, `targetPlayers`) based on drone mode.
