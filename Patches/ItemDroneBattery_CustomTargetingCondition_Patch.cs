using HarmonyLib;
using UnityEngine;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDroneBattery), nameof(ItemDroneBattery.CustomTargetingCondition))]
internal static class ItemDroneBattery_CustomTargetingCondition_Patch
{
	private static bool Prefix(ItemDroneBattery __instance, GameObject target, ref bool __result)
	{
		if (ConfigurationController.Enabled?.Value == false) return true;
		if (__instance == null || target == null) return true;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller != null && controller.CurrentMode == DroneMode.Drain)
		{
			__result = controller.CustomTargetingCondition(target);
			return false;
		}

		return true;
	}
}
