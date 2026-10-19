using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Xeonstrap.UI.ViewModels.Installer;

public class InstallViewModel : NotifyPropertyChangedViewModel
{
	private readonly Xeonstrap.Installer installer = new Xeonstrap.Installer();

	private readonly string _originalInstallLocation;
	private string _soberInstallStatus = string.Empty;

	private System.Threading.CancellationTokenSource? _soberCancellation;

	public EventHandler<bool>? SetCanContinueEvent;

	public Xeonstrap.Models.SettingTasks.ShortcutTask PlayerIconTask { get; }
	public Xeonstrap.Models.SettingTasks.ShortcutTask StudioIconTask { get; }
	public Xeonstrap.Models.SettingTasks.ShortcutTask SettingsIconTask { get; }

	internal void SetImport(Xeonstrap.Utility.BootstrapperImportPlan? plan)
	{
		installer.PendingImport = plan;
	}

	public string InstallLocation
	{
		get
		{
			return installer.InstallLocation;
		}
		set
		{
			if (!string.IsNullOrEmpty(ErrorMessage))
			{
				SetCanContinueEvent?.Invoke(this, e: true);
				installer.InstallLocationError = "";
				OnPropertyChanged(nameof(ErrorMessage));
			}
			installer.InstallLocation = value;
			OnPropertyChanged(nameof(DataFoundMessageVisibility));
		}
	}

	public Visibility DataFoundMessageVisibility
	{
		get
		{
			if (!installer.ExistingDataPresent)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public string ErrorMessage => installer.InstallLocationError;

	public string InstallLocationTitle => Xeonstrap.Utility.Platform.IsMacOS
		? Xeonstrap.Resources.Strings.Installer_MacOS_DataLocation_Title
		: Xeonstrap.Resources.Strings.Installer_Install_Location_Title;

	public string InstallLocationDescription => Xeonstrap.Utility.Platform.IsMacOS
		? Xeonstrap.Resources.Strings.Installer_MacOS_DataLocation_Text
		: Xeonstrap.Resources.Strings.Installer_Install_Location_Text;

	public string SoberInstallStatus
	{
		get => _soberInstallStatus;
		private set
		{
			_soberInstallStatus = value;
			OnPropertyChanged(nameof(SoberInstallStatus));
		}
	}

	public void SetSoberInstallStatus(string message)
	{
		SoberInstallStatus = message;
	}

	public bool CreateDesktopShortcuts
	{
		get
		{
			return installer.CreateDesktopShortcuts;
		}
		set
		{
			installer.CreateDesktopShortcuts = value;
		}
	}

	public bool CreateStartMenuShortcuts
	{
		get
		{
			return installer.CreateStartMenuShortcuts;
		}
		set
		{
			installer.CreateStartMenuShortcuts = value;
		}
	}

	public bool ExtractRobloxIcons
	{
		get
		{
			return installer.ExtractRobloxIcons;
		}
		set
		{
			installer.ExtractRobloxIcons = value;
		}
	}

	public bool CreatePlayerShortcut
	{
		get
		{
			return installer.CreatePlayerShortcut;
		}
		set
		{
			installer.CreatePlayerShortcut = value;
		}
	}

	public bool CreateStudioShortcut
	{
		get
		{
			return installer.CreateStudioShortcut;
		}
		set
		{
			installer.CreateStudioShortcut = value;
		}
	}

	public bool CreateSettingsShortcut
	{
		get
		{
			return installer.CreateSettingsShortcut;
		}
		set
		{
			installer.CreateSettingsShortcut = value;
		}
	}

	public ICommand BrowseInstallLocationCommand => new RelayCommand(BrowseInstallLocation);

	public ICommand ResetInstallLocationCommand => new RelayCommand(ResetInstallLocation);

	public ICommand OpenFolderCommand => new RelayCommand(OpenFolder);

	public InstallViewModel()
	{
		_originalInstallLocation = installer.InstallLocation;
		string suffix = Xeonstrap.Utility.Shortcut.Suffix;
		PlayerIconTask = new("RobloxPlayer", Paths.Desktop, Xeonstrap.Resources.Strings.LaunchMenu_LaunchRoblox + suffix, "-player", true, true);
		StudioIconTask = new("RobloxStudio", Paths.Desktop, Xeonstrap.Resources.Strings.LaunchMenu_LaunchRobloxStudio + suffix, "-studio", true, true);
		SettingsIconTask = new("Settings", Paths.Desktop, Xeonstrap.Resources.Strings.Menu_Title + suffix, "-settings", true, true);
		installer.FunctionShortcutTasks = [PlayerIconTask, StudioIconTask, SettingsIconTask];
	}

	public async System.Threading.Tasks.Task<bool> DoInstallAsync(Action<string>? report = null)
	{
		if (!installer.CheckInstallLocation())
		{
			SetCanContinueEvent?.Invoke(this, e: false);
			OnPropertyChanged(nameof(ErrorMessage));
			return false;
		}
		try
		{
			PlayerIconTask.ValidateAppearance(CreatePlayerShortcut);
			StudioIconTask.ValidateAppearance(CreateStudioShortcut);
			SettingsIconTask.ValidateAppearance(CreateSettingsShortcut);
			installer.DoInstall();
		}
		catch (Exception ex)
		{
			App.Logger.WriteException("InstallViewModel::DoInstall", ex);
			Frontend.ShowMessageBox("Xeonstrap could not finish installing to this location.\n\n" + ex.Message + "\n\nPick a different folder and try again.", MessageBoxImage.Hand);
			SetCanContinueEvent?.Invoke(this, e: true);
			return false;
		}
		if (Xeonstrap.Utility.Platform.IsLinux)
		{
			SoberInstallStatus = "Installing Sober";
			using System.Threading.CancellationTokenSource cancellation = new();
			_soberCancellation = cancellation;
			try
			{
				Xeonstrap.Platform.OperationResult result = await new Xeonstrap.Platform.Linux.LinuxSoberInstaller(new Xeonstrap.Core.SystemProcessService())
					.InstallAsync(cancellation.Token, report);
				App.Logger.WriteLine("InstallViewModel::DoInstall", result.Succeeded ? "Sober installed" : (result.Failure?.Message ?? "Sober could not be installed"));
				if (!result.Succeeded)
					Frontend.ShowMessageBox(result.Failure?.Message ?? "Sober could not be installed", MessageBoxImage.Error, MessageBoxButton.OK);
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				App.Logger.WriteLine("InstallViewModel::DoInstall", "The Sober install was cancelled");
				return false;
			}
			catch (Exception ex)
			{
				App.Logger.WriteException("InstallViewModel::DoInstall", ex);
				Frontend.ShowMessageBox("Sober could not be installed: " + ex.Message, MessageBoxImage.Error, MessageBoxButton.OK);
			}
			finally
			{
				_soberCancellation = null;
			}
		}
		return true;
	}

	public void CancelSoberInstall()
	{
		try
		{
			_soberCancellation?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private void BrowseInstallLocation()
	{
		if (Xeonstrap.Utility.Platform.IsLinux)
		{
			Microsoft.Win32.OpenFolderDialog dialog = new()
			{
				Title = "Choose the Xeonstrap installation folder",
				InitialDirectory = InstallLocation
			};
			if (dialog.ShowDialog() == true)
			{
				InstallLocation = dialog.FolderName;
				OnPropertyChanged(nameof(InstallLocation));
			}
			return;
		}

		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
		{
			InstallLocation = folderBrowserDialog.SelectedPath;
			OnPropertyChanged(nameof(InstallLocation));
		}
	}

	private void ResetInstallLocation()
	{
		InstallLocation = _originalInstallLocation;
		OnPropertyChanged(nameof(InstallLocation));
	}

	private void OpenFolder()
	{
		if (!Xeonstrap.Utility.Platform.IsWindows)
		{
			Xeonstrap.Utility.PlatformShell.TryOpenFolder(Paths.Base);
			return;
		}

		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Xeonstrap.Utility.PlatformShell.WindowsTool("explorer.exe"),
				UseShellExecute = true
			};
			startInfo.ArgumentList.Add(Paths.Base);
			Process.Start(startInfo);
		}
		catch (Exception ex)
		{
			App.Logger.WriteLine("InstallViewModel::OpenFolder", "Could not open the install folder: " + ex.Message);
		}
	}
}
