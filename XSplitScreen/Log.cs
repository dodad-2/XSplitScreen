using BepInEx.Logging;
using System;
using UnityEngine;

namespace Dodad.XSplitscreen
{
	internal static class Log
	{
		private static ELogChannel ActiveChannels = ELogChannel.None;
		private static ManualLogSource Source;

		/// <summary>
		/// Initialize the logger with the plugin log source and log level
		/// </summary>
		/// <param name="source"></param>
		/// <param name="channels"></param>
		internal static void Init(ManualLogSource source, ELogChannel channels)
		{
			Source = source;

			SetActiveChannels(channels);
		}

		/// <summary>
		/// Set the log level for the plugin
		/// </summary>
		/// <param name="channels"></param>
		internal static void SetActiveChannels(ELogChannel channels)
		{
			ActiveChannels = channels;

			Source.LogMessage($"Set log channels to '{channels}'");
		}

		/// <summary>
		/// Output a message to the console
		/// </summary>
		/// <param name="data"></param>
		/// <param name="channel"></param>
		internal static void Print(object data, ELogChannel channel = ELogChannel.Debug)
		{
			if (!ActiveChannels.HasFlag(ELogChannel.All) &&
				!ActiveChannels.HasFlag(channel))
				return;

			if (channel.HasFlag(ELogChannel.Message))
				Source.LogMessage(data);
			else if (channel.HasFlag(ELogChannel.Info))
				Source.LogInfo(data);
			else if (channel.HasFlag(ELogChannel.Warning))
				Source.LogWarning(data);
			else if (channel.HasFlag(ELogChannel.Error))
				Source.LogError(data);
			else if (channel.HasFlag(ELogChannel.Fatal))
				Source.LogFatal(data);
			else if (channel.HasFlag(ELogChannel.Debug))
				Source.LogDebug(data);
		}

		[Flags]
		internal enum ELogChannel
		{
			None = 1 << 0,
			Message = 1 << 1,
			Info = 1 << 2,
			Warning = 1 << 3,
			Error = 1 << 4,
			Fatal = 1 << 5,
			Debug = 1 << 6,
			All = 1 << 7,
		}
	}
}