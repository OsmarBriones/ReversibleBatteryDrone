using BepInEx.Configuration;

namespace ReversibleBatteryDrone;

internal static class ConfigurationController
{
	private static ConfigFile? ConfigFile { get; set; }

	internal static ConfigEntry<bool>? Enabled { get; private set; }

	// Item Siphoning
	internal static ConfigEntry<int>? TargetBatteryDrainPercentPerSecond { get; private set; }

	// Life Leeching
	internal static ConfigEntry<int>? MonsterDamageFlatHpPerTick { get; private set; }
	internal static ConfigEntry<int>? PlayerDamageFlatHpPerTick { get; private set; }
	internal static ConfigEntry<int>? DroneBatteryGainPercentPerTick { get; private set; }
	internal static ConfigEntry<float>? LeechTickIntervalSeconds { get; private set; }
	internal static ConfigEntry<bool>? AllowTargetingMonsters { get; private set; }
	internal static ConfigEntry<bool>? AllowTargetingPlayers { get; private set; }

	internal static void Initialize(ConfigFile config)
	{
		ConfigFile = config;

		Enabled = ConfigFile.Bind(
			"General",
			nameof(Enabled),
			true,
			"Enable or disable the ReversibleBatteryDrone mod entirely."
		);

		// Item Siphoning
		TargetBatteryDrainPercentPerSecond = ConfigFile.Bind(
			"ItemSiphoning",
			nameof(TargetBatteryDrainPercentPerSecond),
			25,
			new ConfigDescription(
				"Percentage of battery (1% to 100%) drained from the targeted item per second and transferred directly into the drone's battery. At 25%, an item with 100% battery drains in 4 seconds. Can be set up to 100% for instant 1-second drain.",
				new AcceptableValueRange<int>(1, 100)
			)
		);

		// Life Leeching
		MonsterDamageFlatHpPerTick = ConfigFile.Bind(
			"LifeLeech",
			nameof(MonsterDamageFlatHpPerTick),
			2,
			new ConfigDescription(
				"Flat health points (HP) of damage dealt to the targeted monster on every tick (1 to 100 HP).",
				new AcceptableValueRange<int>(1, 100)
			)
		);

		PlayerDamageFlatHpPerTick = ConfigFile.Bind(
			"LifeLeech",
			nameof(PlayerDamageFlatHpPerTick),
			1,
			new ConfigDescription(
				"Flat health points (HP) of damage dealt to the targeted player/teammate on every tick (1 to 100 HP).",
				new AcceptableValueRange<int>(1, 100)
			)
		);

		DroneBatteryGainPercentPerTick = ConfigFile.Bind(
			"LifeLeech",
			nameof(DroneBatteryGainPercentPerTick),
			3,
			new ConfigDescription(
				"Percentage of battery charge (1% to 100%) added to the drone on every tick while leeching life from a monster or player.",
				new AcceptableValueRange<int>(1, 100)
			)
		);

		LeechTickIntervalSeconds = ConfigFile.Bind(
			"LifeLeech",
			nameof(LeechTickIntervalSeconds),
			0.5f,
			new ConfigDescription(
				"Time in seconds between each damage and battery gain tick when leeching life from a living target (0.1s to 5.0s).",
				new AcceptableValueRange<float>(0.1f, 5.0f)
			)
		);

		AllowTargetingMonsters = ConfigFile.Bind(
			"LifeLeech",
			nameof(AllowTargetingMonsters),
			true,
			"Whether the drone in Drain mode is allowed to latch onto monsters and leech their health."
		);

		AllowTargetingPlayers = ConfigFile.Bind(
			"LifeLeech",
			nameof(AllowTargetingPlayers),
			true,
			"Whether the drone in Drain mode is allowed to latch onto other players and leech their health (PvP friendly-fire toggle)."
		);

		ConfigFile.Save();
	}

	internal static void Reload()
	{
		ConfigFile?.Reload();
		ConfigFile?.Save();
	}
}
