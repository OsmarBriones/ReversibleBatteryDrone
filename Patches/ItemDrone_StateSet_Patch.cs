using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDrone), nameof(ItemDrone.StateSet))]
internal static class ItemDrone_StateSet_Patch
{
	private static bool Prefix(ItemDrone __instance, ItemDrone.State newState)
	{
		if (ConfigurationController.Enabled?.Value == false) return true;
		if (__instance == null) return true;

		// While in Drain mode, prevent the drone from collapsing into NoBattery state so it can absorb energy
		if (newState == ItemDrone.State.NoBattery)
		{
			var controller = __instance.GetComponent<ReversibleDroneController>();
			if (controller != null && controller.CurrentMode == DroneMode.Drain)
			{
				return false;
			}
		}

		return true;
	}
}
