using BepInEx.Configuration;

namespace ReversibleBatteryDrone;

internal static class ConfigurationController
{
	private static ConfigFile? ConfigFile { get; set; }

	internal static ConfigEntry<bool>? Enabled { get; private set; }
	internal static ConfigEntry<float>? DrainRateBattery { get; private set; }
	internal static ConfigEntry<int>? DrainDamageMonsters { get; private set; }
	internal static ConfigEntry<int>? DrainDamagePlayers { get; private set; }
	internal static ConfigEntry<float>? BatteryGainFromHealth { get; private set; }
	internal static ConfigEntry<bool>? AllowMonsterDrain { get; private set; }
	internal static ConfigEntry<bool>? AllowPlayerDrain { get; private set; }
	internal static ConfigEntry<float>? DrainTickRate { get; private set; }

	internal static void Initialize(ConfigFile config)
	{
		ConfigFile = config;

		Enabled = ConfigFile.Bind("General", nameof(Enabled), true, "Enable or disable this mod.");

		DrainRateBattery = ConfigFile.Bind("Drain", nameof(DrainRateBattery), 5.0f, "Battery charge drained per second when siphoning items.");

		DrainDamageMonsters = ConfigFile.Bind("Drain", nameof(DrainDamageMonsters), 2, "Damage per tick dealt to monsters when draining their life.");

		DrainDamagePlayers = ConfigFile.Bind("Drain", nameof(DrainDamagePlayers), 1, "Damage per tick dealt to players when draining their life.");

		BatteryGainFromHealth = ConfigFile.Bind("Drain", nameof(BatteryGainFromHealth), 3.0f, "Battery gained by the drone per tick when siphoning health from monsters or players.");

		AllowMonsterDrain = ConfigFile.Bind("Drain", nameof(AllowMonsterDrain), true, "Whether the drone can latch onto monsters in Drain mode.");

		AllowPlayerDrain = ConfigFile.Bind("Drain", nameof(AllowPlayerDrain), true, "Whether the drone can latch onto players in Drain mode.");

		DrainTickRate = ConfigFile.Bind("Drain", nameof(DrainTickRate), 0.5f, "Seconds between health drain damage ticks on living targets.");

		ConfigFile.Save();
	}

	internal static void Reload()
	{
		ConfigFile?.Reload();
		ConfigFile?.Save();
	}
}
