using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using static Dodad.XSplitscreen.Log;

namespace Dodad.XSplitscreen
{
	public interface IUserSettingsModule
	{
		/// <summary>
		/// Unique identifier for the settings module.
		/// </summary>
		string ModuleKey { get; }
	}

	public class ColorSettingsModule : IUserSettingsModule
	{
		public string ModuleKey => "Color";
		[JsonIgnore]
		public UnityEngine.Color Color => new UnityEngine.Color(MainR, MainG, MainB, MainA);
		public float MainR = 1;
		public float MainG = 1;
		public float MainB = 1;
		public float MainA = 1;
	}

	public class TrailsSettingsModule : IUserSettingsModule
	{
		public string ModuleKey => "Trails";
		public string TrailKey = "none";
	}

	public class UserSettings
	{
		/// <summary>
		/// Dictionary of settings modules indexed by module key.
		/// </summary>
		public Dictionary<string, IUserSettingsModule> Modules = new Dictionary<string, IUserSettingsModule>();
	}

	/// <summary>
	/// Handles loading, updating, and saving user settings for multiple users and modules.
	/// Individual modules can be updated without disk writes, and batch save can be triggered manually.
	/// </summary>
	public static class SplitScreenSettings
	{
		// Cache of loaded user settings to avoid frequent disk access
		private static readonly Dictionary<string, UserSettings> _settingsCache = new Dictionary<string, UserSettings>();

		// Tracks which user settings have unsaved changes
		private static readonly HashSet<string> _dirtyUsers = new HashSet<string>();

		private static string GetFilePath(string fileName)
		{
			var baseDir = Path.GetDirectoryName(Plugin.Singleton.Config.ConfigFilePath);
			var settingsDir = Path.Combine(baseDir, "XSplitscreen");
			Directory.CreateDirectory(settingsDir);
			return Path.Combine(settingsDir, $"{fileName}.json");
		}

		/// <summary>
		/// Gets all settings for a user (from cache or disk).
		/// </summary>
		public static UserSettings GetUserSettings(string fileName)
		{
			if (fileName == null)
				return null;

			if (_settingsCache.TryGetValue(fileName, out var settings))
				return settings;

			string filePath = GetFilePath(fileName);
			if (File.Exists(filePath))
			{
				try
				{
					var json = File.ReadAllText(filePath, Encoding.UTF8);
					var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
					var userSettings = new UserSettings();

					if (dict != null)
					{
						foreach (var kvp in dict)
						{
							IUserSettingsModule module = DeserializeModule(kvp.Key, kvp.Value);
							if (module != null)
								userSettings.Modules[kvp.Key] = module;
						}
					}
					_settingsCache[fileName] = userSettings;
					return userSettings;
				}
				catch (Exception ex)
				{
					Print($"Failed to load settings for user file '{fileName}': {ex.Message}", ELogChannel.Error);
					return CreateDefaultSettings(fileName);
				}
			}
			else
			{
				return CreateDefaultSettings(fileName);
			}
		}

		private static UserSettings CreateDefaultSettings(string fileName)
		{
			var settings = new UserSettings();
			_settingsCache[fileName] = settings;
			return settings;
		}

		/// <summary>
		/// Adds or updates a module for a user in memory (does not write to disk).
		/// </summary>
		public static void SetUserModule<T>(string fileName, T module) where T : IUserSettingsModule
		{
			var settings = GetUserSettings(fileName);
			settings.Modules[module.ModuleKey] = module;
			// Mark as dirty for batch saving
			_dirtyUsers.Add(fileName);
		}

		/// <summary>
		/// Gets a module for a user, or null if not found.
		/// </summary>
		public static T GetUserModule<T>(string fileName) where T : class, IUserSettingsModule, new()
		{
			var settings = GetUserSettings(fileName);
			if (settings.Modules.TryGetValue(GetModuleKey<T>(), out var module))
				return module as T;
			return null;
		}

		/// <summary>
		/// Saves all modified user settings to disk in a batch.
		/// Only dirty users are saved.
		/// </summary>
		public static void BatchSaveDirtyUsers()
		{
			foreach (var fileName in _dirtyUsers)
			{
				SaveUserSettingsImmediate(fileName);
			}
			_dirtyUsers.Clear();

			Log.Print("BatchSaveDirtyUsers");
		}

		/// <summary>
		/// Forces a disk write for a specific user (regardless of dirty state).
		/// </summary>
		public static void SaveUserSettingsImmediate(string fileName)
		{
			var settings = GetUserSettings(fileName);
			var dict = new Dictionary<string, string>();
			foreach (var kvp in settings.Modules)
			{
				dict[kvp.Key] = JsonConvert.SerializeObject(kvp.Value);
			}

			string filePath = GetFilePath(fileName);
			try
			{
				string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
				File.WriteAllText(filePath, json, Encoding.UTF8);
				Print($"Settings saved for user file '{fileName}'", ELogChannel.Debug);
			}
			catch (Exception ex)
			{
				Print($"Failed to save settings for user file '{fileName}': {ex.Message}", ELogChannel.Error);
			}
		}

		/// <summary>
		/// Returns a reference to a user's module, creating it if necessary. Use this to keep an object-bound reference.
		/// The returned module reference will persist changes to the module in the cache.
		/// </summary>
		public static T GetOrCreateUserModule<T>(string fileName) where T : class, IUserSettingsModule, new()
		{
			if (fileName == null || fileName.Length == 0)
				return default;

			var settings = GetUserSettings(fileName);
			var key = GetModuleKey<T>();
			if (!settings.Modules.TryGetValue(key, out var module))
			{
				module = new T();
				settings.Modules[key] = module;
				_dirtyUsers.Add(fileName);
			}
			return module as T;
		}

		/// <summary>
		/// Marks a user as dirty (call when a module changes).
		/// </summary>
		public static void MarkUserDirty(string fileName)
		{
			// Optionally: validate moduleKey exists
			_dirtyUsers.Add(fileName);
		}

		private static string GetModuleKey<T>() where T : IUserSettingsModule, new()
		{
			return new T().ModuleKey;
		}

		private static IUserSettingsModule DeserializeModule(string key, string json)
		{
			switch (key)
			{
				case "Color":
					return JsonConvert.DeserializeObject<ColorSettingsModule>(json);
				case "Trails":
					return JsonConvert.DeserializeObject<TrailsSettingsModule>(json);
				default:
					Print($"Unknown settings module '{key}'", ELogChannel.Warning);
					return null;
			}
		}
	}
}