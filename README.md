# ReversibleBatteryDrone

Makes the charge drone reversible: each activation alternates between Charge Mode and Drain Mode (draining items, monsters, or players into battery life). Fully configurable. Truly Host-Only: clients do not need the mod installed.

## Features

- **Alternating Activation Mode**: Use your standard interaction key (`E` by default) while holding the charge drone to toggle it ON/OFF. Each activation cleanly alternates modes:
  - **1st Activation**: Turns ON in **Charge Mode** (vanilla yellow beam & LED), charging low-battery items. Turn OFF when done.
  - **2nd Activation**: Turns ON in **Drain Mode** (vibrant red beam & LED for modded clients), reversing energy flow to siphon batteries and leech monster/player health. Turn OFF when done.
  - **Smart Battery Overrides**:
    - **When Completely Empty (0% Battery)**: Automatically skips the unusable charge mode and activates directly in **Drain Mode** (Red), allowing you to immediately recharge the empty drone!
    - **When Full (100% Battery)**: Automatically skips drain mode and activates directly in **Charge Mode**.
- **Battery Siphoning & HUD Feedback**: In Drain mode, the drone attaches to items with remaining energy and siphons their power directly into its own battery. The target's floating battery HUD is visible during draining with visual feedback.
- **Life Leeching**: If deployed near monsters or other players in Drain mode, the drone latches onto them, leeching small amounts of health over time and converting it into battery charge.
- **Monster Aggression & Chase Alerts**: Attaching the drone to an enemy or detaching/removing it from them alerts the monster and triggers a direct chase against the responsible player (enabled by default, customizable).
- **Anti-Exploit & Energy Conservation**:
  - Direct 1:1 energy transfer prevents infinite battery loops.
  - Items in the shop (`shopItem`) cannot be siphoned.
  - Non-rechargeable items (`isUnchargable`) cannot be siphoned.
  - Player stamina/energy is completely unaffected.
- **100% Host-Only Multiplayer**: Only the host needs to install the mod! Drone AI, flight physics, battery transfers, and monster damage execute authoritatively on the host and synchronize across all connected clients. Clients with the mod installed also receive full visual effects (vibrant red beam & LEDs).
- **Fully Configurable**: Easily tweak drain rates, monster/player damage per tick, health-to-battery conversion gain, and PvP friendly-fire toggles.

## Requirements
- [BepInEx Pack for R.E.P.O.](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)

## Installation
1. Install the latest [BepInEx Pack](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/).
2. Place `ReversibleBatteryDrone.dll` into your `BepInEx/plugins` folder (or install via r2modman / Thunderstore Mod Manager).
3. Launch the game once — the configuration file will be generated automatically inside `BepInEx/config`.

## Configuration
All settings are controlled through `com.osmar.ReversibleBatteryDrone.cfg` located in `BepInEx/config`:

### `[ItemSiphoning]`
- `TargetBatteryDrainPercentPerSecond` (default: `25`): Percentage of battery (`1%` to `100%`, integer) drained from the targeted item per second and transferred directly into the drone's battery. At 25%/s, an item with 100% battery drains in 4 seconds. Can be set up to 100% for instant drain.

### `[LifeLeech]`
- `MonsterDamageFlatHpPerTick` (default: `1`): Flat hit points (HP) of damage (`1` to `100`) dealt to the targeted monster on each tick.
- `PlayerDamageFlatHpPerTick` (default: `10`): Flat hit points (HP) of damage (`1` to `100`) dealt to the targeted teammate/player on each tick.
- `DroneBatteryGainPercentPerTick` (default: `1`): Percentage of battery charge (`1%` to `100%`, integer) added to the drone on each tick while leeching life from a monster or player.
- `LeechTickIntervalSeconds` (default: `0.5`): Time in seconds between each damage and battery gain tick when leeching life from a living target (`0.1s` to `5.0s`).
- `AllowTargetingMonsters` (default: `true`): Whether the drone in Drain mode is allowed to latch onto monsters and leech their health.
- `AllowTargetingPlayers` (default: `true`): Whether the drone in Drain mode is allowed to latch onto other players and leech their health (PvP friendly-fire toggle).
- `AlertEnemyOnAttach` (default: `true`): Whether attaching the drone in Drain mode to an enemy alerts them and initiates a direct chase toward the player.
- `AlertEnemyOnDetach` (default: `true`): Whether detaching the drone in Drain mode from an enemy alerts them and initiates a direct chase toward the player (only applies if a player actively grabbed/interacted with it near the monster).
- `AlertEnemyDetectionRange` (default: `6`): Maximum distance in meters (`1m` to `20m`, integer) between the player and the enemy for the enemy to detect and chase the player on attach/detach. If the player is outside this range, or if the drone naturally detaches on full charge or distance break, the enemy will not be alerted.

## Issues & Bug Reports
The official way to report issues, suggest improvements, or submit feedback is by opening an issue on the official GitHub repository:
👉 [GitHub Issues](https://github.com/OsmarBriones/ReversibleBatteryDrone/issues)

## Credits
Developed by **Osmar Briones**
