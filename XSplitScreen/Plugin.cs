using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using System;
using Rewired;
using System.Reflection;
using UnityEngine;
using System.Linq;
using RoR2.UI.MainMenu;
using RoR2.UI;
using Newtonsoft.Json;
using R2API;
using dodad.XSplitscreen.Components;
using System.Runtime.CompilerServices;

namespace dodad.XSplitscreen
{
    [BepInPlugin(PluginGUID, PluginName, "4.0.0")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
	[BepInDependency(LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
	public class Plugin : BaseUnityPlugin
    {
		#region Variables
		public const string PluginGUID = PluginAuthor + "." + PluginName;
		public const string PluginAuthor = "com.dodad";
		public const string PluginName = "XSplitscreen";

		// public const string PluginVersion = "3.1.4";
		//public const string PluginTag = "XSS";
		//public const string PluginBundle = "xsplitscreenbundle";
		//public const bool developerMode = false;
		//public const bool clearAssignmentsOnStart = false;
		//public const bool logModeOverrideToAll = false;
		//public static bool active => UserManager.localUsers.Count > 0;
		//public static string bundleKey { get; private set; }
		//private static GameObject assignmentScreen;
		//private static GameObject titleButton;
		//private static Coroutine initializationCoroutine;

		//-----------------------------------------------------------------------------------------------------------

		public static Plugin Singleton { get; private set; }
        public static AssetBundle Resources { get; private set; }
        private static HGButton MainMenuTitleButton;
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Mono Methods
		public void Awake()
        {
            if (Singleton != null)
                return;

            Singleton = this;
            
            // Load logger

            if (!LoadLogger())
                return;

            // Load assets

            if (!LoadAssets())
                return;
            else
                Log.Print($"Loaded '{Resources.GetAllAssetNames().Length}' assets");

            // Handle patches

			SetGenericPatches();

            // Load language tokens

            if (!LoadLanguage())
                return;
            else
                Log.Print("Loaded language");

            // Set up listeners

            SetSubscriptions(true);

			/*Log.Init(Logger);

            var assembly = Assembly.GetExecutingAssembly();
            string bundleKey;

            if (XLibrary.Resources.RegisterBundle(assembly, this, $"DoDad.XSplitScreen.{PluginBundle}", out bundleKey))
            {
                Log.LogOutput($"Initializing", Log.LogLevel.Debug);
                XLibrary.Plugin.InitializePlugin(assembly, this, new XLibrary.Language.TokenConfiguration(PluginTag));
                Initialize();
                SceneManager.activeSceneChanged += ActiveSceneChanged;
                Plugin.bundleKey = bundleKey;
            }
            else
            {
                Log.LogOutput($"Unable to register asset bundle. Plugin disabled.", Log.LogLevel.Warning);
                return;
            }*/
		}
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Initialization
        /// <summary>
        /// Load language file from disk
        /// </summary>
        private static bool LoadLanguage()
		{
            // TODO
            // If a file is found, load it and merge the tokens to account for user customizations

			string filePath = $"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\language.json";

            try
            {
                MultiDictionary tokens = new MultiDictionary();

                // Write a language file if missing, otherwise load it

                if(!System.IO.File.Exists(filePath))
                {
                    // English

                    string enKey = "en";

                    tokens.Add(enKey, new StringDictionary());

                    tokens[enKey].Add("XSS_NAME_HOVER", "Modify splitscreen settings.");
                    tokens[enKey].Add("XSS_OPTION_DISCORD_HOVER", "Join for support, feedback or updates");
                    tokens[enKey].Add("XSS_PRESS_START", "- press start -");
                    tokens[enKey].Add("XSS_CONFIG_PROFILE", "Profile");
                    tokens[enKey].Add("XSS_CONFIG_COLOR", "Color");
                    tokens[enKey].Add("XSS_OPTION_MMM", "Multi-Monitor Mode");
                    tokens[enKey].Add("XSS_OPTION_MMM_HOVER", "Enable Multi-Monitor Mode (cannot be disabled)");

                    // pt-br

                    string ptBrKey = "pt-br";

					tokens.Add(ptBrKey, new StringDictionary());

					tokens[ptBrKey].Add("XSS_NAME_HOVER", "Modificar as configurações da tela-dividida.");
					tokens[ptBrKey].Add("XSS_OPTION_DISCORD_HOVER", "Entre para suporte, dar feedback ou ver atualizações");
					tokens[ptBrKey].Add("XSS_OPTION_MMM", "Multi-Monitor Mode");
					tokens[ptBrKey].Add("XSS_OPTION_MMM_HOVER", "Enable Multi-Monitor Mode (cannot be disabled)");

					// fr

					string frKey = "fr";

					tokens.Add(frKey, new StringDictionary());

					tokens[frKey].Add("XSS_NAME_HOVER", "Modifier les paramètres de splitscreen.");
					tokens[frKey].Add("XSS_OPTION_DISCORD_HOVER", "Rejoignez nous pour du support, du feedback ou des mises à jour");
					tokens[frKey].Add("XSS_OPTION_MMM", "Multi-Monitor Mode");
					tokens[frKey].Add("XSS_OPTION_MMM_HOVER", "Enable Multi-Monitor Mode (cannot be disabled)");

					// Shared

					foreach (var language in tokens)
                    {
                        language.Value.Add("XSS_NAME", "XSplitscreen");
                        language.Value.Add("XSS_OPTION_DISCORD", "Discord");
						language.Value.Add("XSS_UNSET", "- not set -");
						//language.Value.Add("XSS_PRESS_START", "- press start -");
					}

                    System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(tokens, Formatting.Indented));
				}
                else
                {
                    var file = System.IO.File.ReadAllText(filePath);

                    var jObject = JsonConvert.DeserializeObject(file, typeof(MultiDictionary));

					tokens = (MultiDictionary)jObject;
                }

				// Add tokens through LanguageAPI

                foreach(var language in tokens)
                {
                    foreach(var token in language.Value)
                        LanguageAPI.Add(token.Key, token.Value, language.Key);
                }

                return true;
			}
            catch (Exception e)
            {
                Log.Print(e, Log.ELogChannel.Fatal);

                return false;
            }
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Subscribe or unsubscribe to events
		/// </summary>
		/// <param name="state"></param>
		private static void SetSubscriptions(bool state)
        {
            if(state)
			{
				MainMenuController.OnMainMenuInitialised += OnMainMenuInitialized;
			}
            else
            {
				MainMenuController.OnMainMenuInitialised -= OnMainMenuInitialized;
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Load the user set log levels from the Config file
		/// </summary>
		/// <returns></returns>
		private static bool LoadLogger()
		{
            // Use enum names to retrieve Config entries containing bools to 
            // determine whether or not we should print messages from that 
            // log channel

            Log.ELogChannel channels = Log.ELogChannel.None;

            var logChannelKeys = Enum.GetNames(typeof(Log.ELogChannel))
                .Where(x => x.ToLower() != "none").ToArray();

            int channelCount = logChannelKeys.Length;

			ConfigEntry<bool>[] channelEntries = new ConfigEntry<bool>[channelCount];

			string section = "Log Channels";

			try
			{
                for(int e = 0; e < channelCount; e++)
                {
                    channelEntries[e] = Singleton.Config.Bind(section, logChannelKeys[e], true);

                    if (channelEntries[e].Value)
                        channels |= (Log.ELogChannel)Enum.Parse(typeof(Log.ELogChannel), logChannelKeys[e]);
                }

				Log.Init(Singleton.Logger, channels);

                return true;
			}
			catch (Exception e)
			{
				Log.Init(Singleton.Logger, Log.ELogChannel.All);
				Log.Print(e, Log.ELogChannel.Error);

				return false;
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Load the included Asset Bundle and make those resources available to the mod
		/// </summary>
		private static bool LoadAssets()
        {
            try
			{
				var assembly = Assembly.GetExecutingAssembly();

                var resourceStream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Last());

                using (resourceStream)
                    Resources = AssetBundle.LoadFromStream(resourceStream);

                return Resources != null;
			}
            catch(Exception e)
            {
                Log.Print(e, Log.ELogChannel.Fatal);

                return false;
            }
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Create hooks for low level splitscreen issues
		/// </summary>
		private static void SetGenericPatches()
		{
			var harmony = new HarmonyLib.Harmony("com.dodad.xsplitscreen.hooks");

			// Resize RoR2Application max local players

			var ror2AppResizePlayersOriginal = typeof(RoR2.RoR2Application).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
			var ror2AppResizePlayersPatch = typeof(Plugin).GetMethod("RoR2AppResizePlayers", BindingFlags.Static | BindingFlags.NonPublic);

			harmony.Patch(ror2AppResizePlayersOriginal, prefix: new HarmonyLib.HarmonyMethod(ror2AppResizePlayersPatch));

			// Add Rewired players

			var rewiredClonePlayersOriginal = typeof(InputManager_Base).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
			var rewiredClonePlayersPatch = typeof(Plugin).GetMethod("RewiredClonePlayers", BindingFlags.Static | BindingFlags.NonPublic);

			harmony.Patch(rewiredClonePlayersOriginal, prefix: new HarmonyLib.HarmonyMethod(rewiredClonePlayersPatch));

            // UI patches

            var mmcInteractableOriginal = typeof(RoR2.UI.MainMenu.MainMenuController).GetMethod("SetButtonsInteractible", BindingFlags.Instance | BindingFlags.Public);
            var mmcInteractablePatch = typeof(Plugin).GetMethod("SetButtonInteractable", BindingFlags.Static | BindingFlags.NonPublic);

            harmony.Patch(mmcInteractableOriginal, prefix: new HarmonyLib.HarmonyMethod(mmcInteractablePatch));
		}
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Initialization
        /// <summary>
        /// Load and format the main menu UI
        /// </summary>
		private static void CreateTitleUI()
        {
            if (MainMenuTitleButton != null)
                return;

            // Main menu title button

            var container = MainMenuController.instance.singlePlayerButton.transform.parent;

            var titleObject = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.MainMenuButton);

            if(titleObject == null)
            {
                Log.Print("UIHelper prefab is null (MainMenuButton)");
                return;
            }

			titleObject.name = "GenericMenuButton (XSplitscreen)";
			titleObject.transform.SetParent(container);
			titleObject.transform.SetSiblingIndex(MainMenuController.instance.singlePlayerButton.transform.GetSiblingIndex() - 1);
			titleObject.transform.localScale = Vector3.one;

            var titleButtonLanguage = titleObject.GetComponent<LanguageTextMeshController>();
            titleButtonLanguage.token = "XSS_NAME";

            var titleButton = titleObject.GetComponent<HGButton>();
            titleButton.hoverToken = "XSS_NAME_HOVER";
            titleButton.requiredTopLayer = MainMenuController.instance.singlePlayerButton.requiredTopLayer;
            titleButton.hoverLanguageTextMeshController = MainMenuController.instance.singlePlayerButton.hoverLanguageTextMeshController;
            titleButton.updateTextOnHover = true;

			titleObject.SetActive(true);

            // Create Splitscreen Menu Controller

            CreateMenuForDisplay(0);

			titleButton.onClick.AddListener(() =>
			{
				MainMenuController.instance.SetDesiredMenuScreen(SplitscreenMenuController.Singleton);
			});
		}

		//-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a menu for a display
        /// </summary>
        /// <param name="id"></param>
		internal static SplitscreenMenuController CreateMenuForDisplay(int id)
		{
			Log.Print($"[{Plugin.Singleton.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : id = '{id}'");

			try
			{
				GameObject menuPrefab = Resources.LoadAsset<GameObject>("menu.prefab");

				var menu = GameObject.Instantiate(menuPrefab);
				menu.name = $"[Display {id}] MENU: XSplitscreen";
				menu.transform.SetParent(MainMenuController.instance.transform, true);

                var menuController = menu.transform.Find("Splitscreen Menu");

				var canvas = menuController.gameObject.GetComponent<Canvas>();
                canvas.targetDisplay = id;

				var controller = menuController.gameObject.AddComponent<SplitscreenMenuController>();
                controller.id = id;

                if(id != 0)
                {
                    SplitscreenMenuController.Singleton.onEnter.AddListener(() =>
                    {
                        controller.OnEnter(null);
                    });
					SplitscreenMenuController.Singleton.onExit.AddListener(() =>
					{
						controller.OnExit(null);
					});
				}

                return controller;
			}
			catch (Exception e)
			{
				Log.Print(e, Log.ELogChannel.Error);

                return null;
			}
		}
		/*private static void Initialize()
        {
            HookManager.InitializeCustomHookList();
            HookManager.UpdateHooks(HookType.General, true);
        }
        private static bool InitializeTitleButton()
        {
            if (titleButton != null)
                Destroy(titleButton);

            var singlePlayerButton = GameObject.Find("GenericMenuButton (Singleplayer)");

            if (singlePlayerButton == null || MainMenuController.instance == null)
                return false;

            // Create main menu button

            titleButton = Instantiate(XLibrary.Resources.GetPrefabUI("MainMenuButton"));

            titleButton.name = "GenericMenuButton (XSplitScreen)";
            titleButton.transform.SetParent(singlePlayerButton.transform.parent);
            titleButton.transform.SetSiblingIndex(singlePlayerButton.transform.GetSiblingIndex() - 1);
            titleButton.transform.localScale = Vector3.one;

            return true;
        }
        private static bool InitializeUI()
        {
            if (titleButton == null)
                return false;

            // Create assignment screen

            assignmentScreen = Instantiate(XLibrary.Resources.GetPrefabUI("Screen"));
            assignmentScreen.name = "MENU: XSplitScreen";
            assignmentScreen.transform.SetParent(MainMenuController.instance.transform);
            assignmentScreen.transform.localScale = Vector3.one;
            assignmentScreen.SetActive(true);

            GameObject screen = assignmentScreen.transform.GetChild(0).gameObject;
            screen.SetActive(false);
            screen.AddComponent<AssignmentScreen>();
            screen.GetComponent<AssignmentScreen>().Initialize();

            HGButton hgButton = titleButton.GetComponent<HGButton>();
            hgButton.hoverToken = "XSS_NAME_HOVER";
            hgButton.onClick.RemoveAllListeners();
            hgButton.onClick.AddListener(screen.GetComponent<AssignmentScreen>().OpenScreen);

            LanguageTextMeshController langController = titleButton.GetComponent<LanguageTextMeshController>();

            R2API.LanguageAPI.Add("XSS_NAME", PluginName);
            langController.token = "XSS_NAME";

            titleButton.SetActive(true);

            //RoR2.RoR2Application.instance.gameObject.AddComponent<MultiMonitorCameraManager>();
            return true;
        }*/
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Public Methods
		/*internal static void RecreateAssignmentScreen()
        {
            Destroy(assignmentScreen);
            CreateInitializationRoutine();
        }*/
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Event Handlers
        /// <summary>
        /// Try to create the main menu UI
        /// </summary>
		private static void OnMainMenuInitialized()
        {
            if (MainMenuTitleButton != null)
                return;

            UIHelper.Initialize();

            CreateTitleUI();
		}
		/*public static void ActiveSceneChanged(Scene previous, Scene current)
        {
            if (current.name.Equals("title"))
                CreateInitializationRoutine();
        }*/
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Coroutines
		/*internal static IEnumerator WaitToInitializeUI()
        {
            while (!XLibrary.Plugin.uiPrefabsReady)
                yield return null;

            Log.LogOutput($"Initializing UI", Log.LogLevel.Message);

            while (!InitializeTitleButton())
                yield return null;

            while (!InitializeUI())
                yield return null;

            //Log.LogOutput($"WaitToInitializeUI complete");
            initializationCoroutine = null;

            yield return null;
        }*/
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Language
		/*public void InitializeLanguage(TokenConfiguration configuration) { }
        public virtual Dictionary<string, string> GetLanguage()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            dictionary.Add("XSS_NAME_HOVER", "Modify splitscreen settings.");
            dictionary.Add("XSS_OPTION_DISCORD", "Discord");
            dictionary.Add("XSS_OPTION_DISCORD_HOVER", "Join for support, feedback or updates");
            dictionary.Add("XSS_OPTION_RESET_ASSIGNMENTS_HOVER", "Unassign all local players");
            dictionary.Add("XSS_ERROR_MINIMUM", "Minimum of 2 players not met");
            dictionary.Add("XSS_ERROR_PROFILE", "Cannot use the same profile for more than 1 local user");
            dictionary.Add("XSS_ERROR_PROFILE_2", "Not enough profiles exist to add a local user");
            dictionary.Add("XSS_ERROR_CONTROLLER", "Drag a controller here to assign it to a profile");
            dictionary.Add("XSS_ERROR_POSITION", "Position error: Please reset all assignments using the Reset button");
            dictionary.Add("XSS_ERROR_DISPLAY", "Display error: Please reset all assignments using the Reset button");
            dictionary.Add("XSS_ERROR_OTHER", "Unknown error: Please reset all assignments using the Reset button");
            dictionary.Add("XSS_ERROR_SPLITSCREEN_ENABLED", "Disable splitscreen to change assignments");
            dictionary.Add("XSS_ERROR_FIRSTTIME", "Assign profiles and controllers for each monitor here");
            dictionary.Add("XSS_USEROPTION_HUDSCALE", "HUD Scale: {0}%"); // TODO create missing entries
            dictionary.Add("XSS_ENABLE", "Enable");
            dictionary.Add("XSS_DISABLE", "Disable");
            //dictionary.Add("XSS_LEMON_DAMAGE", "+25% damage!");
            //dictionary.Add("XSS_LEMON_SPEED", "+25% speed!");
            //dictionary.Add("XSS_LEMON_HEALTH", "+25% health!");
            //dictionary.Add("XSS_LEMON_SHIELD", "+25% shield!");
            return dictionary;
        }*/
		#endregion

		//-----------------------------------------------------------------------------------------------------------

		#region Patching
		/// <summary>
		/// Update the interactable state of the splitscreen button in the title menu
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="__0"></param>
		private static void SetButtonInteractable(MainMenuController __instance, bool __0)
        {
            MainMenuTitleButton.interactable = __0;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Add players to the Rewired Input Manager
		/// </summary>
		/// <param name="__instance"></param>
		private static void RewiredClonePlayers(InputManager_Base __instance)
		{
            if (__instance.userData.players.Count != 3)
                return;

            var player = __instance.userData.players[2].Clone();

            for (int e = 0; e < 14; e++)
            {
                var clone = player.Clone();
                clone.name = $"Player{e + 3}";
                clone.descriptiveName = clone.name;

				__instance.userData.players.Add(clone);
            }
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Set RoR2's max local player count to 16
		/// </summary>
		/// <param name="__instance"></param>
		private static void RoR2AppResizePlayers(RoR2.RoR2Application __instance)
		{
			typeof(RoR2.RoR2Application).GetField("maxLocalPlayers").SetValue(__instance, 16);
		}

		/*private static void CreateInitializationRoutine()
        {
            if (initializationCoroutine == null)
                initializationCoroutine = instance.StartCoroutine(WaitToInitializeUI());
        }*/
		#endregion
	}
}