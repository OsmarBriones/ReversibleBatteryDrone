using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDroneBattery), "Update")]
internal static class ItemDroneBattery_Update_Patch
{
	private static bool Prefix(ItemDroneBattery __instance)
	{
		if (ConfigurationController.Enabled?.Value == false) return true;
		if (__instance == null) return true;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller == null) return true;

		if (controller.CurrentMode == DroneMode.Drain)
		{
			var itemEquippable = __instance.GetComponent<ItemEquippable>();
			var itemDrone = __instance.GetComponent<ItemDrone>();
			var physGrab = __instance.GetComponent<PhysGrabObject>();

			if (itemEquippable != null && itemEquippable.isEquipped) return false;
			if (!SemiFunc.IsMasterClientOrSingleplayer()) return false;
			if (itemDrone == null || !itemDrone.itemActivated) return false;

			if (physGrab != null)
			{
				physGrab.OverrideZeroGravity();
				physGrab.OverrideDrag(1f);
				physGrab.OverrideAngularDrag(10f);
			}

			if (itemDrone.magnetActive)
			{
				controller.ExecuteDrain();
			}

			return false; // Skip vanilla charge logic
		}

		return true; // Run vanilla charge logic
	}
}
