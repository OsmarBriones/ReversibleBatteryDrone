using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDrone), "CheckTargetDeath")]
internal static class ItemDrone_CheckTargetDeath_Patch
{
	private static bool Prefix(ItemDrone __instance)
	{
		if (ConfigurationController.Enabled?.Value == false) return true;
		if (__instance == null) return true;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller != null && controller.CurrentMode == DroneMode.Drain)
		{
			return false; // Skip vanilla target death check so it doesn't prematurely force turn off
		}

		return true;
	}
}
