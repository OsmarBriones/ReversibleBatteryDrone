# Changelog

## [1.0.2] - 2026-10-05
- **Documentation & Media**: Added gameplay showcase GIF to README demonstrating alternating Charge Mode and Drain Mode mechanics in action.

## [1.0.1] - 2026-10-05
- **Targeting Priority Hierarchy**: Implemented strict category priority (`Battery Items` > `Enemies` > `Players`) when searching for drain targets, ensuring the drone always siphons battery items first instead of latching onto nearby teammates or monsters.
- **Ray Attachment & Transform Binding Fix**: Fixed `NewRayHitPoint` binding to pass the root `PhysGrabObject` transform rather than child colliders, preventing `NullReferenceException` and ensuring reliable beam tethering in singleplayer and multiplayer.
- **Nested Component & Self-Collision Support**: Added recursive child search for `ItemBattery` and excluded the drone's own child colliders from target evaluation.
- **Default Siphoning Rate Adjusted**: Changed default `TargetBatteryDrainPercentPerSecond` from 25% to 5% per second to perfectly match the vanilla game's native recharge rate (20 seconds for full charge/drain).

## [1.0.0] - 2026-10-03
- **Initial Release!**
- **Reversible Energy Transfer**: Alternates between **Charge Mode** (powering items with a yellow beam) and **Drain Mode** (siphoning energy back into the drone with a red beam).
- **Alternating Activation Lifecycle**: Toggle cleanly between Charge -> Off -> Drain -> Off using the standard interact key (`E`).
- **Smart Battery Overrides**: Automatically forces Drain mode when completely discharged (0% battery) and Charge mode when fully charged (100% battery).
- **Life Leeching**: Siphons health from monsters and players in Drain mode, converting biological life into drone battery charge.
- **Monster Aggression & Chase**: Attaching or detaching the drone near enemies triggers realistic aggression and pursuit mechanics.
- **Dynamic In-Hand HUD Prompts**: Displays `"Recharge Droid [E]"` or `"Drain Droid [E]"` based on the active or upcoming mode.
- **Asymmetric Multiplayer (Host-Only Gameplay)**: Only the host needs to install the mod for 100% working gameplay across vanilla clients, while modded clients enjoy enhanced red laser beams and LED visuals.
- **Full Configuration**: Comprehensive settings in `BepInEx/config/com.osmar.ReversibleBatteryDrone.cfg`.
