using System.Collections.Generic;
using Microsoft.Win32;
using Xeonstrap.Enums;

namespace Xeonstrap.Extensions;

public static class ThemeEx
{
	public static IReadOnlyCollection<Theme> Selections { get; } = new Theme[16]
	{
		Theme.Default,
		Theme.Aurora,
		Theme.Custom,
		Theme.Dark,
		Theme.UltraGray,
		Theme.Light,
		Theme.Xeonstrap,
		Theme.Red,
		Theme.Orange,
		Theme.Yellow,
		Theme.Green,
		Theme.Cyan,
		Theme.Blue,
		Theme.Purple,
		Theme.Pink,
		Theme.Berry
	};

	public static Theme GetFinal(this Theme dialogTheme)
	{
		if (dialogTheme != Theme.Default)
		{
			return dialogTheme;
		}
		if (!Xeonstrap.Utility.Platform.SupportsRegistry)
		{
			return Theme.Dark;
		}
		using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
		object? obj = registryKey?.GetValue("AppsUseLightTheme");
		if (obj is int && (int)obj == 0)
		{
			return Theme.Dark;
		}
		return Theme.Light;
	}
}
