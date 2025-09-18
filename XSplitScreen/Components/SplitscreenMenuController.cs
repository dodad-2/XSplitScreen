using Rewired;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dodad.XSplitscreen.Components
{
	public class SplitscreenMenuController : BaseMainMenuScreen
	{
		public static SplitscreenMenuController Singleton { get; private set; }

		internal static GameObject TextPrefab { get; private set; }
		internal static UILayerKey UiLayerKey { get; private set; }

		/// <summary>
		/// This field is null checked during menu enter and if true will go back to the main menu
		/// </summary>
		private static HGButton discordButton;
		private static HGButton multiMonitorButton;
		private static HGButton backButton;
		private static Player mainPlayer;
		private static LocalUserSlot.InputBank mainInput;

		/// <summary>
		/// Monitor device id
		/// </summary>
		internal int monitorId;

		public Canvas MenuCanvas => _canvas;
		public Camera MenuCamera => _camera;
		private bool hasInitialized;

		/// <summary>
		/// Wait 2 frames to invoke Juiced event
		/// </summary>
		private int framesSinceMultiMonitorEnabled = 3;
		private Canvas _canvas;
		private Camera _camera;

		//-----------------------------------------------------------------------------------------------------------

		public new void Awake()
		{
			Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : Alive");

			if (Singleton == null)
				Singleton = this;
			else
				Singleton.onEnter.AddListener(() =>
				{
					OnEnter(Singleton.myMainMenuController);
				});

			onEnter = new UnityEngine.Events.UnityEvent();
			onExit = new UnityEngine.Events.UnityEvent();

			_canvas = GetComponent<Canvas>();

			// FirstSelectedObjectProvider is defined in 2 assemblies
			// Use Reflection to set the field value

			var thisType = typeof(BaseMainMenuScreen);
			var fsoType = thisType.Assembly.GetType("FirstSelectedObjectProvider");
			var fsoField = thisType.GetField("firstSelectedObjectProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var fsoComponent = gameObject.AddComponent(fsoType);

			fsoField.SetValue(this, fsoComponent);

			desiredCameraTransform = transform.parent.Find("World Position").transform;

			// Add required components

			var eventProvider = gameObject.AddComponent<MPEventSystemProvider>();
			eventProvider.fallBackToMainEventSystem = true;

			/*var eventLocator = gameObject.AddComponent<MPEventSystemLocator>(); // Disabled 24-12-24
			eventLocator.eventSystemProvider = eventProvider;
			eventLocator.Awake();

			// Layer key

			uiLayerKey = gameObject.AddComponent<UILayerKey>();
			uiLayerKey.layer = ScriptableObject.CreateInstance<UILayer>();
			uiLayerKey.layer.priority = 10;
			uiLayerKey.eventSystemLocator = eventLocator;
			uiLayerKey.onBeginRepresentTopLayer = new UnityEngine.Events.UnityEvent();
			uiLayerKey.onEndRepresentTopLayer = new UnityEngine.Events.UnityEvent();
			uiLayerKey.Awake();*/

			if (monitorId == 0)
			{
				mainPlayer = LocalUserManager.GetRewiredMainPlayer();
				mainInput = new LocalUserSlot.InputBank();
				_camera = Camera.main;
			}

			gameObject.SetActive(false);
		}

		//-----------------------------------------------------------------------------------------------------------

		public void LateUpdate()
		{
			if (monitorId != 0)
				return;

			framesSinceMultiMonitorEnabled++;
		}

		//-----------------------------------------------------------------------------------------------------------
		public new void Update()
		{
			base.Update();

			// Only process UI navigation for monitor 0
			if (monitorId != 0)
				return;

			/*if (mainPlayer.GetButton(7))
				Log.Print("Button 7"); 
			if (mainPlayer.GetButton(8))
				Log.Print("Button 8");
			if (mainPlayer.GetButton(9))
				Log.Print("Button 9");
			if (mainPlayer.GetButton(10))
				Log.Print("Button 10");
			if (mainPlayer.GetButton(5))
				Log.Print("Button 5");
			if (mainPlayer.GetButton(6))
				Log.Print("Button 6");
			if (mainPlayer.GetButton(28))
				Log.Print("Button 28");*/


			// TODO create an invisible button 
			mainInput.Update(mainPlayer);

			HandleNavigationInput();
			HandleMenuExit();
			HandleLoadGame();

			// Trigger onEnter event after enabling multi-monitor (delayed by two frames)
			if (framesSinceMultiMonitorEnabled == 2)
				Singleton.onEnter.Invoke();
		}

		private void HandleNavigationInput()
		{
			if (mainPlayer.controllers.GetLastActiveController() is Keyboard)
				return;

			if (mainInput.Down)
			{
				SelectInitialButtonIfNoneSelected();
			}
			else if (mainInput.Up)
			{
				HandleUpNavigation();
			}
		}

		private void HandleMenuExit()
		{
			if (mainPlayer.controllers.GetLastActiveController() is Keyboard)
				return;

			if (mainInput.East)
			{
				Log.Print($"LastActiveController = '{mainPlayer.controllers.GetLastActiveController()}'");
				MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
			}
		}

		private void SelectInitialButtonIfNoneSelected()
		{
			if (EventSystem.current.currentSelectedGameObject == null)
			{
				if (multiMonitorButton.interactable)
					EventSystem.current.SetSelectedGameObject(multiMonitorButton.gameObject);
				else
					EventSystem.current.SetSelectedGameObject(discordButton.gameObject);
			}
		}

		private void HandleUpNavigation()
		{
			var selected = EventSystem.current.currentSelectedGameObject;
			if (selected == multiMonitorButton.gameObject)
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
			else if (selected == discordButton.gameObject)
			{
				if (multiMonitorButton.interactable)
					EventSystem.current.SetSelectedGameObject(multiMonitorButton.gameObject);
				else
					EventSystem.current.SetSelectedGameObject(null);
			}
		}

		//-----------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the player clicks on the Multi-Monitor button
		/// </summary>
		public void EnableMultiMonitorMode()
		{
			Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : id -> '{monitorId}'");

			int displayCount = Display.displays.Length;

			if (displayCount == 1)
				return;

			var mainCamera = CameraRigController.instancesList.First();

			for (int e = 0; e < displayCount; e++)
			{
				if (e == 0)
					continue;

				if (!Display.displays[e].active)
					Display.displays[e].Activate();

				var newMenu = Plugin.CreateMenuForDisplay(e);

				// Create camera

				var camera = Instantiate(mainCamera);
				camera.name = $"[Display {e}] Camera";

				camera.sceneCam.targetDisplay = e;
				camera.sceneCam.transform.position = Singleton.desiredCameraTransform.position;
				camera.sceneCam.transform.rotation = Singleton.desiredCameraTransform.rotation;

				newMenu._camera = camera.sceneCam;
			}

			EventSystem.current.SetSelectedGameObject(null);

			framesSinceMultiMonitorEnabled = 0;
		}

		//-----------------------------------------------------------------------------------------------------------

		public override void OnEnter(MainMenuController mainMenuController)
		{
			if(!hasInitialized)
				CreateUI();

			if (monitorId != 0)
			{
				gameObject.SetActive(true);

				// TODO camera position

				return;
			}

			myMainMenuController = mainMenuController;

			if (SimpleDialogBox.instancesList.Count == 0)
				firstSelectedObjectProvider?.EnsureSelectedObject();

			onEnter.Invoke();

			if (discordButton == null)
			{
				Log.Print("Unable to create Splitscreen menu. Please post the log in the Discord server: https://discord.gg/maHhJSv62G", Log.ELogChannel.Error);

				MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		public override void OnExit(MainMenuController mainMenuController)
		{
			if (monitorId != 0)
			{
				gameObject.SetActive(false);

				// TODO camera position

				return;
			}

			if (myMainMenuController == mainMenuController)
				myMainMenuController = null;

			onExit.Invoke();
		}

		//-----------------------------------------------------------------------------------------------------------

		private void CreateUI()
		{
			// Create lower left buttons

			var mainPanel = transform.Find("Main Panel");

			// Local user panel

			var localUserPanel = mainPanel.Find("User Panel");

			localUserPanel.gameObject.AddComponent<LocalUserPanel>().Initialize(this, monitorId);
			
			// Assignment panel

			var assignmentPanel = mainPanel.Find("Assignment Panel");

			assignmentPanel.gameObject.AddComponent<AssignmentPanel>().Initialize();

			hasInitialized = true;

			if (monitorId != 0)
				return;

			// Back panel

			var backPanelTemplate = MainMenuController.instance.extraGameModeMenuScreen.transform.Find("Main Panel/BackPanel");
			var menuButtonPanelTemplate = MainMenuController.instance.extraGameModeMenuScreen.transform.Find("Main Panel/GenericMenuButtonPanel");

			if (backPanelTemplate == null ||
				menuButtonPanelTemplate == null)
				return;

			var backPanelClone = GameObject.Instantiate(backPanelTemplate.gameObject);

			var backPanelRectTransform = backPanelClone.GetComponent<RectTransform>();

			backPanelRectTransform.SetParent(mainPanel);

			backPanelRectTransform.offsetMax = new Vector2(700, 0);
			backPanelRectTransform.offsetMin = Vector2.zero;
			backPanelRectTransform.transform.localScale = Vector3.one;

			// Back button

			backButton = backPanelClone.transform.Find("ButtonPanel (JUICED)/Button, Return").GetComponent<HGButton>();

			var backHoverToken = backButton.hoverToken;

			UIHelper.ClearHGButton(backButton, false);

			backButton.hoverToken = backHoverToken;
			backButton.onClick.AddListener(() =>
			{
				MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
				backButton.OnClickCustom();
			});

			Destroy(backButton.GetComponent<DisableIfNoExpansion>());

			backButton.GetComponent<MPEventSystemLocator>().Awake();

			// Juice

			var backJuice = backButton.transform.parent.GetComponent<UIJuice>();

			onEnter.AddListener(() =>
			{
				backJuice.TransitionAlphaFadeIn();
				backJuice.TransitionPanFromLeft();
			});

			// Menu panel

			var menuButtonPanelClone = GameObject.Instantiate(menuButtonPanelTemplate.gameObject);

			var menuButtonPanelRectTransform = menuButtonPanelClone.GetComponent<RectTransform>();

			menuButtonPanelRectTransform.SetParent(mainPanel);

			menuButtonPanelRectTransform.offsetMax = Vector2.zero;
			menuButtonPanelRectTransform.offsetMin = new Vector2(0, 160);
			menuButtonPanelRectTransform.transform.localScale = Vector3.one;

			// Delete extra buttons

			GameObject.Destroy(menuButtonPanelRectTransform.Find("JuicePanel/GenericMenuButton (Weekly)").gameObject);
			GameObject.Destroy(menuButtonPanelRectTransform.Find("JuicePanel/GenericMenuButton (Eclipse)").gameObject);

			// Discord button

			discordButton = menuButtonPanelRectTransform.Find("JuicePanel/GenericMenuButton (Infinite Tower)").GetComponent<HGButton>();
			discordButton.name = "Discord";

			UIHelper.ClearHGButton(discordButton);

			discordButton.GetComponentInChildren<LanguageTextMeshController>().token = "XSS_OPTION_DISCORD";

			discordButton.hoverLanguageTextMeshController = menuButtonPanelRectTransform.Find("JuicePanel/DescriptionPanel, Naked/ContentSizeFitter/DescriptionText").GetComponent<LanguageTextMeshController>();
			discordButton.hoverToken = "XSS_OPTION_DISCORD_HOVER";
			discordButton.updateTextOnHover = true;
			discordButton.GetComponent<MPEventSystemLocator>().Awake();

			discordButton.onClick.AddListener(() =>
			{
				Application.OpenURL("https://discord.gg/maHhJSv62G");
			});

			// Multi monitor mode

			multiMonitorButton = Instantiate(discordButton.gameObject).GetComponent<HGButton>();
			multiMonitorButton.transform.SetParent(discordButton.transform.parent);
			multiMonitorButton.transform.localScale = Vector3.one;
			multiMonitorButton.transform.SetSiblingIndex(0);

			multiMonitorButton.GetComponentInChildren<LanguageTextMeshController>().token = "XSS_OPTION_MMM";

			multiMonitorButton.name = "Multi Monitor Mode";
			multiMonitorButton.hoverToken = "XSS_OPTION_MMM_HOVER";
			multiMonitorButton.GetComponent<MPEventSystemLocator>().Awake();

			multiMonitorButton.onClick.RemoveAllListeners();
			multiMonitorButton.onClick.AddListener(() =>
			{
				EnableMultiMonitorMode();
				multiMonitorButton.interactable = false;
			});

			// Check if displays are already activated and if so disable the button or create additional menus

			int displayCount = Display.displays.Length;

			if (displayCount != 1)
			{
				for (int e = 1; e < displayCount; e++)
				{
					if (Display.displays[e].active)
					{
						multiMonitorButton.interactable = false;
						break;
					}
				}
			}
			else
				multiMonitorButton.interactable = false;

			if(!multiMonitorButton.interactable && 
				displayCount != 1)
			{
				EnableMultiMonitorMode();
			}

			// Link buttons

			var multiMonitorNavigation = multiMonitorButton.navigation;
			multiMonitorNavigation.selectOnDown = discordButton;

			var discordNavigation = discordButton.navigation;
			discordNavigation.selectOnDown = backButton;
			discordNavigation.selectOnUp = multiMonitorButton;

			var backNavigation = backButton.navigation;
			backNavigation.selectOnUp = discordButton;

			multiMonitorButton.navigation = multiMonitorNavigation;
			discordButton.navigation = discordNavigation;
			backButton.navigation = backNavigation;

			// Text prefab

			TextPrefab = new GameObject("SimpleText Prefab", typeof(RectTransform), typeof(HGTextMeshProUGUI));
			TextPrefab.SetActive(false);

			var textPrefabHg = TextPrefab.GetComponent<HGTextMeshProUGUI>();

			var textTemplate = backButton.GetComponentInChildren<HGTextMeshProUGUI>();

			textPrefabHg.font = textTemplate.font;
			textPrefabHg.color = textTemplate.color;
			textPrefabHg.material = textTemplate.material;
			textPrefabHg.colorGradient = textTemplate.colorGradient;
			textPrefabHg.fontSharedMaterial = textTemplate.fontSharedMaterial;
			textPrefabHg.fontSizeMax =	textTemplate.fontSizeMax;
			textPrefabHg.fontSizeMin = textTemplate.fontSizeMin;
			textPrefabHg.fontSize = textTemplate.fontSize;
			textPrefabHg.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
			textPrefabHg.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;

			var textPrefabLang = TextPrefab.AddComponent<LanguageTextMeshController>();

			textPrefabLang.textMeshPro = textPrefabHg;
			textPrefabLang.token = "XSS_UNSET";

			UIHelper.AddPrefab(UIHelper.EUIPrefabIndex.SimpleText, textPrefabLang.gameObject);

			AssignmentConfigurator.OnClaimUpdated += OnClaimUpdated;
		}

		public void OnClaimUpdated(bool state)
		{
			if(state)
			{
				var users = AssignmentConfigurator.Instances.Where(x => x.HasUser).ToList();

				if (users.Count > 1 && users.TrueForAll(x => x.IsReady))
				{
					AllowLoad = state;
				}
			}
			else
			{
				AllowLoad = false;
			}

			LoadTimer = 5f;
		}

		private static float LoadTimer;
		private static bool AllowLoad;
		private static bool didLoad;

		public void HandleLoadGame()
		{
			if (AllowLoad)
			{
				if (LoadTimer > 0f)
				{
					LoadTimer -= Time.unscaledDeltaTime;
				}
				else
				{
					var users = LocalUserSlot.Instances.Where(x => x.LocalPlayer != null).ToList().OrderBy(x => !x.IsKeyboardUser).ToArray();
					List<UserAssignmentData> assignments = new();

					// Get controllers of next player to be applied on the next iteration
					var controllers = users[users.Length - 1].LocalPlayer.controllers.Controllers.ToList();

					// Shift all input players down

					for (int e = users.Length - 1; e > 0; e--)
					{
						Log.Print($"User '{users[e].name}' with index '{e}'");
						Log.Print($"Current LocalPlayer: '{users[e].LocalPlayer.name}', previous '{users[e - 1].LocalPlayer.name}'");
						var nextControllers = users[e - 1].LocalPlayer.controllers.Controllers.ToList();

						users[e].LocalPlayer = users[e - 1].LocalPlayer;
						users[e].LocalPlayer.controllers.ClearAllControllers();

						foreach (var controller in controllers)
							users[e].LocalPlayer.controllers.AddController(controller, false);

						controllers = nextControllers;
					}

					// Assign PlayerMain

					users[0].LocalPlayer = ReInput.players.GetPlayer("PlayerMain");

					foreach (var controller in controllers)
						users[0].LocalPlayer.controllers.AddController(controller, false);

					for (int e = 0; e < users.Length; e++)
					{
						var userData = new UserAssignmentData()
						{
							Profile = users[e].Profile,
							UserIndex = e,
							InputPlayer = users[e].LocalPlayer,
						};

						assignments.Add(userData);
					}

					SplitscreenUserManager.InitializeUsers(assignments);

					AllowLoad = false;

					SplitscreenUserManager.EnableSplitscreen();

					LoadTimer = 5f;

					didLoad = true;
					// load new scene
				}
			}
			else if(didLoad)
			{
				if(LoadTimer > 0)
				{
					LoadTimer -= Time.deltaTime;
				}
				else
				{
					didLoad = false;

					//SplitscreenUserManager.DisableSplitscreen();
				}
			}
		}
	}
}
