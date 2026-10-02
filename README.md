# ReversibleBatteryDrone

Makes the charge drone reversible: 1st tap charges items, 2nd tap turns red to drain items, monsters, or players into battery life, 3rd tap shuts off. Fully configurable. Only Host, clients don't need it.

## Features

- **3-State Sequential Mode Cycling**: Use your standard interaction key (`E` by default) while holding the charge drone to easily cycle through states without extra keybinds:
  - **1st Tap**: Turns ON in **Charge Mode** (vanilla yellow beam & LED), charging low-battery items.
  - **2nd Tap**: Switches to **Drain Mode** (vibrant red beam & LED), reversing energy flow.
  - **3rd Tap**: Shuts the drone OFF completely.
- **Battery Siphoning**: In Drain mode, the drone attaches to items with remaining energy and siphons their power directly into its own battery.
- **Life Leeching**: If deployed near monsters or other players in Drain mode, the drone latches onto them, leeching small amounts of health over time and converting it into battery charge.
- **Host-Only Multiplayer**: Only the host needs to install the mod; health and battery transfer mechanics execute on the host and automatically sync across all connected clients.
- **Fully Configurable**: Easily tweak battery drain rates, monster/player damage per tick, health-to-battery conversion gain, and PvP friendly-fire toggles.

## Requirements
- [BepInEx Pack for R.E.P.O.](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)

## Installation
1. Install the latest [BepInEx Pack](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/).
2. Place `ReversibleBatteryDrone.dll` into your `BepInEx/plugins` folder (or install via r2modman / Thunderstore Mod Manager).
3. Launch the game once — the configuration file will be generated automatically inside `BepInEx/config`.

## Configuration
All settings are controlled through `com.osmar.ReversibleBatteryDrone.cfg` located in `BepInEx/config`:

- `DrainRateBattery` (default: `5.0`): Battery charge drained per second when siphoning items.
- `DrainDamageMonsters` (default: `2`): Damage dealt per tick to monsters when leeching their life.
- `DrainDamagePlayers` (default: `1`): Damage dealt per tick to players when leeching their life.
- `BatteryGainFromHealth` (default: `3.0`): Battery percentage gained by the drone per tick when siphoning health.
- `AllowMonsterDrain` (default: `true`): Whether the drone can latch onto monsters in Drain mode.
- `AllowPlayerDrain` (default: `true`): Whether the drone can latch onto teammates/players in Drain mode.
- `DrainTickRate` (default: `0.5`): Seconds between health drain damage ticks on living targets.

## Issues & Bug Reports
The official way to report issues, suggest improvements, or submit feedback is by opening an issue on the official GitHub repository:
👉 [GitHub Issues](https://github.com/OsmarBriones/ReversibleBatteryDrone/issues)

## Credits
Developed by **Osmar Briones**
