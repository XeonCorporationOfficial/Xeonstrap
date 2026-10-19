using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Xeonstrap.UI.Elements.Controls;

public partial class BootstrapperImportControl : UserControl
{
	private CancellationTokenSource? _importCts;
	public bool StageOnly { get; set; }
	internal Xeonstrap.Utility.BootstrapperImportPlan? PendingPlan { get; private set; }
	public event EventHandler? Imported;

	public BootstrapperImportControl()
	{
		InitializeComponent();
		if (Xeonstrap.Utility.Platform.IsLinux)
			Visibility = Visibility.Collapsed;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_importCts?.Cancel();
	}

	private async void ApplyFrom_Click(object sender, RoutedEventArgs e)
	{
		if (_importCts != null || sender is not System.Windows.Controls.Button applyButton
			|| applyButton.Parent is not StackPanel panel || panel.Children.OfType<ComboBox>().FirstOrDefault() is not ComboBox sourceSelector
			|| sourceSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string source)
			return;

		using CancellationTokenSource cts = new();
		_importCts = cts;
		applyButton.IsEnabled = false;
		sourceSelector.IsEnabled = false;
		Window? owner = Window.GetWindow(this);
		bool ownerEnabled = owner?.IsEnabled == true;
		bool ownerDisabled = false;
		try
		{
			string? settingsPath = await Xeonstrap.Utility.BootstrapperSettingsImport.FindSettingsAsync(source, cts.Token);
			cts.Token.ThrowIfCancellationRequested();
			if (settingsPath == null)
			{
				var picker = new Microsoft.Win32.OpenFileDialog
				{
					Title = "Select " + source + " settings",
					Filter = "Settings JSON (*.json)|*.json",
					FileName = "Settings.json",
					CheckFileExists = true,
					Multiselect = false
				};
				if (picker.ShowDialog(Window.GetWindow(this)) != true)
					return;
				settingsPath = await Xeonstrap.Utility.BootstrapperSettingsImport.ResolvePickedPathAsync(picker.FileName);
			}

			if (owner != null && ownerEnabled)
			{
				ownerDisabled = true;
				owner.IsEnabled = false;
			}
			var plan = await Xeonstrap.Utility.BootstrapperSettingsImport.ReadAsync(source, settingsPath, cts.Token);
			cts.Token.ThrowIfCancellationRequested();
			if (StageOnly)
			{
				PendingPlan = plan;
			}
			else
			{
				App.Settings.FlushDeferred();
				await Task.Run(() => Xeonstrap.Utility.BootstrapperSettingsImport.Apply(plan, App.Settings, App.FastFlags), cts.Token);
				if (cts.IsCancellationRequested)
					return;
				Imported?.Invoke(this, EventArgs.Empty);
				Xeonstrap.UI.RestartNotificationService.Require("bootstrapperImport", "Settings imported", "Restart Xeonstrap to refresh settings and open pages. Roblox settings take effect on its next launch.", "Restart now", Xeonstrap.UI.RestartTarget.Application);
			}
			if (ownerDisabled)
				owner!.IsEnabled = true;
			Frontend.ShowMessageBox($"Selected {plan.Settings.Count} settings and {plan.Flags?.Count ?? 0} FastFlags from {source}.\n\nSkipped {plan.Skipped} settings without a compatible Xeonstrap setting. " + (StageOnly ? "These settings will be applied when you install Xeonstrap." : "Restart Xeonstrap to refresh open pages."));
		}
		catch (OperationCanceledException) when (cts.IsCancellationRequested)
		{
		}
		catch (Exception ex)
		{
			if (ownerDisabled)
				owner!.IsEnabled = true;
			App.Logger.WriteException("BootstrapperImport::ApplyFrom", ex);
			if (!cts.IsCancellationRequested)
				Frontend.ShowMessageBox("Settings could not be imported: " + ex.Message, MessageBoxImage.Warning);
		}
		finally
		{
			if (ownerDisabled)
				owner!.IsEnabled = true;
			_importCts = null;
			applyButton.IsEnabled = true;
			sourceSelector.IsEnabled = true;
		}
	}

}
