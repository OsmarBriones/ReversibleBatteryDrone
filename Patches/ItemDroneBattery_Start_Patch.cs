using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemDroneBattery), "Start")]
internal static class ItemDroneBattery_Start_Patch
{
	private static void Postfix(ItemDroneBattery __instance)
	{
		if (__instance == null) return;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller == null)
		{
			__instance.gameObject.AddComponent<ReversibleDroneController>();
		}

		var pv = __instance.GetComponent<Photon.Pun.PhotonView>();
		pv?.RefreshRpcMonoBehaviourCache();
	}
}
