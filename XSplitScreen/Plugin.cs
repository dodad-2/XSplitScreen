using BepInEx;
using DoDad.XLibrary.Interfaces;
using DoDad.XLibrary.Language;
using DoDad.XSplitScreen.Components;
using R2API.Utils;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoDad.XSplitScreen
{
    [BepInDependency(XLibrary.Plugin.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class Plugin : BaseUnityPlugin, ILanguage
    {
        #region Variables
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "com.DoDad";
        public const string PluginName = "XSplitScreen";
        public const string PluginVersion = "3.1.3";
        public const string PluginTag = "XSS";
        public const string PluginBundle = "xsplitscreenbundle";
        public const bool developerMode = true;
        public const bool clearAssignmentsOnStart = false;
        public const bool logModeOverrideToAll = false;
        public static bool active => UserManager.localUsers.Count > 0;
        public static string bundleKey { get; private set; }
        private static GameObject assignmentScreen;
        private static GameObject titleButton;
        private static Coroutine initializationCoroutine;
        internal static Plugin instance { get; private set; }
        #endregion

        #region Mono Methods
        public void Awake()
        {
            instance = this;

            Log.Init(Logger);

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
            }
        }
        #endregion

        #region Initialization
        private static void Initialize()
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
        }
        #endregion

        #region Public Methods
        internal static void RecreateAssignmentScreen()
        {
            Destroy(assignmentScreen);
            CreateInitializationRoutine();
        }
        #endregion

        #region Event Handlers
        public static void ActiveSceneChanged(Scene previous, Scene current)
        {
            if (current.name.Equals("title"))
                CreateInitializationRoutine();
        }
        #endregion

        #region Coroutines
        internal static IEnumerator WaitToInitializeUI()
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
        }
        #endregion

        #region Language
        public void InitializeLanguage(TokenConfiguration configuration) { }
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
        }
        #endregion

        #region Helpers
        private static void CreateInitializationRoutine()
        {
            if (initializationCoroutine == null)
                initializationCoroutine = instance.StartCoroutine(WaitToInitializeUI());
        }
        #endregion
    }
}