using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace dodad.XSplitscreen
{
	/// <summary>
	/// Formats and stores useful UI prefabs. This code was copied over from the 
	/// previous version of the mod and may contain issues
	/// </summary>
	internal static class UIHelper
	{
		private static Dictionary<EUIPrefabIndex, GameObject> prefabs;
		private static GameObject prefabRoot;

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Generate templates for common UI elements
		/// </summary>
		internal static void Initialize()
		{
			prefabs = new Dictionary<EUIPrefabIndex, GameObject>();
			prefabRoot = new GameObject("XSplitscreen UI Prefabs", typeof(RectTransform));
			prefabRoot.SetActive(false);

			// Main menu button

			GameObject mainMenuButtonTemplate = GameObject.Find("GenericMenuButton (Singleplayer)");
			GameObject mainMenuScreenTemplate = MainMenuController.instance.extraGameModeMenuScreen.gameObject;

			if (mainMenuButtonTemplate == null || mainMenuScreenTemplate == null)
				return;

			GameObject mainMenuButtonPrefab = GameObject.Instantiate(mainMenuButtonTemplate);
			mainMenuButtonPrefab.name = "MainMenuButton";
			mainMenuButtonPrefab.transform.localScale = Vector3.one;

			GameObject.Destroy(mainMenuButtonPrefab.GetComponent<RoR2.ConsoleFunctions>());

			mainMenuButtonPrefab.GetComponents<ViewableTag>().ToList().ForEach(x => GameObject.Destroy(x));

			HGButton button = mainMenuButtonPrefab.GetComponent<HGButton>();

			ClearHGButton(button);

			AddPrefab(EUIPrefabIndex.MainMenuButton, mainMenuButtonPrefab);

			/*// Menu screen

			var mainMenuScreen = GameObject.Instantiate(mainMenuScreenTemplate);
			mainMenuScreen.name = "Screen";

			var screenJuicePanel = mainMenuScreen.transform.GetChild(0).GetChild(0).GetChild(0);

			var screenOption = GameObject.Instantiate(screenJuicePanel.GetChild(1).gameObject);

			GameObject.Destroy(screenJuicePanel.GetChild(1).gameObject);
			GameObject.Destroy(screenJuicePanel.GetChild(2).gameObject);
			GameObject.Destroy(screenJuicePanel.GetChild(3).gameObject);

			GameObject.Destroy(mainMenuScreen.GetComponentInChildren<BaseMainMenuScreen>());

			AddPrefab(EUIPrefabIndex.Screen, mainMenuScreen);

			// Screen option

			var screenOptionHGButton = screenOption.GetComponent<HGButton>();

			ClearHGButton(screenOptionHGButton);

			screenOption.name = "Option";
			GameObject.Destroy(screenOption.GetComponent<RoR2.DisableIfGameModded>());
			GameObject.Destroy(screenOption.GetComponent<RoR2.ConsoleFunctions>());

			AddPrefab(EUIPrefabIndex.ScreenOption, screenOption);

			// SimpleImage

			GameObject simpleImage = new GameObject("SimpleImage", typeof(RectTransform), typeof(UnityEngine.UI.Image));

			AddPrefab(EUIPrefabIndex.SimpleImage, simpleImage, false);

			// SimpleText

			GameObject simpleText = ((SubmenuMainMenuScreen) MainMenuController.instance.settingsMenuScreen).submenuPanelPrefab.transform.GetChild(3).GetChild(2).GetChild(5).GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(3).GetChild(3).gameObject;
			simpleText.name = "SimpleText";
			simpleText.transform.SetParent(prefabRoot.transform);

			AddPrefab(EUIPrefabIndex.SimpleText, simpleText, false);

			// RightArrow

			GameObject rightArrow = GameObject.Instantiate(((SubmenuMainMenuScreen) MainMenuController.instance.settingsMenuScreen).submenuPanelPrefab.transform.GetChild(3).GetChild(2).GetChild(5).GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(3).GetChild(1).gameObject);

			var rightArrowButton = rightArrow.GetComponent<HGButton>();

			ClearHGButton(rightArrowButton);

			AddPrefab(EUIPrefabIndex.RightArrow, rightArrow);

			// SimpleButton

			var simpleHG = GameObject.Instantiate(simpleImage, prefabRoot.transform);
			simpleHG.name = "SimpleButton";
			simpleHG.AddComponent<MPEventSystemLocator>();
			simpleHG.AddComponent<HGButton>();

			var simpleButton = simpleHG.GetComponent<HGButton>();
			simpleButton.image = simpleHG.GetComponent<UnityEngine.UI.Image>();
			simpleButton.colors = rightArrowButton.colors;

			ClearHGButton(simpleButton);

			AddPrefab(EUIPrefabIndex.SimpleButton, simpleHG);

			// Dropdown

			var dropdownPrefab = GameObject.Instantiate(MainMenuController.instance.settingsMenuScreen.GetComponentInChildren<SubmenuMainMenuScreen>(true).submenuPanelPrefab.GetComponentInChildren<MPDropdown>(true).gameObject);
			dropdownPrefab.name = "Dropdown";

			var dropdown = dropdownPrefab.GetComponent<MPDropdown>();
			dropdown.allowAllEventSystems = true;
			dropdown.ClearOptions();
			dropdown.onValueChanged?.RemoveAllListeners();
			dropdown.onValueChanged?.m_PersistentCalls?.Clear();

			AddPrefab(EUIPrefabIndex.Dropdown, dropdownPrefab);

			// Slider

			var sliderPrefab = GameObject.Instantiate(MainMenuController.instance.settingsMenuScreen.GetComponentInChildren<SubmenuMainMenuScreen>(true).submenuPanelPrefab.GetComponentInChildren<SettingsSlider>(true).gameObject.transform.GetChild(4).gameObject);
			sliderPrefab.name = "Slider";

			var sliderPrefabRect = sliderPrefab.GetComponent<RectTransform>();
			sliderPrefabRect.anchorMin = Vector2.zero;
			sliderPrefabRect.anchorMax = Vector2.one;

			var slider = sliderPrefab.GetComponentInChildren<Slider>();

			var sliderRect = slider.GetComponent<RectTransform>();
			sliderRect.anchorMin = Vector2.zero;
			sliderRect.anchorMax = Vector2.one;

			slider.onValueChanged.RemoveAllListeners();
			slider.onValueChanged.m_PersistentCalls.Clear();

			AddPrefab(EUIPrefabIndex.Slider, sliderPrefab);

			// Toggle

			var togglePrefab = MainMenuController.instance.multiplayerMenuScreen.GetComponentInChildren<MPToggle>(true).gameObject;

			var toggle = GameObject.Instantiate(togglePrefab).GetComponent<MPToggle>();
			toggle.name = "Toggle";
			toggle.mpControlHelper.allowAllEventSystems = true;

			var toggleRect = toggle.GetComponent<RectTransform>();

			toggleRect.pivot = Vector2.one / 2f;

			toggleRect.GetComponent<RectTransform>().anchorMax = Vector2.one;
			toggleRect.GetComponent<RectTransform>().anchorMin = Vector2.zero;

			foreach (Transform child in toggle.GetComponentsInChildren<Transform>(true))
				child.transform.localPosition = Vector3.zero;

			GameObject.Destroy(toggle.GetComponent<LayoutElement>());

			AddPrefab(EUIPrefabIndex.Toggle, toggle.gameObject);

			// Description

			var descriptionPrefab = MainMenuController.instance.settingsMenuScreen.GetComponentInChildren<SubmenuMainMenuScreen>(true).submenuPanelPrefab.transform.GetChild(3).GetChild(2).GetChild(0).gameObject;

			var description = GameObject.Instantiate(descriptionPrefab);

			description.name = "Description";

			AddPrefab(EUIPrefabIndex.Description, description);

			Log.Print($"Created {prefabs.Count} UI prefabs");*/
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Instantiate a UI prefab
		/// </summary>
		/// <param name="index"></param>
		/// <returns>null if not found</returns>
		internal static GameObject GetPrefab(EUIPrefabIndex index)
		{
			if (prefabs.ContainsKey(index))
				return GameObject.Instantiate(prefabs[index]);

			return null;
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Clear a given HGButton making it ready for cloning
		/// </summary>
		/// <param name="button"></param>
		internal static void ClearHGButton(HGButton button, bool clearLanguageController = true)
		{
			button.hoverToken = "XL_UNSET";
			button.requiredTopLayer = null;
			button.updateTextOnHover = false;
			button.hoverLanguageTextMeshController = null;
			button.disableGamepadClick = false;
			button.allowAllEventSystems = true;
			button.defaultFallbackButton = false;
			button.onClick?.m_PersistentCalls?.Clear();
			button.onClick?.RemoveAllListeners();
			button.onSelect = new UnityEngine.Events.UnityEvent();
			button.onDeselect = new UnityEngine.Events.UnityEvent();

			//button.onDeselect?.RemoveAllListeners();
			//button.onSelect?.RemoveAllListeners();

			if (!clearLanguageController)
				return;

			LanguageTextMeshController langController = button.GetComponent<LanguageTextMeshController>();

			if (langController)
				langController.token = "XL_UNSET";
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Track a prefab
		/// </summary>
		/// <param name="key"></param>
		/// <param name="prefab"></param>
		/// <param name="useRefresher"></param>
		internal static void AddPrefab(EUIPrefabIndex key, GameObject prefab, bool useRefresher = true)
		{
			if (prefabs.ContainsKey(key))
				return;

			prefab.SetActive(false);
			prefab.transform.SetParent(prefabRoot.transform);

			prefabs.Add(key, prefab);
		}

		//-----------------------------------------------------------------------------------------------------------

		internal enum EUIPrefabIndex
		{
			MainMenuButton,
			Screen,
			ScreenOption,
			SimpleImage,
			SimpleText,
			RightArrow,
			SimpleButton,
			Dropdown,
			Slider,
			Toggle,
			Description
		}
	}
}
