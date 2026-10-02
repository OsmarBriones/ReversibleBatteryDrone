using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDrone), "StateSearching")]
internal static class ItemDrone_StateSearching_Patch
{
	private static bool Prefix(ItemDrone __instance)
	{
		if (ConfigurationController.Enabled?.Value == false) return true;
		if (__instance == null) return true;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller != null && controller.CurrentMode == DroneMode.Drain)
		{
			controller.CustomStateSearching();
			return false;
		}

		return true;
	}
}
