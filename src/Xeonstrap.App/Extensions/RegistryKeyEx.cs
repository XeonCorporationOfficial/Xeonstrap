using System;
using System.Windows;
using Microsoft.Win32;
using Xeonstrap.Enums;
using Xeonstrap.Resources;
using Xeonstrap.UI;

namespace Xeonstrap.Extensions;

public static class RegistryKeyEx
{
	public static void SetValueSafe(this RegistryKey registryKey, string? name, object value)
	{
		if (!Xeonstrap.Utility.Platform.SupportsRegistry)
		{
			App.Logger.WriteLine("RegistryKeyEx::SetValueSafe", $"Skipped registry write on this platform: {registryKey}\\{name}");
			return;
		}
		try
		{
			App.Logger.WriteLine("RegistryKeyEx::SetValueSafe", $"Writing '{value}' to {registryKey}\\{name}");
			registryKey.SetValue(name, value);
		}
		catch (UnauthorizedAccessException)
		{
			Frontend.ShowMessageBox(Strings.Dialog_RegistryWriteError, MessageBoxImage.Hand);
			App.Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
		}
	}

	public static void DeleteValueSafe(this RegistryKey registryKey, string name)
	{
		if (!Xeonstrap.Utility.Platform.SupportsRegistry)
		{
			App.Logger.WriteLine("RegistryKeyEx::DeleteValueSafe", $"Skipped registry delete on this platform: {registryKey}\\{name}");
			return;
		}
		try
		{
			App.Logger.WriteLine("RegistryKeyEx::DeleteValueSafe", $"Deleting {registryKey}\\{name}");
			registryKey.DeleteValue(name);
		}
		catch (UnauthorizedAccessException)
		{
			Frontend.ShowMessageBox(Strings.Dialog_RegistryWriteError, MessageBoxImage.Hand);
			App.Terminate(ErrorCode.ERROR_INSTALL_FAILURE);
		}
	}
}
