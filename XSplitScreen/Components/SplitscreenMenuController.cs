using Rewired;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace dodad.XSplitscreen.Components
{
	public class SplitscreenMenuController : BaseMainMenuScreen
	{
		public static SplitscreenMenuController Singleton { get; private set; }

		internal static GameObject textPrefab { get; private set; }
		internal static UILayerKey uiLayerKey { get; private set; }

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
		internal int id;

		private bool initialized;

		/// <summary>
		/// Wait 2 frames to invoke Juiced event
		/// </summary>
		private int framesSinceMultiMonitorEnabled = 3;

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

			if (id == 0)
			{
				mainPlayer = LocalUserManager.GetRewiredMainPlayer();
				mainInput = new LocalUserSlot.InputBank();
			}

			gameObject.SetActive(false);
		}

		//-----------------------------------------------------------------------------------------------------------

		public void LateUpdate()
		{
			if (id != 0)
				return;

			framesSinceMultiMonitorEnabled++;
		}

		//-----------------------------------------------------------------------------------------------------------

		public new void Update()
		{
			base.Update();

			if (id != 0)
				return;
			
			mainInput.Update(mainPlayer);

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

			// To handle main user UI interaction, manually set the selected object depending on 
			// the direction of the left stick (up / down) and whether or not a selection exists

			// TODO create an invisible button 

			if (mainInput.Down)
			{
				//Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : Down");

				if (EventSystem.current.currentSelectedGameObject == null)
				{
					//Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : Selecting Discord button");

					if(multiMonitorButton.interactable)
						EventSystem.current.SetSelectedGameObject(multiMonitorButton.gameObject);
					else
						EventSystem.current.SetSelectedGameObject(discordButton.gameObject);
				}
			}
			else if(mainInput.Up) // TODO description text does not disappear on deselect with controller
			{
				//Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : Up");

				if(EventSystem.current.currentSelectedGameObject == multiMonitorButton.gameObject)
				{
					EventSystem.current.SetSelectedGameObject(null);
				}
				else if(EventSystem.current.currentSelectedGameObject == discordButton.gameObject)
				{
					if(multiMonitorButton.interactable)
						EventSystem.current.SetSelectedGameObject(multiMonitorButton.gameObject);
					else
						EventSystem.current.SetSelectedGameObject(null);
				}
			}
			else if(mainInput.East)
			{
				MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
			}

			if(framesSinceMultiMonitorEnabled == 2)
				Singleton.onEnter.Invoke();
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Called when the player clicks on the Multi-Monitor button
		/// </summary>
		public void EnableMultiMonitorMode()
		{
			Log.Print($"[{this.GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : id -> '{id}'");

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

				Plugin.CreateMenuForDisplay(e);

				// Create camera

				var camera = Instantiate(mainCamera);
				camera.name = $"[Display {e}] Camera";

				camera.sceneCam.targetDisplay = e;
				camera.sceneCam.transform.position = Singleton.desiredCameraTransform.position;
				camera.sceneCam.transform.rotation = Singleton.desiredCameraTransform.rotation;

				//menu.OnEnter(MainMenuController.instance);
			}

			EventSystem.current.SetSelectedGameObject(null);

			framesSinceMultiMonitorEnabled = 0;
		}

		//-----------------------------------------------------------------------------------------------------------

		public override void OnEnter(MainMenuController mainMenuController)
		{
			if(!initialized)
				CreateUI();

			if (id != 0)
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
			if (id != 0)
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

			var localUserPanel = mainPanel.Find("Local User Panel");

			localUserPanel.gameObject.AddComponent<LocalUserPanel>().Initialize(id);

			// Assignment panel

			var assignmentPanel = mainPanel.Find("Assignment Panel");

			assignmentPanel.gameObject.AddComponent<AssignmentPanel>().Initialize();

			initialized = true;

			if (id != 0)
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

			textPrefab = new GameObject("SimpleText Prefab", typeof(RectTransform), typeof(HGTextMeshProUGUI));
			textPrefab.SetActive(false);

			var textPrefabHg = textPrefab.GetComponent<HGTextMeshProUGUI>();

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

			var textPrefabLang = textPrefab.AddComponent<LanguageTextMeshController>();

			textPrefabLang.textMeshPro = textPrefabHg;
			textPrefabLang.token = "XSS_UNSET";

			UIHelper.AddPrefab(UIHelper.EUIPrefabIndex.SimpleText, textPrefabLang.gameObject);
		}
	}
}
