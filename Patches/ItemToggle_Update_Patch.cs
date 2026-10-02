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
		if (controller == null) return true;

		var physGrab = __instance.GetComponent<PhysGrabObject>();
		if (physGrab == null || !physGrab.heldByLocalPlayer) return true;

		if (PlayerController.instance != null && PlayerController.instance.InputDisableTimer <= 0f && SemiFunc.InputDown(InputKey.Interact))
		{
			if (TutorialDirector.instance != null)
			{
				TutorialDirector.instance.playerUsedToggle = true;
			}
			controller.HandleInteractPress();
			return false;
		}

		return true;
	}
}
