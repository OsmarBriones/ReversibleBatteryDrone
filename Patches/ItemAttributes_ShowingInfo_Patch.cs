using HarmonyLib;

namespace ReversibleBatteryDrone.Patches;

[HarmonyPatch(typeof(ItemAttributes), nameof(ItemAttributes.ShowingInfo))]
internal static class ItemAttributes_ShowingInfo_Patch
{
	private static void Postfix(ItemAttributes __instance, ref string ___promptName)
	{
		if (ConfigurationController.Enabled?.Value == false) return;
		if (__instance == null || string.IsNullOrEmpty(___promptName)) return;

		var controller = __instance.GetComponent<ReversibleDroneController>();
		if (controller == null) return;

		var mode = controller.GetEffectiveModeForDisplay();
		if (mode == DroneMode.Drain)
		{
			if (___promptName.Contains("Recharge Droid"))
			{
				___promptName = ___promptName.Replace("Recharge Droid", "Drain Droid");
			}
			else if (___promptName.Contains("Dron de recarga"))
			{
				___promptName = ___promptName.Replace("Dron de recarga", "Dron de drenado");
			}
			else if (___promptName.Contains("Recharge"))
			{
				___promptName = ___promptName.Replace("Recharge", "Drain");
			}
			else if (___promptName.Contains("recarga"))
			{
				___promptName = ___promptName.Replace("recarga", "drenado");
			}
		}
	}
}
