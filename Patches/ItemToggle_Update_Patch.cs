using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemToggle), "Update")]
internal static class ItemToggle_Update_Patch
{
	private static bool Prefix(ItemToggle __instance)
	{
		if (ConfigurationController.Enabled?.Value == false) return true;
		if (__instance == null || __instance.disabled) return true;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller == null)
		{
			if (__instance.GetComponent<ItemDroneBattery>() != null || __instance.GetComponent<ItemDrone>() != null)
			{
				controller = __instance.gameObject.AddComponent<ReversibleDroneController>();
				var pv = __instance.GetComponent<Photon.Pun.PhotonView>();
				pv?.RefreshRpcMonoBehaviourCache();
				ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] Controller dynamically attached in ItemToggle_Update. ViewID: {pv?.ViewID}");
			}
			else
			{
				return true;
			}
		}

		var physGrab = __instance.GetComponent<PhysGrabObject>();
		if (physGrab == null || !physGrab.heldByLocalPlayer) return true;

		if (PlayerController.instance != null && PlayerController.instance.InputDisableTimer <= 0f && SemiFunc.InputDown(InputKey.Interact))
		{
			if (TutorialDirector.instance != null)
			{
				TutorialDirector.instance.playerUsedToggle = true;
			}
			var pv = controller.GetComponent<Photon.Pun.PhotonView>();
			ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] [Input] Local player pressed E on ViewID: {pv?.ViewID}. CurrentMode: {controller.CurrentMode}, ToggleState: {__instance.toggleState}");
			controller.HandleInteractPress();
			return false;
		}

		return true;
	}
}
