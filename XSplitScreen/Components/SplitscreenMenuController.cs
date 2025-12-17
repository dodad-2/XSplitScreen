using Rewired;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dodad.XSplitscreen.Components
{
	public class SplitscreenMenuController : BaseMainMenuScreen
	{
		public static SplitscreenMenuController Singleton { get; private set; }

		internal static GameObject TextPrefab { get; private set; }
		internal static UILayerKey UiLayerKey { get; private set; }
		internal static LocalUserSlot.InputBank MainInput
		{
			get => mainInput;
		}
		public static float LoadTimer
		{
			get => loadTimer;
		}
		public static bool ReadyToLoad
		{
			get => AllowLoad;
		}

		// Buttons and player input references
		private static HGButton discordButton;
		private static HGButton multiMonitorButton;
		private static HGButton backButton;
		private static HGButton creditsButton;
		private static HGButton gameModeButton;
		private static Player mainPlayer;
		private static LocalUserSlot.InputBank mainInput;
		private static CanvasGroup notificationGroup;

		internal int monitorId;

		public Canvas MenuCanvas => _canvas;
		public Camera MenuCamera => _camera;
		private bool hasInitialized;

		private int framesSinceMultiMonitorEnabled = 3; // Wait 2 frames to invoke Juiced event

		private Canvas _canvas;
		private Camera _camera;

		private static float loadTimer;
		private static bool AllowLoad;
		private static bool ShowBackupWarning = true;
		private static float BackupWarningTimer = 20f;
		private const float BackupTimeout = 20f;

		//-----------------------------------------------------------------------------------------------------------

		public new void Awake()
		{
			Log.Print($"[{GetType().Name}.{MethodBase.GetCurrentMethod().Name}] : Alive");

			PrintDebugInfo();

			if (Singleton == null)
			{
				Singleton = this;
				RoR2Application.isInLocalMultiPlayer = SplitscreenUserManager.IsSplitscreenEnabled;
			}
			else
			{
				Singleton.onEnter.AddListener(OnEnterHandler);
			}

			onEnter = new UnityEngine.Events.UnityEvent();
			onExit = new UnityEngine.Events.UnityEvent();

			_canvas = GetComponent<Canvas>();

			var baseType = typeof(BaseMainMenuScreen);
			var fsoType = baseType.Assembly.GetType("FirstSelectedObjectProvider");
			var fsoField = baseType.GetField("firstSelectedObjectProvider", BindingFlags.NonPublic | BindingFlags.Instance);
			var fsoComponent = gameObject.AddComponent(fsoType);
			fsoField.SetValue(this, fsoComponent);

			desiredCameraTransform = transform.parent.Find("World Position").transform;

			var eventProvider = gameObject.AddComponent<MPEventSystemProvider>();
			eventProvider.fallBackToMainEventSystem = true;

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
			if (monitorId != 0) return;
			framesSinceMultiMonitorEnabled++;
		}

		public new void OnEnable()
		{
			base.OnEnable();

			if (monitorId != 0)
				return;
		}

		//-----------------------------------------------------------------------------------------------------------

		public new void Update()
		{
			base.Update();

			if (monitorId != 0) return;

			mainInput.Update(mainPlayer);

			HandleNavigationInput();
			HandleMenuExit();
			HandleLoadGame();
			HandleNotificationUpdate();

			if (framesSinceMultiMonitorEnabled == 2)
				Singleton.onEnter.Invoke();
		}

		public void OnDestroy()
		{
			if(monitorId == 0)
				AssignmentConfigurator.OnClaimUpdated -= OnClaimUpdated;
		}
		
		private void HandleNotificationUpdate()
		{
			if (notificationGroup == null)
				return;

			float groupAlphaTarget = 1;

			if(ShowBackupWarning)
			{
				BackupWarningTimer -= Time.unscaledDeltaTime;

				if (Mathf.Abs(BackupTimeout - BackupWarningTimer) >= 0.4f)
				{
					if (mainInput.MouseLeft || mainInput.South || BackupWarningTimer <= 0)
					{
						ShowBackupWarning = false;
						groupAlphaTarget = 0f;
						LocalUserPanel.AllowChanges = true;
					}
				}
			}
			else
			{
				groupAlphaTarget = 0f;
			}

			if(Mathf.Abs(notificationGroup.alpha - groupAlphaTarget) > 0.01f)
				notificationGroup.alpha = Mathf.MoveTowards(notificationGroup.alpha, groupAlphaTarget, Time.unscaledDeltaTime * 10f);
			else
			{
				if(notificationGroup.alpha < 1)
					Destroy(notificationGroup.gameObject);
			}
		}

		private void HandleNavigationInput()
		{
			if (ShowBackupWarning)
				return;

			var last = mainPlayer.controllers.GetLastActiveController();

			if (last is Keyboard || last is Mouse) return;

			if (mainInput.Down || mainInput.Up)
			{
				EnsureSelectedObject();
			}
		}

		private void HandleMenuExit()
		{
			if (mainPlayer.controllers.GetLastActiveController() is Keyboard || CreditsController.ShowCredits || ShowBackupWarning) 
				return;

			if (mainInput.East)
			{
				MainMenuController.instance.SetDesiredMenuScreen(MainMenuController.instance.titleMenuScreen);
			}
		}

		private void EnsureSelectedObject()
		{
			if (CreditsController.ShowCredits)
				return;

			if (EventSystem.current.currentSelectedGameObject == null)
				EventSystem.current.SetSelectedGameObject(discordButton.gameObject);

			/*if (EventSystem.current.currentSelectedGameObject != null) return;

			EventSystem.current.SetSelectedGameObject(creditsButton.gameObject);*/
			/*if (multiMonitorButton.interactable)
				EventSystem.current.SetSelectedGameObject(multiMonitorButton.gameObject);
			else
				EventSystem.current.SetSelectedGameObject(discordButton.gameObject);*/
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Called when the player clicks on the Multi-Monitor button
		/// </summary>
		public void EnableMultiMonitorMode()
		{
			Log.Print($"EnableMultiMonitorMode: '{Display.displays.Length}' displays");

			int displayCount = Display.displays.Length;
			if (displayCount == 1) return;

			var mainCamera = CameraRigController.instancesList.First();

			for (int i = 1; i < displayCount; i++)
			{
				if (!Display.displays[i].active)
					Display.displays[i].Activate();

				var newMenu = Plugin.CreateMenuForDisplay(i);

				var camera = Instantiate(mainCamera);
				camera.name = $"[Display {i}] Camera";
				camera.sceneCam.targetDisplay = i;
				camera.sceneCam.transform.position = Singleton.desiredCameraTransform.position;
				camera.sceneCam.transform.rotation = Singleton.desiredCameraTransform.rotation;

				newMenu._camera = camera.sceneCam;
			}

			EventSystem.current.SetSelectedGameObject(null);
			framesSinceMultiMonitorEnabled = 0;
		}

		//-----------------------------------------------------------------------------------------------------------

		private void OnEnterHandler()
		{
			OnEnter(Singleton.myMainMenuController);
		}

		public override void OnEnter(MainMenuController mainMenuController)
		{
			if (monitorId == 0)
			{
				SplitscreenUserManager.DisableSplitscreen();
			}

			if (!hasInitialized)
				CreateUI();

			if (monitorId != 0)
			{
				gameObject.SetActive(true);

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

				return;
			}

			if (myMainMenuController == mainMenuController)
				myMainMenuController = null;

			onExit.Invoke();

			SplitScreenSettings.BatchSaveDirtyUsers();
		}

		//-----------------------------------------------------------------------------------------------------------

		private void CreateUI()
		{
			var mainPanel = transform.Find("Main Panel");
			var localUserPanel = mainPanel.Find("User Panel");
			localUserPanel.gameObject.AddComponent<LocalUserPanel>().Initialize(this, monitorId);

			var assignmentPanel = mainPanel.Find("Assignment Panel");
			assignmentPanel.gameObject.AddComponent<AssignmentPanel>().Initialize(this);

			hasInitialized = true;

			notificationGroup = transform.Find("Notification Panel").GetComponent<CanvasGroup>();

			if (monitorId != 0)
			{
				Destroy(notificationGroup.gameObject);

				return;
			}

			var backPanelTemplate = MainMenuController.instance.extraGameModeMenuScreen.transform.Find("Main Panel/BackPanel");
			var menuButtonPanelTemplate = MainMenuController.instance.extraGameModeMenuScreen.transform.Find("Main Panel/GenericMenuButtonPanel");

			if (backPanelTemplate == null || menuButtonPanelTemplate == null) return;

			var backPanelClone = Instantiate(backPanelTemplate.gameObject);
			var backPanelRect = backPanelClone.GetComponent<RectTransform>();
			backPanelRect.SetParent(mainPanel);
			backPanelRect.offsetMax = new Vector2(700, 0);
			backPanelRect.offsetMin = Vector2.zero;
			backPanelRect.transform.localScale = Vector3.one;

			// Back button setup
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

			var backJuice = backButton.transform.parent.GetComponent<UIJuice>();
			onEnter.AddListener(() =>
			{
				backJuice.TransitionAlphaFadeIn();
				backJuice.TransitionPanFromLeft();
			});

			// Menu panel setup
			var menuButtonPanelClone = Instantiate(menuButtonPanelTemplate.gameObject);
			var menuButtonPanelRect = menuButtonPanelClone.GetComponent<RectTransform>();
			menuButtonPanelRect.SetParent(mainPanel);
			menuButtonPanelRect.offsetMax = Vector2.zero;
			menuButtonPanelRect.offsetMin = new Vector2(0, 160);
			menuButtonPanelRect.transform.localScale = Vector3.one;


			// Discord button setup
			discordButton = menuButtonPanelRect.Find("JuicePanel/GenericMenuButton (Infinite Tower)").GetComponent<HGButton>();
			discordButton.name = "Discord";
			UIHelper.ClearHGButton(discordButton);
			discordButton.GetComponentInChildren<LanguageTextMeshController>().token = "XSS_OPTION_DISCORD";
			discordButton.hoverLanguageTextMeshController = menuButtonPanelRect.Find("JuicePanel/DescriptionPanel, Naked/ContentSizeFitter/DescriptionText").GetComponent<LanguageTextMeshController>();
			discordButton.hoverToken = "XSS_OPTION_DISCORD_HOVER";
			discordButton.updateTextOnHover = true;
			discordButton.GetComponent<MPEventSystemLocator>().Awake();
			discordButton.onClick.AddListener(() =>
			{
				Application.OpenURL("https://discord.gg/maHhJSv62G");
			});

			// Remove extra buttons
			foreach (Transform child in menuButtonPanelRect.Find("JuicePanel"))
			{
				if(child.name == "Discord" || child.name == "DescriptionPanel, Naked")
					continue;
				Log.Print($"Destroying extra button: {child.name}", Log.ELogChannel.Debug);
				Destroy(child.gameObject);
			}

			var gameModeButtonTemplate = MainMenuController.instance.multiplayerMenuScreen.transform.Find("Inner90/MainMultiplayerMenu/GenericMenuButtonPanel/JuicePanel/GameMode");
			gameModeButton = Instantiate((gameModeButtonTemplate.gameObject)).GetComponent<HGButton>();
			gameModeButton.hoverLanguageTextMeshController = menuButtonPanelRect.Find("JuicePanel/DescriptionPanel, Naked/ContentSizeFitter/DescriptionText").GetComponent<LanguageTextMeshController>();
			gameModeButton.transform.SetParent(discordButton.transform.parent);
			gameModeButton.transform.localScale = Vector3.one;
			gameModeButton.transform.SetSiblingIndex(0);
			gameModeButton.name = "GameMode";
			gameModeButton.hoverToken = "XSS_OPTION_GM_HOVER";
			gameModeButton.GetComponent<MPEventSystemLocator>().Awake();
			Destroy(gameModeButton.transform.Find("Canvas").gameObject);
			
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

			int displayCount = Display.displays.Length;
			multiMonitorButton.interactable = displayCount != 1 && !Enumerable.Range(1, displayCount - 1).Any(i => Display.displays[i].active);

			if (!multiMonitorButton.interactable && displayCount != 1)
			{
				EnableMultiMonitorMode();
			}

			// Credits button setup
			creditsButton = Instantiate(discordButton.gameObject).GetComponent<HGButton>();
			creditsButton.transform.SetParent(discordButton.transform.parent);
			creditsButton.transform.localScale = Vector3.one;
			creditsButton.transform.SetSiblingIndex(0);
			creditsButton.GetComponentInChildren<LanguageTextMeshController>().token = "XSS_CREDITS";
			creditsButton.name = "Credits";
			creditsButton.hoverToken = "XSS_CREDITS_HOVER";
			creditsButton.GetComponent<MPEventSystemLocator>().Awake();
			creditsButton.onClick.RemoveAllListeners();
			creditsButton.onClick.AddListener(() =>
			{
				if (!CreditsController.ShowCredits)
				{
					ExecuteNextFrame.Invoke(() =>
					{
						CreditsController.ShowCredits = true;
					});

					EventSystem.current.SetSelectedGameObject(null);
				}
			});

			// Link buttons navigation
			var creditsNav = creditsButton.navigation;
			creditsNav.selectOnDown = multiMonitorButton;
			creditsNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;

			var multiMonitorNav = multiMonitorButton.navigation;
			multiMonitorNav.selectOnUp = creditsButton;
			multiMonitorNav.selectOnDown = discordButton;
			multiMonitorNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;

			var discordNav = discordButton.navigation;
			discordNav.selectOnUp = multiMonitorButton;
			discordNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
			/*discordNav.selectOnDown = backButton;

			var backNav = backButton.navigation;
			backNav.selectOnUp = discordButton;*/

			creditsButton.navigation = creditsNav;
			multiMonitorButton.navigation = multiMonitorNav;
			discordButton.navigation = discordNav;
			//backButton.navigation = backNav;

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
			textPrefabHg.fontSizeMax = textTemplate.fontSizeMax;
			textPrefabHg.fontSizeMin = textTemplate.fontSizeMin;
			textPrefabHg.fontSize = textTemplate.fontSize;
			textPrefabHg.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
			textPrefabHg.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;

			var textPrefabLang = TextPrefab.AddComponent<LanguageTextMeshController>();
			textPrefabLang.textMeshPro = textPrefabHg;
			textPrefabLang.token = "XSS_UNSET";

			UIHelper.AddPrefab(UIHelper.EUIPrefabIndex.SimpleText, textPrefabLang.gameObject);

			AssignmentConfigurator.OnClaimUpdated += OnClaimUpdated;

			// Backup warning

			if (ShowBackupWarning)
			{
				var warningText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);
				warningText.GetComponentInChildren<LanguageTextMeshController>().token = "XSS_WARN";
				var warnLayout = warningText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
				var warnRect = warningText.gameObject.GetComponent<RectTransform>();
				warnRect.anchorMax = new Vector2(0.8f, 1f);
				warnRect.anchorMin = new Vector2(0.2f, 0f);
				//warnLayout.GetComponent<RectTransform>().sizeDelta = new Vector2(512, 200);
				warningText.transform.SetParent(notificationGroup.transform.Find("Content").transform);
				warningText.transform.localScale = Vector3.one;
				warningText.gameObject.SetActive(true);

				var phase1 = notificationGroup.transform.Find("W1").gameObject.AddComponent<PhasingGraphicColor>();
				phase1.phaseOffset = 1;

				notificationGroup.transform.Find("W2").gameObject.AddComponent<PhasingGraphicColor>();

				/*var notificationPanel = transform.Find("Notification Panel").transform;

				var infoContainer = new GameObject("Info");
				infoContainer.transform.SetParent(notificationPanel);
				var infoRect = infoContainer.AddComponent<RectTransform>();
				infoRect.transform.localPosition = Vector3.zero;
				infoRect.transform.localScale = Vector3.one;
				infoRect.anchorMin = Vector2.zero;
				infoRect.anchorMax = Vector2.one;

				var infoLayout = infoContainer.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
				infoLayout.childAlignment = TextAnchor.MiddleCenter;
				infoLayout.childForceExpandWidth = false;
				infoLayout.childForceExpandHeight = false;
				infoLayout.childControlWidth = false;
				infoLayout.childControlHeight = false;
				infoLayout.spacing = 20f;

				var warningTexture = Plugin.Resources.LoadAsset<Texture2D>("warning.png");
				var warningSprite = Sprite.Create(warningTexture, new Rect(0, 0, warningTexture.width, warningTexture.height), new Vector2(0.5f, 0.5f));

				var image1Object = new GameObject("Image1");
				var image1Image = image1Object.AddComponent<UnityEngine.UI.Image>();
				image1Image.sprite = warningSprite;
				var image1Rect = image1Object.GetComponent<RectTransform>();
				image1Rect.sizeDelta = new Vector2(65f, 65f);
				image1Rect.SetParent(infoContainer.transform);
				var phase1 = image1Rect.gameObject.AddComponent<PhasingGraphicColor>();
				phase1.phaseOffset = 1;

				var warningText = UIHelper.GetPrefab(UIHelper.EUIPrefabIndex.SimpleText);
				warningText.GetComponentInChildren<LanguageTextMeshController>().token = "XSS_WARN";
				var warnLayout = warningText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
				warnLayout.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 200);
				warningText.transform.SetParent(infoContainer.transform);
				warningText.transform.localScale = Vector3.one;
				warningText.gameObject.SetActive(true);

				var image2Object = new GameObject("Image2");
				var image2Image = image2Object.AddComponent<UnityEngine.UI.Image>();
				image2Image.sprite = warningSprite;
				var image2Rect = image2Object.GetComponent<RectTransform>();
				image2Rect.sizeDelta = new Vector2(65f, 65f);
				image2Rect.SetParent(infoContainer.transform);
				image2Rect.gameObject.AddComponent<PhasingGraphicColor>();

				notificationPanel.transform.SetAsLastSibling();
				notificationPanel.gameObject.SetActive(true);*/

				LocalUserPanel.AllowChanges = false;
			}
			else
			{
				Destroy(notificationGroup.gameObject);
			}

			// Credits

			transform.Find("Credits Panel").gameObject.AddComponent<CreditsController>();
		}

		public void OnClaimUpdated(bool state)
		{
			if (state)
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

			loadTimer = 5f;
		}

		public void HandleLoadGame()
		{
			if (!AllowLoad) return;

			if (loadTimer > 0f)
			{
				loadTimer -= Time.unscaledDeltaTime;
				return;
			}

			SplitScreenSettings.BatchSaveDirtyUsers();

			var users = LocalUserSlot.Instances.Where(x => x.LocalPlayer != null)
				.OrderBy(x => !x.IsKeyboardUser)
				.ToArray();

			List<UserAssignmentData> assignments = new();

			var controllers = users[users.Length - 1].LocalPlayer.controllers.Controllers.ToList();

			// Shift all input players down
			for (int i = users.Length - 1; i > 0; i--)
			{
				var nextControllers = users[i - 1].LocalPlayer.controllers.Controllers.ToList();

				users[i].LocalPlayer = users[i - 1].LocalPlayer;
				users[i].LocalPlayer.controllers.ClearAllControllers();

				foreach (var controller in controllers)
					users[i].LocalPlayer.controllers.AddController(controller, false);

				controllers = nextControllers;
			}

			// Assign PlayerMain to first slot
			users[0].LocalPlayer = ReInput.players.GetPlayer("PlayerMain");
			foreach (var controller in controllers)
				users[0].LocalPlayer.controllers.AddController(controller, false);

			// Prepare assignments
			for (int i = 0; i < users.Length; i++)
			{
				assignments.Add(new UserAssignmentData
				{
					Profile = users[i].Profile ?? PlatformSystems.saveSystem.CreateGuestProfile(),
					UserIndex = i,
					InputPlayer = users[i].LocalPlayer,
					CameraRect = users[i].ScreenRect,
					Display = users[i].Panel.Controller.monitorId
				});
			}

			SplitscreenUserManager.InitializeUsers(assignments);

			AllowLoad = false;
			
			CarouselController carousel = gameModeButton.GetComponent<CarouselController>();
			var gamemodeValue= carousel.GetCurrentValue();
			SplitscreenUserManager.EnableSplitscreen(gamemodeValue);
		}

		public static void PrintDebugInfo()
		{
			Log.Print($"Displays:");
			foreach (var d in Display.displays)
				Log.Print($" - '{d}', '{d.active}', '{d.systemWidth} x {d.systemHeight}'");

			Log.Print($"Controllers:");
			foreach (var c in ReInput.controllers.Controllers)
				Log.Print($" - '{c.id}', '{c.name}', '{c.type}', '{c.hardwareName}', '{c.inputSource}', '{c.hardwareIdentifier}', '{c.hardwareTypeGuid}'");
		}
	}
}