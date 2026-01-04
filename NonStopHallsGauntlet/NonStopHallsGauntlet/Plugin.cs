using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using NonStopHallsGauntlet.Player;
using UnityEngine.SceneManagement;

namespace NonStopHallsGauntlet;

[BepInPlugin("com.lagerthon.NonStopHallsGauntlet", "High Halls Gauntlet", "1.2.0")]
public class Plugin : BaseUnityPlugin
{
	private const string HARMONY_ID = "com.lagerthon.NonStopHallsGauntlet";
	public static Plugin Instance { get; private set; }
	internal static ManualLogSource Log;
	private static Harmony _rootHarmony = null!;
	internal static Harmony RootHarmony => _rootHarmony;
	internal static ConfigEntry<KeyboardShortcut> TeleportKey;
	internal static ConfigEntry<KeyboardShortcut> CrestKey;
	public static ConfigEntry<int> posX;
	public static ConfigEntry<int> posY;
	public static ConfigEntry<float> scale;
	public static ConfigEntry<int> gap;
	public static ConfigEntry<int> nailUpgrade;
	public static ConfigEntry<int> numSilkHeart;
	public static ConfigEntry<int> numEnemies;
	public static ConfigEntry<int> enemyHeal;
	public static ConfigEntry<int> numTools;
	public static ConfigEntry<float> toolSpawnInterval;
	public static ConfigEntry<float> toolDisappearTime;
	public GameObject replenishManagerGO;
	private bool CrestsToolsUnlock = false;

	private void Awake()
	{
		_rootHarmony = new Harmony("com.lagerthon.NonStopHallsGauntlet");

		Instance = this;

		Log = Logger;

		posX = Config.Bind(
			"Enemy Images",
			"X Offset",
			400,
			"Changes horizontal position of images");

		posY = Config.Bind(
			"Enemy Images",
			"Y Offset",
			220,
			"Changes vertical position of images");

		scale = Config.Bind(
			"Enemy Images",
			"Scale",
			1.0f,
			"Changes the size of images and text");

		gap = Config.Bind(
			"Enemy Images",
			"Gap",
			10,
			"Changes  gap between images (pixels)");

		nailUpgrade = Config.Bind(
			"Player Settings",
			"Nail Upgrade",
			0,
			new ConfigDescription(
			"Nail Upgrades",
			new AcceptableValueRange<int>(0, 3)));

		numSilkHeart = Config.Bind(
			"Player Settings",
			"Number of Silk Hearts",
			2,
			new ConfigDescription(
			"Number of Silk Hearts",
			new AcceptableValueRange<int>(0, 3)));

		numEnemies = Config.Bind(
			"Enemy Settings",
			"Max Enemies",
			3,
			new ConfigDescription(
			"Maximum number of enemies per wave",
			new AcceptableValueRange<int>(3, 5)));

		enemyHeal = Config.Bind(
			"Enemy Settings",
			"Enemy heal value",
			10,
			new ConfigDescription(
			"Enemy HP shoots up by this percent of their max HP everytime they heal",
			new AcceptableValueList<int>(0, 10, 20, 30, 40, 50)));

		numTools = Config.Bind(
			"Red Tools Settings",
			"Number of tools",
			1,
			new ConfigDescription(
			"Number of tools that spawn at the same time",
			new AcceptableValueRange<int>(1, 3)));

		toolSpawnInterval = Config.Bind(
			"Red Tools Settings",
			"Tool Spawn Interval (seconds)",
			30f,
			new ConfigDescription(
			"Upon usage, one of the equipped red tools will spawn after approximately this many seconds",
			new AcceptableValueRange<float>(10f, 60f)));

		toolDisappearTime = Config.Bind(
			"Red Tools Settings",
			"Tool Pickup Time (seconds)",
			5f,
			new ConfigDescription(
			"Once spawned, the tool will disappear after this time",
			new AcceptableValueRange<float>(5f, 10f)));

		TeleportKey = Config.Bind(
			"Teleport Key",
			"Teleport to the High Halls gauntlet",
			new KeyboardShortcut(KeyCode.RightShift),
			"Key to move to High Halls gauntlet and apply player skills");

		CrestKey = Config.Bind(
			"Tools and Crests Key",
			"Unlock all Crests and Tools",
			new KeyboardShortcut(KeyCode.LeftShift),
			"WARNING: This will unlock all Crests and Tools permanently in the save file"
		);

		_rootHarmony.PatchAll(typeof(Battle));
		var overlayEnemies = new GameObject("OverlayEnemies");
		overlayEnemies.AddComponent<OverlayEnemies>();
		var highHallGO = new GameObject("MoveToHighHall");
		highHallGO.AddComponent<MoveToHighHall>();
		replenishManagerGO = new GameObject("ReplenishManager");
		DontDestroyOnLoad(replenishManagerGO);
	}
	private void Update()
	{
		HeroController hero = HeroController.instance;
		if (hero == null)
			return;

		if (TeleportKey.Value.IsDown() && hero != null)
		{
			MoveToHighHall.Instance.StartHighHallTeleport();
			OverlayEnemies.Instance.ClearAllOverlays();
			ReplenishManager rm = Plugin.Instance.replenishManagerGO.GetComponent<ReplenishManager>();
			if (rm != null)
			{
				Destroy(rm);
			}
		}

		if (!CrestsToolsUnlock && CrestKey.Value.IsDown() && hero != null)
		{
			ToolItemManager.UnlockAllTools();
			ToolItemManager.UnlockAllCrests();
			CrestsToolsUnlock = true;
		}

		if (SceneManager.GetActiveScene().name == "Quit_To_Menu")
		{
			OverlayEnemies.Instance.ClearAllOverlays();
			ReplenishManager rm = Plugin.Instance.replenishManagerGO.GetComponent<ReplenishManager>();
			if (rm != null)
			{
				Destroy(rm);
			}
		}
	}
}