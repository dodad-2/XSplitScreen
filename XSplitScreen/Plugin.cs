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
using Dodad.XSplitscreen.Components;
using System.Runtime.CompilerServices;
using System.IO;

namespace Dodad.XSplitscreen
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

		//-----------------------------------------------------------------------------------------------------------

		public static Plugin Singleton { get; private set; }
        public static AssetBundle Resources { get; private set; }
        public static Action OnOpenMenu;

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
		/// Load language file from disk and merge with defaults
		/// </summary>
		private static bool LoadLanguage()
		{
			string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "language.json");

			try
			{
				// Create default language tokens
				MultiDictionary defaultTokens = CreateDefaultLanguageTokens();
				MultiDictionary userTokens = null;

				// Check if custom language file exists
				if (File.Exists(filePath))
				{
					try
					{
						string fileContent = File.ReadAllText(filePath);
						userTokens = JsonConvert.DeserializeObject<MultiDictionary>(fileContent);

						// Validate the loaded data
						if (userTokens == null)
						{
							Log.Print("Language file exists but contains invalid data. Using defaults.", Log.ELogChannel.Warning);
							userTokens = null;
						}
					}
					catch (JsonException jsonEx)
					{
						Log.Print($"Error parsing language file: {jsonEx.Message}. Using defaults.", Log.ELogChannel.Warning);
					}
					catch (IOException ioEx)
					{
						Log.Print($"Error reading language file: {ioEx.Message}. Using defaults.", Log.ELogChannel.Warning);
					}
				}

				// Merge user tokens with defaults or use defaults if no valid user file
				MultiDictionary finalTokens = userTokens != null ?
					MergeLanguageTokens(defaultTokens, userTokens) :
					defaultTokens;

				// Save the complete language file for user reference/editing
				File.WriteAllText(filePath, JsonConvert.SerializeObject(finalTokens, Formatting.Indented));

				// Add tokens to LanguageAPI
				foreach (var language in finalTokens)
				{
					foreach (var token in language.Value)
					{
						LanguageAPI.Add(token.Key, token.Value, language.Key);
					}
				}

				return true;
			}
			catch (Exception e)
			{
				Log.Print($"Fatal error in LoadLanguage: {e.Message}", Log.ELogChannel.Fatal);
				Log.Print(e, Log.ELogChannel.Fatal);
				return false;
			}
		}

		/// <summary>
		/// Creates the default language tokens for all supported languages
		/// </summary>
		private static MultiDictionary CreateDefaultLanguageTokens()
		{
			MultiDictionary tokens = new MultiDictionary();

			// Define shared tokens that will be applied to all languages
			StringDictionary sharedTokens = new StringDictionary
			{
				{ "XSS_NAME", "XSplitscreen" },
				{ "XSS_OPTION_DISCORD", "Discord" },
				{ "XSS_UNSET", "- not set -" },
				{ "XSS_SELECT_SCREEN", "Select Screen" },
				{ "XSS_READY", "READY" },
				{ "XSS_ONLINE", "Online" }
			};

			// English (complete set - reference for other languages)
			string enKey = "en";
			tokens.Add(enKey, new StringDictionary
			{
				{ "XSS_NAME_HOVER", "Modify splitscreen settings." },
				{ "XSS_OPTION_DISCORD_HOVER", "Join for support, feedback or updates" },
				{ "XSS_PRESS_START_KBM", "- click or press start -" },
				{ "XSS_PRESS_START", "- press start -" },
				{ "XSS_CONFIG_PROFILE", "Profile" },
				{ "XSS_CONFIG_COLOR", "Color" },
				{ "XSS_OPTION_MMM", "Multi-Monitor Mode" },
				{ "XSS_OPTION_MMM_HOVER", "Enable Multi-Monitor Mode (cannot be disabled)" }
			});

			// Portuguese (Brazilian)
			string ptBrKey = "pt-br";
			tokens.Add(ptBrKey, new StringDictionary
			{
				{ "XSS_NAME_HOVER", "Modificar as configurações da tela-dividida." },
				{ "XSS_OPTION_DISCORD_HOVER", "Entre para suporte, dar feedback ou ver atualizações" },
				{ "XSS_PRESS_START_KBM", "- clique ou pressione start -" },
				{ "XSS_PRESS_START", "- pressione start -" },
				{ "XSS_CONFIG_PROFILE", "Perfil" },
				{ "XSS_CONFIG_COLOR", "Cor" },
				{ "XSS_OPTION_MMM", "Modo Multi-Monitor" },
				{ "XSS_OPTION_MMM_HOVER", "Ativar o Modo Multi-Monitor (não pode ser desativado)" }
			});

			// French
			string frKey = "fr";
			tokens.Add(frKey, new StringDictionary
			{
				{ "XSS_NAME_HOVER", "Modifier les paramètres de splitscreen." },
				{ "XSS_OPTION_DISCORD_HOVER", "Rejoignez nous pour du support, du feedback ou des mises à jour" },
				{ "XSS_PRESS_START_KBM", "- cliquez ou appuyez sur start -" },
				{ "XSS_PRESS_START", "- appuyez sur start -" },
				{ "XSS_CONFIG_PROFILE", "Profil" },
				{ "XSS_CONFIG_COLOR", "Couleur" },
				{ "XSS_OPTION_MMM", "Mode Multi-Écrans" },
				{ "XSS_OPTION_MMM_HOVER", "Activer le Mode Multi-Écrans (ne peut pas être désactivé)" }
			});

			// Apply shared tokens to all languages
			foreach (var language in tokens)
			{
				foreach (var token in sharedTokens)
				{
					language.Value[token.Key] = token.Value;
				}
			}

			return tokens;
		}

		/// <summary>
		/// Merges user language tokens with default tokens, preserving user customizations
		/// </summary>
		private static MultiDictionary MergeLanguageTokens(MultiDictionary defaultTokens, MultiDictionary userTokens)
		{
			MultiDictionary result = new MultiDictionary();

			// First add all default languages and tokens
			foreach (var language in defaultTokens)
			{
				result.Add(language.Key, new StringDictionary());
				foreach (var token in language.Value)
				{
					result[language.Key].Add(token.Key, token.Value);
				}
			}

			// Then overlay user customizations
			foreach (var language in userTokens)
			{
				// Create language if it doesn't exist in defaults
				if (!result.ContainsKey(language.Key))
				{
					result.Add(language.Key, new StringDictionary());
				}

				// Add or replace tokens with user values
				foreach (var token in language.Value)
				{
					if (result[language.Key].ContainsKey(token.Key))
					{
						result[language.Key][token.Key] = token.Value;
					}
					else
					{
						result[language.Key].Add(token.Key, token.Value);
					}
				}
			}

			return result;
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

			// Testing

			var mpbAOriginal = typeof(RoR2.UI.MPButton).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
			var mpbAPatch = typeof(Plugin).GetMethod("MPButton_Awake", BindingFlags.Static | BindingFlags.NonPublic);

			harmony.Patch(mpbAOriginal, postfix: new HarmonyLib.HarmonyMethod(mpbAPatch));
		}

		//-----------------------------------------------------------------------------------------------------------
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
                OnOpenMenu?.Invoke();
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
                controller.monitorId = id;

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

			if (SplitscreenUserManager.IsSplitscreenEnabled())
				SplitscreenUserManager.DisableSplitscreen();

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

		#region Patching
		private static void MPButton_Awake(RoR2.UI.MPButton __instance)
		{

		}

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