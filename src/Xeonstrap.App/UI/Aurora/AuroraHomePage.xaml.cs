using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Mvvm.Contracts;
using Xeonstrap.Enums;
using Xeonstrap.Utility;

namespace Xeonstrap.UI.Aurora;

/// <summary>
/// Landing surface of the Aurora shell. It answers one question first ("what do I do now?")
/// and keeps the three numbers a user actually checks - channel, flag count, build - above
/// the fold. Everything else stays one click away.
/// </summary>
public partial class AuroraHomePage : Page
{
	public AuroraHomePage()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		HeroStatus.Text = "Xeonstrap ready";
		HeroSubtitle.Text = "Settings are applied the next time Roblox starts. Pick a channel, tune the client, or launch straight away.";

		ChannelValue.Text = App.Settings.Prop.Channel;
		ChannelHint.Text = "Roblox client channel";

		FlagsValue.Text = App.FastFlags.Prop.Count.ToString();
		FlagsHint.Text = "overrides saved";

		BuildValue.Text = "v" + App.Version;
		BuildHint.Text = Xeonstrap.Utility.Platform.IsWindows
			? "Windows build"
			: Xeonstrap.Utility.Platform.IsLinux ? "Linux build" : "macOS build";
	}

	private void LaunchPlayer_Click(object sender, RoutedEventArgs e)
	{
		TryLaunch(LaunchMode.Player);
	}

	private void LaunchStudio_Click(object sender, RoutedEventArgs e)
	{
		TryLaunch(LaunchMode.Studio);
	}

	private static void TryLaunch(LaunchMode mode)
	{
		try
		{
			LaunchHandler.LaunchRoblox(mode);
		}
		catch (Exception ex)
		{
			App.Logger?.WriteException("AuroraHomePage::Launch", ex);
		}
	}

	private void OpenFastFlags_Click(object sender, MouseButtonEventArgs e) =>
		Open(typeof(Xeonstrap.UI.Elements.Settings.Pages.FastFlagsPage));

	private void OpenAppearance_Click(object sender, MouseButtonEventArgs e) =>
		Open(typeof(Xeonstrap.UI.Elements.Settings.Pages.AppearancePage));

	private void OpenMods_Click(object sender, MouseButtonEventArgs e) =>
		Open(typeof(Xeonstrap.UI.Elements.Settings.Pages.ModsPage));

	private void OpenManager_Click(object sender, MouseButtonEventArgs e) =>
		Open(typeof(Xeonstrap.UI.Elements.Settings.Pages.DownloadsPage));

	private void Open(Type pageType)
	{
		if (Window.GetWindow(this) is INavigationWindow navigation)
			navigation.Navigate(pageType);
	}
}
