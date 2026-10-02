using Photon.Pun;
using UnityEngine;

namespace ReversibleBatteryDrone;

internal class ReversibleDroneController : MonoBehaviour
{
	private ItemDrone itemDrone = null!;
	private ItemDroneBattery itemDroneBattery = null!;
	private ItemBattery itemBattery = null!;
	private ItemToggle itemToggle = null!;
	private PhysGrabObject physGrabObject = null!;
	private PhotonView photonView = null!;

	public DroneMode CurrentMode { get; private set; } = DroneMode.Charge;
	public int DroneCycleState { get; private set; } = 0; // 0 = Off, 1 = Charge, 2 = Drain

	private Color originalDroneColor;
	private Color originalBeamColor;
	private bool colorsCached;

	private float tickTimer;

	private void Awake()
	{
		itemDrone = GetComponent<ItemDrone>();
		itemDroneBattery = GetComponent<ItemDroneBattery>();
		itemBattery = GetComponent<ItemBattery>();
		itemToggle = GetComponent<ItemToggle>();
		physGrabObject = GetComponent<PhysGrabObject>();
		photonView = GetComponent<PhotonView>();
	}

	private void Start()
	{
		CacheOriginalColors();
		UpdateVisuals();
	}

	private void CacheOriginalColors()
	{
		if (colorsCached) return;
		if (itemDrone != null)
		{
			originalDroneColor = itemDrone.droneColor;
			originalBeamColor = itemDrone.beamColor;
			if (originalDroneColor != default)
			{
				colorsCached = true;
			}
		}
	}

	private void Update()
	{
		if (!colorsCached)
		{
			CacheOriginalColors();
		}

		// If drone was turned off externally or ran out of battery, reset state
		if (itemToggle != null && !itemToggle.toggleState && DroneCycleState != 0)
		{
			DroneCycleState = 0;
			CurrentMode = DroneMode.Charge;
			UpdateVisuals();
		}

		// Ensure targeting flags match current mode during search
		if (itemDrone != null && itemDrone.itemActivated)
		{
			if (CurrentMode == DroneMode.Drain)
			{
				itemDrone.targetValuables = true;
				itemDrone.targetNonValuables = true;
				itemDrone.targetEnemies = ConfigurationController.AllowTargetingMonsters?.Value ?? true;
				itemDrone.targetPlayers = ConfigurationController.AllowTargetingPlayers?.Value ?? true;
			}
			else
			{
				itemDrone.targetEnemies = false;
				itemDrone.targetPlayers = false;
			}
		}
	}

	public void HandleInteractPress()
	{
		// 3-state cycle:
		// State 0 (Off) -> State 1 (Charge)
		// State 1 (Charge) -> State 2 (Drain)
		// State 2 (Drain) -> State 0 (Off)
		int player = SemiFunc.PhotonViewIDPlayerAvatarLocal();

		if (DroneCycleState == 0)
		{
			// 1st tap: Turn ON in Charge mode (Yellow)
			DroneCycleState = 1;
			SetMode(DroneMode.Charge);
			itemToggle.ToggleItem(true, player);
		}
		else if (DroneCycleState == 1)
		{
			// 2nd tap: Switch to Drain mode (Red)
			DroneCycleState = 2;
			SetMode(DroneMode.Drain);
			PlayModeSwitchSound();
		}
		else
		{
			// 3rd tap: Turn OFF
			DroneCycleState = 0;
			SetMode(DroneMode.Charge); // reset for next activation
			itemToggle.ToggleItem(false, player);
		}
	}

	public void SetMode(DroneMode mode)
	{
		if (GameManager.Multiplayer() && photonView != null && photonView.ViewID != 0)
		{
			photonView.RPC(nameof(SetModeRPC), RpcTarget.All, (int)mode);
		}
		else
		{
			SetModeRPC((int)mode);
		}
	}

	[PunRPC]
	public void SetModeRPC(int mode)
	{
		CurrentMode = (DroneMode)mode;
		if (CurrentMode == DroneMode.Drain)
		{
			DroneCycleState = 2;
		}
		else if (itemToggle != null && itemToggle.toggleState)
		{
			DroneCycleState = 1;
		}
		else
		{
			DroneCycleState = 0;
		}
		UpdateVisuals();
	}

	public void UpdateVisuals()
	{
		if (itemDrone == null) return;

		Color activeColor = CurrentMode == DroneMode.Drain ? Color.red : (originalDroneColor != default ? originalDroneColor : itemDrone.droneColor);
		Color activeBeam = CurrentMode == DroneMode.Drain ? new Color(1f, 0.15f, 0.15f, 1f) : (originalBeamColor != default ? originalBeamColor : itemDrone.beamColor);

		itemDrone.droneColor = activeColor;
		itemDrone.beamColor = activeBeam;

		if (itemDrone.lineBetweenTwoPoints != null)
		{
			itemDrone.lineBetweenTwoPoints.SetColor(activeBeam);
		}

		Transform iconTransform = transform.Find("Drone Icon");
		if (iconTransform != null)
		{
			Renderer r = iconTransform.GetComponent<Renderer>();
			if (r != null && r.material != null)
			{
				r.material.SetColor("_EmissionColor", activeColor);
			}
		}

		ItemLight light = GetComponentInChildren<ItemLight>();
		if (light != null && light.itemLight != null)
		{
			light.itemLight.color = activeColor;
		}
	}

	private void PlayModeSwitchSound()
	{
		if (itemDrone != null && itemDrone.itemDroneSounds != null && itemDrone.itemDroneSounds.DroneStart != null)
		{
			itemDrone.itemDroneSounds.DroneStart.Play(transform.position);
		}
	}

	public bool CustomTargetingCondition(GameObject target)
	{
		if (target == null || itemBattery == null) return false;
		if (itemBattery.batteryLife >= 99f) return false;

		// 1. Item with battery
		ItemBattery targetBattery = target.GetComponent<ItemBattery>();
		if (targetBattery != null && targetBattery != itemBattery)
		{
			// Anti-exploit: do not allow draining items marked as unchargable
			if (targetBattery.isUnchargable) return false;

			// Anti-exploit: do not allow draining shop items (which have infinite battery in the shop)
			ItemAttributes attr = target.GetComponent<ItemAttributes>() ?? target.GetComponentInParent<ItemAttributes>();
			if (attr != null && attr.shopItem) return false;

			return targetBattery.batteryLife > 0f;
		}

		// 2. Enemy
		if (ConfigurationController.AllowTargetingMonsters?.Value ?? true)
		{
			EnemyParent enemyParent = target.GetComponentInParent<EnemyParent>() ?? target.GetComponent<EnemyParent>();
			if (enemyParent != null && enemyParent.Enemy != null && enemyParent.Enemy.Health != null)
			{
				return !enemyParent.Enemy.Health.dead && enemyParent.Enemy.Health.healthCurrent > 0;
			}
		}

		// 3. Player
		if (ConfigurationController.AllowTargetingPlayers?.Value ?? true)
		{
			PlayerAvatar player = target.GetComponentInParent<PlayerAvatar>() ?? target.GetComponent<PlayerAvatar>();
			if (player != null && !player.deadSet && player.playerHealth != null)
			{
				return player.playerHealth.health > 0;
			}
		}

		return false;
	}

	public void ExecuteDrain()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
		if (itemBattery == null || itemBattery.batteryLife >= 99.9f)
		{
			itemDrone.MagnetActiveToggle(toggleBool: false);
			return;
		}

		// Siphoning from Item Battery
		if ((bool)itemDrone.magnetTargetPhysGrabObject)
		{
			ItemBattery targetBattery = itemDrone.magnetTargetPhysGrabObject.GetComponent<ItemBattery>();
			if ((bool)targetBattery && targetBattery != itemBattery)
			{
				// Problem 1 Fix: Always show the target's battery HUD with drain animation
				targetBattery.OverrideBatteryShow(0.25f);
				var visualLogic = targetBattery.GetComponentInChildren<BatteryVisualLogic>();
				if (visualLogic != null)
				{
					visualLogic.OverrideBatteryDrain(0.25f);
				}

				// Also display the drone's battery HUD
				itemBattery.OverrideBatteryShow(0.25f);

				// Problem 2 Fix: Direct, conservative battery transfer
				float rate = ConfigurationController.TargetBatteryDrainPercentPerSecond?.Value ?? 20f;
				float transferAmount = rate * Time.deltaTime;

				// Cannot drain more than the target has, nor charge more than what the drone needs
				transferAmount = Mathf.Min(transferAmount, targetBattery.batteryLife);
				float droneNeeded = Mathf.Max(0f, 100f - itemBattery.batteryLife);
				transferAmount = Mathf.Min(transferAmount, droneNeeded);

				if (transferAmount > 0f)
				{
					targetBattery.batteryLife = Mathf.Clamp(targetBattery.batteryLife - transferAmount, 0f, 100f);
					itemBattery.batteryLife = Mathf.Clamp(itemBattery.batteryLife + transferAmount, 0f, 100f);

					targetBattery.TryVisualUpdate();
					itemBattery.TryVisualUpdate();
				}

				// Detach if target is completely drained or drone reached full capacity
				if (targetBattery.batteryLife <= 0.05f)
				{
					targetBattery.batteryLife = 0f;
					targetBattery.SetBatteryLife(0);
					itemDrone.MagnetActiveToggle(toggleBool: false);
				}
				else if (itemBattery.batteryLife >= 99.5f)
				{
					itemBattery.batteryLife = 100f;
					itemDrone.MagnetActiveToggle(toggleBool: false);
				}
				return;
			}

			// Leeching from Monster
			EnemyParent enemyParent = itemDrone.magnetTargetPhysGrabObject.GetComponentInParent<EnemyParent>();
			if ((bool)enemyParent && (bool)enemyParent.Enemy && (bool)enemyParent.Enemy.Health)
			{
				EnemyHealth enemyHealth = enemyParent.Enemy.Health;
				if (enemyHealth.dead || enemyHealth.healthCurrent <= 0)
				{
					itemDrone.MagnetActiveToggle(toggleBool: false);
					return;
				}

				itemBattery.OverrideBatteryShow(0.25f);

				tickTimer += Time.deltaTime;
				float tickRate = ConfigurationController.LeechTickIntervalSeconds?.Value ?? 0.5f;
				if (tickTimer >= tickRate)
				{
					tickTimer = 0f;
					int dmg = ConfigurationController.MonsterDamageFlatHpPerTick?.Value ?? 2;
					float gain = ConfigurationController.DroneBatteryGainPercentPerTick?.Value ?? 3f;

					enemyHealth.Hurt(dmg, Vector3.up * 0.1f);
					itemBattery.batteryLife = Mathf.Clamp(itemBattery.batteryLife + gain, 0f, 100f);
					itemBattery.TryVisualUpdate();

					if (enemyHealth.dead || enemyHealth.healthCurrent <= 0 || itemBattery.batteryLife >= 99.5f)
					{
						itemDrone.MagnetActiveToggle(toggleBool: false);
					}
				}
				return;
			}
		}

		// Leeching from Player
		PlayerAvatar player = itemDrone.playerAvatarTarget;
		if (!player && (bool)itemDrone.playerTumbleTarget)
		{
			player = itemDrone.playerTumbleTarget.playerAvatar;
		}

		if ((bool)player)
		{
			if (player.deadSet || player.playerHealth == null || player.playerHealth.health <= 0)
			{
				itemDrone.MagnetActiveToggle(toggleBool: false);
				return;
			}

			itemBattery.OverrideBatteryShow(0.25f);

			tickTimer += Time.deltaTime;
			float tickRate = ConfigurationController.LeechTickIntervalSeconds?.Value ?? 0.5f;
			if (tickTimer >= tickRate)
			{
				tickTimer = 0f;
				int dmg = ConfigurationController.PlayerDamageFlatHpPerTick?.Value ?? 1;
				float gain = ConfigurationController.DroneBatteryGainPercentPerTick?.Value ?? 3f;

				player.playerHealth.HurtOther(dmg, player.transform.position, false);
				itemBattery.batteryLife = Mathf.Clamp(itemBattery.batteryLife + gain, 0f, 100f);
				itemBattery.TryVisualUpdate();

				if (player.deadSet || player.playerHealth.health <= 0 || itemBattery.batteryLife >= 99.5f)
				{
					itemDrone.MagnetActiveToggle(toggleBool: false);
				}
			}
		}
	}
}
