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
	public DroneMode NextActivationMode { get; private set; } = DroneMode.Charge;
	public int DroneCycleState => (itemToggle != null && itemToggle.toggleState) ? (CurrentMode == DroneMode.Drain ? 2 : 1) : 0;

	private Color originalDroneColor;
	private Color originalBeamColor;
	private bool colorsCached;

	private float tickTimer;
	private EnemyParent? activeEnemyTarget;
	private bool naturalFullChargeDetach;
	private bool distanceBreakDetach;
	private float lastPlayerInteractTime = -999f;
	private bool previousToggleState;

	private void Awake()
	{
		itemDrone = GetComponent<ItemDrone>();
		itemDroneBattery = GetComponent<ItemDroneBattery>();
		itemBattery = GetComponent<ItemBattery>();
		itemToggle = GetComponent<ItemToggle>();
		physGrabObject = GetComponent<PhysGrabObject>();
		photonView = GetComponent<PhotonView>();

		if (photonView != null)
		{
			photonView.RefreshRpcMonoBehaviourCache();
		}

		if (itemBattery != null)
		{
			itemBattery.isUnchargable = false;
		}

		if (itemToggle != null)
		{
			itemToggle.onToggle.AddListener(OnItemToggled);
		}

		ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] Controller Awake on ViewID: {photonView?.ViewID}, isMaster: {SemiFunc.IsMasterClientOrSingleplayer()}");
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

		// Track toggle state changes
		if (itemToggle != null && previousToggleState != itemToggle.toggleState)
		{
			HandleToggleStateChange(itemToggle.toggleState);
		}

		// Keep tiny positive charge floor while in Drain mode so vanilla searching and sphere checks never abort
		if (CurrentMode == DroneMode.Drain && itemBattery != null && itemBattery.batteryLife <= 0.05f)
		{
			itemBattery.batteryLife = 0.001f;
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

		// Check if active enemy target detached
		if (SemiFunc.IsMasterClientOrSingleplayer() && activeEnemyTarget != null)
		{
			bool stillAttached = itemDrone != null
				&& itemDrone.currentState == ItemDrone.State.BeamDeployed
				&& itemDrone.targetIsEnemy
				&& itemDrone.enemyTarget == activeEnemyTarget
				&& itemToggle != null && itemToggle.toggleState
				&& CurrentMode == DroneMode.Drain;

			if (!stillAttached)
			{
				EnemyParent detachedEnemy = activeEnemyTarget;
				activeEnemyTarget = null;

				bool isNaturalOrDistance = naturalFullChargeDetach || distanceBreakDetach;
				naturalFullChargeDetach = false;
				distanceBreakDetach = false;

				if (!isNaturalOrDistance)
				{
					NotifyEnemyOfPlayer(detachedEnemy, isAttach: false);
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (itemToggle != null)
		{
			itemToggle.onToggle.RemoveListener(OnItemToggled);
		}

		if (activeEnemyTarget != null)
		{
			bool isNaturalOrDistance = naturalFullChargeDetach || distanceBreakDetach;
			naturalFullChargeDetach = false;
			distanceBreakDetach = false;

			if (!isNaturalOrDistance)
			{
				NotifyEnemyOfPlayer(activeEnemyTarget, isAttach: false);
			}
			activeEnemyTarget = null;
		}
	}

	private void OnItemToggled()
	{
		if (itemToggle == null) return;
		HandleToggleStateChange(itemToggle.toggleState);
	}

	private void HandleToggleStateChange(bool newState)
	{
		if (previousToggleState == newState) return;
		previousToggleState = newState;

		if (newState)
		{
			// Drone was turned ON!
			bool hasCharge = itemBattery != null && itemBattery.batteryLife > 0.05f;
			bool isFull = itemBattery != null && itemBattery.batteryLife >= 99f;

			DroneMode selectedMode;
			if (!hasCharge)
			{
				// Drone empty -> Must siphon!
				selectedMode = DroneMode.Drain;
				NextActivationMode = DroneMode.Charge;
			}
			else if (isFull)
			{
				// Drone full -> Must charge!
				selectedMode = DroneMode.Charge;
				NextActivationMode = DroneMode.Drain;
			}
			else
			{
				// Alternate between Charge and Drain
				selectedMode = NextActivationMode;
				NextActivationMode = (selectedMode == DroneMode.Charge) ? DroneMode.Drain : DroneMode.Charge;
			}

			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] Drone turned ON on ViewID: {photonView?.ViewID}. Mode: {selectedMode}, NextActivationMode: {NextActivationMode}, Battery: {itemBattery?.batteryLife:F1}%");
				SetMode(selectedMode);
			}
		}
		else
		{
			// Drone was turned OFF!
			ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] Drone turned OFF on ViewID: {photonView?.ViewID}. Was Mode: {CurrentMode}, NextActivationMode: {NextActivationMode}");
			if (CurrentMode == DroneMode.Drain)
			{
				if (itemBattery != null && itemBattery.batteryLife <= 0.05f)
				{
					itemBattery.batteryLife = 0f;
				}
			}
			CurrentMode = DroneMode.Charge;
			UpdateVisuals();
		}
	}

	public DroneMode GetAnticipatedActivationMode()
	{
		bool hasCharge = itemBattery != null && itemBattery.batteryLife > 0.05f;
		bool isFull = itemBattery != null && itemBattery.batteryLife >= 99f;

		if (!hasCharge)
		{
			return DroneMode.Drain;
		}
		if (isFull)
		{
			return DroneMode.Charge;
		}
		return NextActivationMode;
	}

	public DroneMode GetEffectiveModeForDisplay()
	{
		if (itemToggle != null && itemToggle.toggleState)
		{
			return CurrentMode;
		}
		return GetAnticipatedActivationMode();
	}

	public void HandleInteractPress()
	{
		lastPlayerInteractTime = Time.time;
		int player = SemiFunc.PhotonViewIDPlayerAvatarLocal();

		if (itemToggle == null) return;

		bool newToggle = !itemToggle.toggleState;
		ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] HandleInteractPress on ViewID: {photonView?.ViewID}. Toggling state to: {newToggle}");

		itemToggle.ToggleItem(newToggle, player);
	}

	public void SetMode(DroneMode mode)
	{
		ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] SetMode({mode}) called on ViewID: {photonView?.ViewID}. Multiplayer: {GameManager.Multiplayer()}");
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
		lastPlayerInteractTime = Time.time;
		ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] SetModeRPC({(DroneMode)mode}) received on ViewID: {photonView?.ViewID}. isMaster: {SemiFunc.IsMasterClientOrSingleplayer()}");

		if (CurrentMode == DroneMode.Drain)
		{
			if (itemBattery != null && itemBattery.batteryLife <= 0.05f)
			{
				itemBattery.batteryLife = 0.001f;
			}

			// If MasterClient and already toggled on, ensure we transition into searching
			if (SemiFunc.IsMasterClientOrSingleplayer() && itemToggle != null && itemToggle.toggleState)
			{
				if (itemDrone != null && (itemDrone.currentState == ItemDrone.State.Off || itemDrone.currentState == ItemDrone.State.NoBattery))
				{
					itemDrone.StateSet(ItemDrone.State.Searching);
				}
				else if (itemDrone != null && (itemDrone.currentState == ItemDrone.State.BeamDeployed || itemDrone.currentState == ItemDrone.State.Attached))
				{
					if (itemDrone.magnetActive)
					{
						itemDrone.MagnetActiveToggle(false);
					}
					itemDrone.StateSet(ItemDrone.State.Searching);
				}
			}

			PlayModeSwitchSound();
		}
		else
		{
			if (itemBattery != null && itemBattery.batteryLife <= 0.05f)
			{
				itemBattery.batteryLife = 0f;
			}

			if (SemiFunc.IsMasterClientOrSingleplayer() && itemToggle != null && itemToggle.toggleState)
			{
				if (itemDrone != null && (itemDrone.currentState == ItemDrone.State.Off || itemDrone.currentState == ItemDrone.State.NoBattery))
				{
					if (itemBattery != null && itemBattery.batteryLife > 0.05f)
					{
						itemDrone.StateSet(ItemDrone.State.Searching);
					}
				}
				else if (itemDrone != null && (itemDrone.currentState == ItemDrone.State.BeamDeployed || itemDrone.currentState == ItemDrone.State.Attached))
				{
					if (itemDrone.magnetActive)
					{
						itemDrone.MagnetActiveToggle(false);
					}
					itemDrone.StateSet(ItemDrone.State.Searching);
				}
			}
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

		if (itemDrone.onSwitchTransform != null)
		{
			Renderer r = itemDrone.onSwitchTransform.GetComponent<Renderer>();
			if (r != null && r.material != null)
			{
				r.material.SetColor("_EmissionColor", activeColor);
			}
		}

		if (itemDrone.dronePyramidTransforms != null)
		{
			foreach (var pyr in itemDrone.dronePyramidTransforms)
			{
				if (pyr != null)
				{
					Renderer r = pyr.GetComponent<Renderer>();
					if (r != null && r.material != null)
					{
						r.material.SetColor("_EmissionColor", activeColor);
					}
				}
			}
		}

		if (itemDrone.droneTransform != null)
		{
			Renderer r = itemDrone.droneTransform.GetComponent<Renderer>();
			if (r != null && r.material != null)
			{
				r.material.SetColor("_EmissionColor", activeColor);
			}
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

		ReversibleBatteryDronePlugin.Logger?.LogInfo($"[ReversibleBatteryDrone] UpdateVisuals on ViewID: {photonView?.ViewID}. Mode: {CurrentMode}, activeBeam: {(CurrentMode == DroneMode.Drain ? "RED" : "YELLOW")}");
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
			if (targetBattery.isUnchargable) return false;
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

	public void CustomStateSearching()
	{
		if (itemDrone.stateStart)
		{
			itemDrone.stateTimerMax = 0.5f;
			itemDrone.stateTimer = 0f;
			itemDrone.BatteryToggle(activated: false);
			itemDrone.lerpAnimationProgress = 0f;
			itemDrone.itemActivated = true;
			if (itemDrone.physGrabObject != null && itemDrone.physGrabObject.impactDetector != null)
			{
				itemDrone.physGrabObject.impactDetector.canHurtLogic = false;
			}
			if (itemDrone.onSwitchTransform != null)
			{
				Renderer r = itemDrone.onSwitchTransform.GetComponent<Renderer>();
				if (r != null && r.material != null)
				{
					r.material.SetColor("_EmissionColor", itemDrone.droneColor);
				}
			}
			if (itemDrone.itemDroneSounds != null && itemDrone.itemDroneSounds.DroneStart != null)
			{
				itemDrone.itemDroneSounds.DroneStart.Play(transform.position);
			}
			itemDrone.droneOwner = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
			itemDrone.stateStart = false;
		}

		itemDrone.soundDroneLoop.PlayLoop(playing: true, 2f, 2f);
		itemDrone.AnimateDrone();

		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}

		if (!itemToggle.toggleState)
		{
			itemDrone.StateSet(ItemDrone.State.Off);
			return;
		}

		itemDrone.stateTimer += Time.deltaTime;
		if (itemDrone.stateTimer < itemDrone.stateTimerMax)
		{
			return;
		}

		itemDrone.stateTimer = 0f;
		itemDrone.playerTumbleTarget = null;
		itemDrone.playerAvatarTarget = null;
		itemDrone.targetIsPlayer = false;
		itemDrone.targetIsEnemy = false;
		itemDrone.targetIsLocalPlayer = false;

		if (FindDrainTarget())
		{
			itemDrone.hadTarget = true;
			itemDrone.ActivateMagnet();
			itemDrone.StateSet(ItemDrone.State.BeamDeployed);
		}
	}

	private bool FindDrainTarget()
	{
		if (itemBattery == null || itemBattery.batteryLife >= 99f) return false;

		float searchRadius = 3.5f;
		Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);

		float closestBatteryDistance = float.MaxValue;
		GameObject? bestBatteryTarget = null;

		float closestEnemyDistance = float.MaxValue;
		GameObject? bestEnemyTarget = null;

		float closestPlayerDistance = float.MaxValue;
		GameObject? bestPlayerTarget = null;

		PlayerAvatar? holdingPlayer = null;
		if (physGrabObject != null && physGrabObject.playerGrabbing.Count > 0)
		{
			holdingPlayer = physGrabObject.playerGrabbing[0].playerAvatar;
		}

		foreach (Collider col in colliders)
		{
			if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

			// 1. Check for ItemBattery
			ItemBattery b = col.GetComponent<ItemBattery>() 
				?? col.GetComponentInParent<ItemBattery>() 
				?? col.GetComponentInChildren<ItemBattery>();

			if (b != null && b != itemBattery && b.batteryLife > 0.05f && !b.isUnchargable)
			{
				ItemAttributes attr = b.GetComponent<ItemAttributes>() ?? b.GetComponentInParent<ItemAttributes>();
				if (attr == null || !attr.shopItem)
				{
					float dist = Vector3.Distance(transform.position, b.transform.position);
					if (dist < closestBatteryDistance)
					{
						closestBatteryDistance = dist;
						bestBatteryTarget = b.gameObject;
					}
				}
			}

			// 2. Check for Enemy
			if (ConfigurationController.AllowTargetingMonsters?.Value ?? true)
			{
				EnemyParent ep = col.GetComponentInParent<EnemyParent>() ?? col.GetComponent<EnemyParent>();
				if (ep != null && ep.Enemy != null && ep.Enemy.Health != null)
				{
					if (!ep.Enemy.Health.dead && ep.Enemy.Health.healthCurrent > 0)
					{
						Transform enemyCenter = ep.Enemy.CenterTransform != null ? ep.Enemy.CenterTransform : ep.transform;
						float dist = Vector3.Distance(transform.position, enemyCenter.position);
						if (dist < closestEnemyDistance)
						{
							closestEnemyDistance = dist;
							bestEnemyTarget = ep.gameObject;
						}
					}
				}
			}

			// 3. Check for Player
			if (ConfigurationController.AllowTargetingPlayers?.Value ?? true)
			{
				PlayerAvatar pa = col.GetComponentInParent<PlayerAvatar>() ?? col.GetComponent<PlayerAvatar>();
				if (pa != null && pa != holdingPlayer && !pa.deadSet && pa.playerHealth != null && pa.playerHealth.health > 0)
				{
					Transform vision = pa.PlayerVisionTarget != null && pa.PlayerVisionTarget.VisionTransform != null ? pa.PlayerVisionTarget.VisionTransform : pa.transform;
					float dist = Vector3.Distance(transform.position, vision.position);
					if (dist < closestPlayerDistance)
					{
						closestPlayerDistance = dist;
						bestPlayerTarget = pa.gameObject;
					}
				}
			}
		}

		GameObject? bestTarget = null;
		int targetType = 0; // 1 = Battery Item, 2 = Enemy, 3 = Player

		// Priority: Battery Items > Enemies > Players
		if (bestBatteryTarget != null)
		{
			bestTarget = bestBatteryTarget;
			targetType = 1;
		}
		else if (bestEnemyTarget != null)
		{
			bestTarget = bestEnemyTarget;
			targetType = 2;
		}
		else if (bestPlayerTarget != null)
		{
			bestTarget = bestPlayerTarget;
			targetType = 3;
		}

		if (bestTarget == null) return false;

		if (targetType != 2 && activeEnemyTarget != null)
		{
			EnemyParent detachedEnemy = activeEnemyTarget;
			activeEnemyTarget = null;
			NotifyEnemyOfPlayer(detachedEnemy, isAttach: false);
		}

		// Attach to best target
		if (targetType == 1)
		{
			ItemBattery targetBattery = bestTarget.GetComponent<ItemBattery>() 
				?? bestTarget.GetComponentInParent<ItemBattery>() 
				?? bestTarget.GetComponentInChildren<ItemBattery>();

			itemDrone.targetIsEnemy = false;
			itemDrone.targetIsPlayer = false;
			itemDrone.targetIsLocalPlayer = false;
			itemDrone.enemyTarget = null;
			itemDrone.playerAvatarTarget = null;
			itemDrone.playerTumbleTarget = null;

			PhysGrabObject? pgo = targetBattery.GetComponent<PhysGrabObject>() 
				?? targetBattery.GetComponentInParent<PhysGrabObject>()
				?? targetBattery.GetComponentInChildren<PhysGrabObject>();

			Transform rootTransform = pgo != null ? pgo.transform : targetBattery.transform;
			int colliderId = -1;
			Vector3 attachPos = rootTransform.position;

			if (pgo != null && pgo.colliders != null && pgo.colliders.Count > 0 && pgo.colliders[0] != null)
			{
				var pgc = pgo.colliders[0].GetComponent<PhysGrabObjectCollider>();
				if (pgc != null)
				{
					colliderId = pgc.colliderID;
					attachPos = pgo.colliders[0].position;
				}
			}

			itemDrone.magnetTarget = (colliderId != -1 && pgo != null && pgo.colliders != null && pgo.colliders.Count > 0) ? pgo.colliders[0] : rootTransform;
			itemDrone.magnetTargetPhysGrabObject = pgo;
			itemDrone.magnetTargetRigidbody = rootTransform.GetComponent<Rigidbody>();

			PhotonView? pv = pgo != null && pgo.photonView != null ? pgo.photonView : targetBattery.GetComponent<PhotonView>();
			int viewId = pv != null ? pv.ViewID : 0;

			// targetTransform MUST be rootTransform (the object with PhysGrabObject), NOT the child collider!
			itemDrone.NewRayHitPoint(attachPos, viewId, colliderId, rootTransform);
			if (itemDrone.rayHitPosition == Vector3.zero) itemDrone.rayHitPosition = new Vector3(0f, 0.05f, 0f);
			itemDrone.attachPoint = itemDrone.rayHitPosition;
			return true;
		}

		if (targetType == 2)
		{
			EnemyParent ep = bestTarget.GetComponentInParent<EnemyParent>() ?? bestTarget.GetComponent<EnemyParent>();
			itemDrone.enemyTarget = ep;
			itemDrone.targetIsEnemy = true;
			itemDrone.targetIsPlayer = false;
			itemDrone.targetIsLocalPlayer = false;
			itemDrone.playerAvatarTarget = null;
			itemDrone.playerTumbleTarget = null;

			Transform enemyCenter = ep.Enemy.CenterTransform != null ? ep.Enemy.CenterTransform : ep.transform;
			PhysGrabObject? pgo = ep.Enemy.Rigidbody != null ? ep.Enemy.Rigidbody.physGrabObject : ep.GetComponent<PhysGrabObject>();
			itemDrone.magnetTargetPhysGrabObject = pgo;
			itemDrone.magnetTargetRigidbody = ep.Enemy.Rigidbody != null ? ep.Enemy.Rigidbody.rb : ep.GetComponent<Rigidbody>();

			Transform rootTransform = pgo != null ? pgo.transform : enemyCenter;
			int colliderId = -1;
			Vector3 attachPos = enemyCenter.position + Vector3.up * 0.1f;

			if (pgo != null && pgo.colliders != null && pgo.colliders.Count > 0 && pgo.colliders[0] != null)
			{
				var pgc = pgo.colliders[0].GetComponent<PhysGrabObjectCollider>();
				if (pgc != null)
				{
					colliderId = pgc.colliderID;
					attachPos = pgo.colliders[0].position;
				}
			}

			itemDrone.magnetTarget = (colliderId != -1 && pgo != null && pgo.colliders != null && pgo.colliders.Count > 0) ? pgo.colliders[0] : enemyCenter;

			PhotonView? pv = pgo != null && pgo.photonView != null ? pgo.photonView : (ep.Enemy.PhotonView != null ? ep.Enemy.PhotonView : ep.GetComponent<PhotonView>());
			int viewId = pv != null ? pv.ViewID : 0;

			// targetTransform MUST be rootTransform (the object with PhysGrabObject), NOT the child collider!
			itemDrone.NewRayHitPoint(attachPos, viewId, colliderId, rootTransform);
			if (itemDrone.rayHitPosition == Vector3.zero) itemDrone.rayHitPosition = new Vector3(0f, 0.05f, 0f);
			itemDrone.attachPoint = itemDrone.rayHitPosition;

			if (activeEnemyTarget != null && activeEnemyTarget != ep)
			{
				EnemyParent detachedEnemy = activeEnemyTarget;
				activeEnemyTarget = null;
				NotifyEnemyOfPlayer(detachedEnemy, isAttach: false);
			}
			activeEnemyTarget = ep;
			NotifyEnemyOfPlayer(ep, isAttach: true);
			return true;
		}

		if (targetType == 3)
		{
			PlayerAvatar pa = bestTarget.GetComponentInParent<PlayerAvatar>() ?? bestTarget.GetComponent<PlayerAvatar>();
			itemDrone.playerAvatarTarget = pa;
			itemDrone.targetIsPlayer = true;
			itemDrone.targetIsEnemy = false;
			itemDrone.targetIsLocalPlayer = pa.isLocal;
			itemDrone.enemyTarget = null;
			itemDrone.playerTumbleTarget = null;

			Transform visionTransform = pa.PlayerVisionTarget != null && pa.PlayerVisionTarget.VisionTransform != null ? pa.PlayerVisionTarget.VisionTransform : pa.transform;
			itemDrone.magnetTarget = visionTransform;
			itemDrone.magnetTargetPhysGrabObject = null;
			itemDrone.magnetTargetRigidbody = null;

			Vector3 attachPos = visionTransform.position;
			itemDrone.NewRayHitPoint(attachPos, pa.photonView.ViewID, -1, visionTransform);
			if (itemDrone.rayHitPosition == Vector3.zero) itemDrone.rayHitPosition = new Vector3(0f, 0.05f, 0f);
			itemDrone.attachPoint = itemDrone.rayHitPosition;
			return true;
		}

		return false;
	}

	public void ExecuteDrain()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
		if (itemBattery == null || itemBattery.batteryLife >= 99.9f)
		{
			naturalFullChargeDetach = true;
			itemDrone.MagnetActiveToggle(toggleBool: false);
			return;
		}

		if (itemDrone.magnetTarget != null && Vector3.Distance(transform.position, itemDrone.magnetTarget.position) > 8f)
		{
			distanceBreakDetach = true;
			itemDrone.MagnetActiveToggle(toggleBool: false);
			return;
		}

		// 1. Siphoning from Item Battery
		ItemBattery? targetBattery = null;
		if (itemDrone.magnetTargetPhysGrabObject != null)
		{
			targetBattery = itemDrone.magnetTargetPhysGrabObject.GetComponent<ItemBattery>();
		}
		if (targetBattery == null && itemDrone.magnetTarget != null)
		{
			targetBattery = itemDrone.magnetTarget.GetComponent<ItemBattery>() ?? itemDrone.magnetTarget.GetComponentInParent<ItemBattery>();
		}

		if (targetBattery != null && targetBattery != itemBattery)
		{
			// Target item visual feedback (floating HUD with drain animation)
			targetBattery.OverrideBatteryShow(0.25f);
			var targetVisualLogic = targetBattery.GetComponentInChildren<BatteryVisualLogic>();
			if (targetVisualLogic != null)
			{
				targetVisualLogic.OverrideBatteryDrain(0.25f);
			}

			// Drone visual feedback (floating HUD with charging animation)
			itemBattery.OverrideBatteryShow(0.25f);
			var droneVisualLogic = itemBattery.GetComponentInChildren<BatteryVisualLogic>();
			if (droneVisualLogic != null)
			{
				droneVisualLogic.OverrideBatteryCharge(0.25f);
			}

			float rate = ConfigurationController.TargetBatteryDrainPercentPerSecond?.Value ?? 5f;

			// Use the native game engine charging and draining pipeline
			targetBattery.Drain(rate);
			itemBattery.ChargeBattery(targetBattery.gameObject, rate);

			// Detach if target is completely drained or drone reached full capacity
			if (targetBattery.batteryLife <= 0.05f)
			{
				targetBattery.batteryLife = 0f;
				targetBattery.SetBatteryLife(0);
				itemDrone.MagnetActiveToggle(toggleBool: false);
			}
			else if (itemBattery.batteryLife >= 99f)
			{
				itemBattery.batteryLife = 100f;
				naturalFullChargeDetach = true;
				itemDrone.MagnetActiveToggle(toggleBool: false);
			}
			return;
		}

		// 2. Leeching from Monster
		EnemyParent? enemyParent = itemDrone.enemyTarget;
		if (enemyParent == null && itemDrone.magnetTargetPhysGrabObject != null)
		{
			enemyParent = itemDrone.magnetTargetPhysGrabObject.GetComponentInParent<EnemyParent>();
		}
		if (enemyParent == null && itemDrone.magnetTarget != null)
		{
			enemyParent = itemDrone.magnetTarget.GetComponentInParent<EnemyParent>() ?? itemDrone.magnetTarget.GetComponent<EnemyParent>();
		}

		if (enemyParent != null && enemyParent.Enemy != null && enemyParent.Enemy.Health != null)
		{
			EnemyHealth enemyHealth = enemyParent.Enemy.Health;
			if (enemyHealth.dead || enemyHealth.healthCurrent <= 0)
			{
				itemDrone.MagnetActiveToggle(toggleBool: false);
				return;
			}

			itemBattery.OverrideBatteryShow(0.25f);
			var droneVisualLogic = itemBattery.GetComponentInChildren<BatteryVisualLogic>();
			if (droneVisualLogic != null)
			{
				droneVisualLogic.OverrideBatteryCharge(0.25f);
			}

			tickTimer += Time.deltaTime;
			float tickRate = ConfigurationController.LeechTickIntervalSeconds?.Value ?? 0.5f;
			if (tickTimer >= tickRate)
			{
				tickTimer = 0f;
				int dmg = ConfigurationController.MonsterDamageFlatHpPerTick?.Value ?? 1;
				float gain = ConfigurationController.DroneBatteryGainPercentPerTick?.Value ?? 1f;

				enemyHealth.Hurt(dmg, Vector3.up * 0.1f);
				itemBattery.ChargeBattery(enemyParent.gameObject, gain * (1f / Mathf.Max(0.01f, tickRate)));

				if (enemyHealth.dead || enemyHealth.healthCurrent <= 0 || itemBattery.batteryLife >= 99f)
				{
					if (itemBattery.batteryLife >= 99f)
					{
						naturalFullChargeDetach = true;
					}
					itemDrone.MagnetActiveToggle(toggleBool: false);
				}
			}
			return;
		}

		// 3. Leeching from Player
		PlayerAvatar? player = itemDrone.playerAvatarTarget;
		if (player == null && itemDrone.playerTumbleTarget != null)
		{
			player = itemDrone.playerTumbleTarget.playerAvatar;
		}

		if (player != null)
		{
			if (player.deadSet || player.playerHealth == null || player.playerHealth.health <= 0)
			{
				itemDrone.MagnetActiveToggle(toggleBool: false);
				return;
			}

			itemBattery.OverrideBatteryShow(0.25f);
			var droneVisualLogic = itemBattery.GetComponentInChildren<BatteryVisualLogic>();
			if (droneVisualLogic != null)
			{
				droneVisualLogic.OverrideBatteryCharge(0.25f);
			}

			tickTimer += Time.deltaTime;
			float tickRate = ConfigurationController.LeechTickIntervalSeconds?.Value ?? 0.5f;
			if (tickTimer >= tickRate)
			{
				tickTimer = 0f;
				int dmg = ConfigurationController.PlayerDamageFlatHpPerTick?.Value ?? 10;
				float gain = ConfigurationController.DroneBatteryGainPercentPerTick?.Value ?? 1f;

				player.playerHealth.HurtOther(dmg, Vector3.zero, false);
				itemBattery.ChargeBattery(player.gameObject, gain * (1f / Mathf.Max(0.01f, tickRate)));

				if (player.deadSet || player.playerHealth.health <= 0 || itemBattery.batteryLife >= 99f)
				{
					if (itemBattery.batteryLife >= 99f)
					{
						naturalFullChargeDetach = true;
					}
					itemDrone.MagnetActiveToggle(toggleBool: false);
				}
			}
		}
	}

	private PlayerAvatar? GetResponsiblePlayerNearEnemy(Vector3 enemyPos, float maxDistance)
	{
		// 1. If currently held by a player within reasonable reach
		if (physGrabObject != null && physGrabObject.playerGrabbing.Count > 0)
		{
			PlayerAvatar grabber = physGrabObject.playerGrabbing[0].playerAvatar;
			if (grabber != null && !grabber.deadSet && !grabber.isDisabled)
			{
				if (Vector3.Distance(grabber.transform.position, enemyPos) <= maxDistance + 2.5f)
				{
					return grabber;
				}
			}
		}

		// 2. Drone owner if alive and within detection range
		if (itemDrone != null && itemDrone.droneOwner != null && !itemDrone.droneOwner.deadSet && !itemDrone.droneOwner.isDisabled)
		{
			if (Vector3.Distance(itemDrone.droneOwner.transform.position, enemyPos) <= maxDistance)
			{
				return itemDrone.droneOwner;
			}
		}

		// 3. Closest alive, non-disabled player within detection range
		PlayerAvatar? nearest = null;
		float minDist = maxDistance;
		var players = SemiFunc.PlayerGetList();
		if (players != null)
		{
			foreach (var p in players)
			{
				if (p != null && !p.deadSet && !p.isDisabled)
				{
					float d = Vector3.Distance(enemyPos, p.transform.position);
					if (d <= minDist)
					{
						minDist = d;
						nearest = p;
					}
				}
			}
		}
		return nearest;
	}

	private void NotifyEnemyOfPlayer(EnemyParent? ep, bool isAttach)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
		if (ep == null || ep.Enemy == null) return;
		if (ep.Enemy.Health != null && (ep.Enemy.Health.dead || ep.Enemy.Health.healthCurrent <= 0)) return;

		bool shouldAlert = isAttach
			? (ConfigurationController.AlertEnemyOnAttach?.Value ?? true)
			: (ConfigurationController.AlertEnemyOnDetach?.Value ?? true);

		if (!shouldAlert) return;

		// For detach: only alert if there was an active player action (holding the drone or recently interacted with it near the enemy)
		if (!isAttach)
		{
			bool isGrabbed = physGrabObject != null && physGrabObject.playerGrabbing.Count > 0;
			bool recentInteract = (Time.time - lastPlayerInteractTime) < 1.0f;
			if (!isGrabbed && !recentInteract)
			{
				return;
			}
		}

		Vector3 enemyPos = ep.Enemy.CenterTransform != null ? ep.Enemy.CenterTransform.position : ep.transform.position;
		float maxRange = ConfigurationController.AlertEnemyDetectionRange?.Value ?? 6.0f;

		PlayerAvatar? targetPlayer = GetResponsiblePlayerNearEnemy(enemyPos, maxRange);
		if (targetPlayer == null) return;

		ep.Enemy.SetChaseTarget(targetPlayer);
		if (ep.Enemy.HasStateChase && (ep.Enemy.CurrentState == EnemyState.Chase || ep.Enemy.CurrentState == EnemyState.ChaseBegin))
		{
			ep.Enemy.TargetPlayerViewID = targetPlayer.photonView.ViewID;
			ep.Enemy.TargetPlayerAvatar = targetPlayer;
		}
	}
}
