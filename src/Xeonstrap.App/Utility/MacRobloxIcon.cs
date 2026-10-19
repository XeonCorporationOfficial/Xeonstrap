using System;
using System.IO;

namespace Xeonstrap.Utility;

internal static class MacRobloxIcon
{
	private const string LogIdent = "MacRobloxIcon";

	private static string IconFile => Path.Combine(Paths.Cache, "RobloxGameIcon.png");

	private static string MarkerFile => Path.Combine(Paths.Cache, "RobloxGameIcon.applied");

	internal static void Restore(string? bundlePath = null)
	{
		if (!OperatingSystem.IsMacOS())
			return;
		try
		{
			if (File.Exists(IconFile))
				File.Delete(IconFile);
			if (!File.Exists(MarkerFile))
				return;
			string path = string.IsNullOrWhiteSpace(bundlePath) ? File.ReadAllText(MarkerFile).Trim() : bundlePath;
			if (Xeonstrap.Platform.MacOS.MacOSShortcut.SetCustomIcon(path, null))
				App.Logger.WriteLine(LogIdent, "Removed the custom icon an earlier Xeonstrap version set on Roblox");
			File.Delete(MarkerFile);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			App.Logger.WriteLine(LogIdent, "The Roblox icon could not be restored: " + ex.Message);
		}
	}
}
