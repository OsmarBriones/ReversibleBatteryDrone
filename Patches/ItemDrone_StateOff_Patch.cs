using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDrone), "StateOff")]
internal static class ItemDrone_StateOff_Patch
{
	private static void Postfix(ItemDrone __instance)
	{
		if (ConfigurationController.Enabled?.Value == false) return;
		if (__instance == null || !SemiFunc.IsMasterClientOrSingleplayer()) return;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller != null && controller.CurrentMode == DroneMode.Drain)
		{
			var toggle = __instance.GetComponent<ItemToggle>();
			if (toggle != null && toggle.toggleState && __instance.currentState == ItemDrone.State.Off)
			{
				__instance.StateSet(ItemDrone.State.Searching);
			}
		}
	}
}
