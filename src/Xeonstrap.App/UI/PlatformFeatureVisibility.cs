﻿using System.Windows;
using Xeonstrap.Platform;

namespace Xeonstrap.UI;

public static class PlatformFeatureVisibility
{
	public static Visibility Overlay { get; } = Resolve(FeatureId.Overlay);

	public static Visibility GlobalInput { get; } = Resolve(FeatureId.GlobalInput);

	public static Visibility AudioSession { get; } = Resolve(FeatureId.AudioSession);

	public static Visibility Tray { get; } = Resolve(FeatureId.Tray);

	public static Visibility ResourceOptimization { get; } = Resolve(FeatureId.ResourceOptimization);

	public static Visibility TasxOptimization { get; } = Xeonstrap.Utility.Platform.IsMacOS ? Visibility.Visible : ResourceOptimization;

	public static Visibility FrameGeneration { get; } = Resolve(FeatureId.FrameGeneration);

	public static Visibility VirtualController { get; } = Resolve(FeatureId.VirtualController);

	public static Visibility WindowsIntegration { get; } = Xeonstrap.Utility.Platform.IsWindows ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility NetworkMtu { get; } = Xeonstrap.Utility.Platform.IsWindows ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility DesktopBackdrop { get; } = Xeonstrap.Utility.Platform.IsWindows || Xeonstrap.Utility.Platform.IsMacOS ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility LinuxIntegration { get; } = Xeonstrap.Utility.Platform.IsLinux ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility NotLinux { get; } = Xeonstrap.Utility.Platform.IsLinux ? Visibility.Collapsed : Visibility.Visible;

	public static Visibility NotMacOS { get; } = Xeonstrap.Utility.Platform.IsMacOS ? Visibility.Collapsed : Visibility.Visible;

	public static Visibility MacOSIntegration { get; } = Xeonstrap.Utility.Platform.IsMacOS ? Visibility.Visible : Visibility.Collapsed;

	public static Visibility PortableImages { get; } = Xeonstrap.Utility.Platform.UsesPortableUi ? Visibility.Visible : Visibility.Collapsed;

	public static bool IsSupported(FeatureId feature)
	{
		return Resolve(feature) == Visibility.Visible;
	}

	private static Visibility Resolve(FeatureId feature)
	{
		if (Xeonstrap.Utility.Platform.IsWindows)
		{
			return Visibility.Visible;
		}

		try
		{
			IPlatformHost? host = Xeonstrap.Utility.Platform.RuntimeHost;
			return Xeonstrap.Core.PlatformFeatureGate.IsHidden(host?.Capabilities, feature)
				? Visibility.Collapsed
				: Visibility.Visible;
		}
		catch
		{
			return Visibility.Visible;
		}
	}
}
