using BepInEx.Configuration;

namespace ReversibleBatteryDrone;

internal static class ConfigurationController
{
	private static ConfigFile? ConfigFile { get; set; }

	internal static ConfigEntry<bool>? Enabled { get; private set; }

	// Item Siphoning
	internal static ConfigEntry<float>? TargetBatteryDrainPercentPerSecond { get; private set; }

	// Life Leeching
	internal static ConfigEntry<int>? MonsterDamageFlatHpPerTick { get; private set; }
	internal static ConfigEntry<int>? PlayerDamageFlatHpPerTick { get; private set; }
	internal static ConfigEntry<float>? DroneBatteryGainPercentPerTick { get; private set; }
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
			20.0f,
			"Percentage of battery (0% to 100%) drained from the targeted item per second and transferred directly into the drone's battery. At 20%, an item with 100% battery drains completely in 5 seconds."
		);

		// Life Leeching
		MonsterDamageFlatHpPerTick = ConfigFile.Bind(
			"LifeLeech",
			nameof(MonsterDamageFlatHpPerTick),
			2,
			"Flat health points (HP) of damage dealt to the targeted monster on every tick."
		);

		PlayerDamageFlatHpPerTick = ConfigFile.Bind(
			"LifeLeech",
			nameof(PlayerDamageFlatHpPerTick),
			1,
			"Flat health points (HP) of damage dealt to the targeted player/teammate on every tick."
		);

		DroneBatteryGainPercentPerTick = ConfigFile.Bind(
			"LifeLeech",
			nameof(DroneBatteryGainPercentPerTick),
			3.0f,
			"Percentage of battery charge (0% to 100%) added to the drone on every tick while leeching life from a monster or player."
		);

		LeechTickIntervalSeconds = ConfigFile.Bind(
			"LifeLeech",
			nameof(LeechTickIntervalSeconds),
			0.5f,
			"Time in seconds between each damage and battery gain tick when leeching life from a living target."
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
