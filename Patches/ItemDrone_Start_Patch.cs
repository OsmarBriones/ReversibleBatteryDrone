using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDrone), "Start")]
internal static class ItemDrone_Start_Patch
{
	private static void Postfix(ItemDrone __instance)
	{
		if (__instance == null) return;
		if (__instance.GetComponent<ItemDroneBattery>() == null) return;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller == null)
		{
			controller = __instance.gameObject.AddComponent<ReversibleDroneController>();
		}

		var pv = __instance.GetComponent<Photon.Pun.PhotonView>();
		pv?.RefreshRpcMonoBehaviourCache();
		ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] ItemDrone.Start patched on GameObject {__instance.gameObject.name}, ViewID: {pv?.ViewID}");
	}
}
