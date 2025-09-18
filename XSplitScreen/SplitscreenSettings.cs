using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using static Dodad.XSplitscreen.Log;

namespace Dodad.XSplitscreen
{
	/// <summary>
	/// Interface for modular settings components.
	/// Each settings module should implement this interface.
	/// </summary>
	public interface IUserSettingsModule
	{
		/// <summary>
		/// Unique identifier for the settings module.
		/// </summary>
		string ModuleKey { get; }
	}

	/// <summary>
	/// Example settings module for color configuration.
	/// </summary>
	public class ColorSettingsModule : IUserSettingsModule
	{
		/// <summary>
		/// Module identifier key.
		/// </summary>
		public string ModuleKey => "Color";

		/// <summary>
		/// Main color RGBA components.
		/// </summary>
		public float MainR = 1;
		public float MainG = 1;
		public float MainB = 1;
		public float MainA = 1;
	}

	/// <summary>
	/// Container for all user settings modules.
	/// </summary>
	public class UserSettings
	{
		/// <summary>
		/// Dictionary of settings modules indexed by module key.
		/// </summary>
		public Dictionary<string, IUserSettingsModule> Modules = new Dictionary<string, IUserSettingsModule>();
	}

	/// <summary>
	/// Static class responsible for loading, saving, and managing user settings.
	/// </summary>
	public static class SplitScreenSettings
	{
		// Cache of loaded user settings to avoid frequent disk access
		private static readonly Dictionary<ulong, UserSettings> _settingsCache = new Dictionary<ulong, UserSettings>();

		/// <summary>
		/// Builds file path for settings using Assembly location.
		/// </summary>
		/// <param name="saveID">Unique identifier for the user's settings</param>
		/// <returns>Full path to the settings file</returns>
		private static string GetFilePath(ulong saveID)
		{
			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var settingsDir = Path.Combine(baseDir, "Settings");
			Directory.CreateDirectory(settingsDir);
			return Path.Combine(settingsDir, $"{saveID}.json");
		}

		/// <summary>
		/// Gets all settings for a user (from cache or disk).
		/// </summary>
		/// <param name="saveID">Unique identifier for the user</param>
		/// <returns>User settings container</returns>
		public static UserSettings GetUserSettings(ulong saveID)
		{
			// Return cached settings if available
			if (_settingsCache.TryGetValue(saveID, out var settings))
				return settings;

			string filePath = GetFilePath(saveID);
			if (File.Exists(filePath))
			{
				try
				{
					var json = File.ReadAllText(filePath, Encoding.UTF8);
					var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

					var userSettings = new UserSettings();
					foreach (var kvp in dict)
					{
						IUserSettingsModule module = DeserializeModule(kvp.Key, kvp.Value);
						if (module != null)
							userSettings.Modules[kvp.Key] = module;
					}

					// Cache the loaded settings
					_settingsCache[saveID] = userSettings;
					return userSettings;
				}
				catch (Exception ex)
				{
					Print($"Failed to load settings for user {saveID}: {ex.Message}", ELogChannel.Error);
					return CreateDefaultSettings(saveID);
				}
			}
			else
			{
				return CreateDefaultSettings(saveID);
			}
		}

		/// <summary>
		/// Creates and caches default settings for a user.
		/// </summary>
		private static UserSettings CreateDefaultSettings(ulong saveID)
		{
			var settings = new UserSettings();
			_settingsCache[saveID] = settings;
			return settings;
		}

		/// <summary>
		/// Sets a specific settings module for a user.
		/// </summary>
		/// <typeparam name="T">Type of the settings module</typeparam>
		/// <param name="saveID">User ID</param>
		/// <param name="module">Module instance to save</param>
		public static void SetUserModule<T>(ulong saveID, T module) where T : IUserSettingsModule
		{
			var settings = GetUserSettings(saveID);
			settings.Modules[module.ModuleKey] = module;
			SaveUserSettings(saveID);
		}

		/// <summary>
		/// Gets a specific settings module for a user, or null if not found.
		/// </summary>
		/// <typeparam name="T">Type of the settings module to retrieve</typeparam>
		/// <param name="saveID">User ID</param>
		/// <returns>Settings module instance or null</returns>
		public static T GetUserModule<T>(ulong saveID) where T : class, IUserSettingsModule, new()
		{
			var settings = GetUserSettings(saveID);
			if (settings.Modules.TryGetValue(GetModuleKey<T>(), out var module))
				return module as T;
			return null;
		}

		/// <summary>
		/// Saves user settings to disk.
		/// </summary>
		/// <param name="saveID">User ID</param>
		public static void SaveUserSettings(ulong saveID)
		{
			var settings = GetUserSettings(saveID);
			var dict = new Dictionary<string, string>();

			foreach (var kvp in settings.Modules)
			{
				dict[kvp.Key] = JsonConvert.SerializeObject(kvp.Value);
			}

			string filePath = GetFilePath(saveID);
			try
			{
				string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
				File.WriteAllText(filePath, json, Encoding.UTF8);
				Print($"Settings saved for user {saveID}", ELogChannel.Debug);
			}
			catch (Exception ex)
			{
				Print($"Failed to save settings for user {saveID}: {ex.Message}", ELogChannel.Error);
			}
		}

		/// <summary>
		/// Gets module key from type (requires instance).
		/// </summary>
		private static string GetModuleKey<T>() where T : IUserSettingsModule, new()
		{
			return new T().ModuleKey;
		}

		/// <summary>
		/// Factory method to deserialize a module based on its key.
		/// </summary>
		/// <param name="key">Module key identifier</param>
		/// <param name="json">JSON serialized data</param>
		/// <returns>Deserialized module instance</returns>
		private static IUserSettingsModule DeserializeModule(string key, string json)
		{
			switch (key)
			{
				case "Color":
					return JsonConvert.DeserializeObject<ColorSettingsModule>(json);
				// Add more cases here for new modules
				default:
					Print($"Unknown settings module '{key}'", ELogChannel.Warning);
					return null;
			}
		}
	}
}