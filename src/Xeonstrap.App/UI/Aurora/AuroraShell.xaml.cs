using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using Xeonstrap.Enums;
using Xeonstrap.UI.Elements.Base;

namespace Xeonstrap.UI.Aurora;

/// <summary>
/// Aurora shell: a grouped rail on the left, a command bar with the primary launch action,
/// and a frame that hosts the page surface.
///
/// The shell is additive. It reuses the existing pages (and therefore the existing view
/// models and setting tasks) while they are migrated one at a time, so every feature stays
/// reachable the moment the shell is opened.
/// </summary>
public partial class AuroraShell : WpfUiWindow, INavigationWindow
{
	private readonly List<AuroraNavEntry> _entries;
	private readonly Dictionary<string, Page> _pages = new(StringComparer.OrdinalIgnoreCase);

	private ListCollectionView? _view;
	private bool _suppressSelection;
	private bool _suppressNavigation;
	private bool _closed;

	public AuroraShell()
	{
		_entries = new List<AuroraNavEntry>(AuroraNavigation.Create());
		InitializeComponent();
	}

	// ------------------------------------------------------------------ lifecycle

	private void Shell_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			_view = new ListCollectionView(_entries)
			{
				Filter = FilterEntry
			};
			_view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AuroraNavEntry.Group)));
			NavList.ItemsSource = _view;

			VersionChip.Text = "v" + App.Version;
			ChannelChip.Text = App.Settings.Prop.Channel;
			StatusText.Text = App.LaunchSettings.QuietFlag.Active ? "Quiet" : "Ready";

			AuroraNavEntry? start = _entries.FirstOrDefault();
			string remembered = App.State.Prop.AuroraLastPage;
			if (!string.IsNullOrEmpty(remembered))
			{
				AuroraNavEntry? last = _entries.FirstOrDefault(entry => string.Equals(entry.Key, remembered, StringComparison.OrdinalIgnoreCase));
				if (last != null)
					start = last;
			}

			if (start != null)
			{
				SetSelection(start);
				NavigateTo(start);
			}

			UpdateNavigationButtons();
			App.Logger?.WriteLine("AuroraShell::Loaded", "Shell ready with " + _entries.Count + " rail entries");
		}
		catch (Exception ex)
		{
			// A broken page or view must not take the whole window down: log it, keep the
			// shell visible and tell the user instead of leaving them with a blank window.
			App.Logger?.WriteException("AuroraShell::Loaded", ex);
			try
			{
				PageTitle.Text = "Aurora shell";
				PageSubtitle.Visibility = Visibility.Visible;
				PageSubtitle.Text = "The shell loaded with a problem. Check the log file for details.";
				RootFrame.Navigate(new AuroraMissingPage("Could not open the start page"));
			}
			catch
			{
			}
		}
	}

	private void Shell_Closed(object sender, EventArgs e)
	{
		if (_closed)
			return;
		_closed = true;
		_pages.Clear();
		FlushOnClose();
		if (LaunchHandler.PortableSessionActive)
			return;
		App.SoftTerminate();
	}

	// ------------------------------------------------------------------ navigation

	private bool FilterEntry(object item)
	{
		if (item is not AuroraNavEntry entry)
			return false;

		string query = SearchBox.Text.Trim();
		if (query.Length == 0)
			return true;

		return entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| entry.Group.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (SearchHint != null)
			SearchHint.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

		_view?.Refresh();
	}

	private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_suppressSelection || NavList.SelectedItem is not AuroraNavEntry entry)
			return;

		NavigateTo(entry);
	}

	private void NavigateTo(AuroraNavEntry entry)
	{
		if (entry.PageType == null)
			return;

		if (RootFrame.Content is Page current && _pages.TryGetValue(entry.Key, out Page? cached) && ReferenceEquals(current, cached))
		{
			UpdateHeader(entry);
			return;
		}

		Page page = GetOrCreatePage(entry);
		_suppressNavigation = true;
		try
		{
			RootFrame.Navigate(page);
		}
		finally
		{
			_suppressNavigation = false;
		}

		UpdateHeader(entry);
		UpdateNavigationButtons();

		if (!string.Equals(App.State.Prop.AuroraLastPage, entry.Key, StringComparison.Ordinal))
		{
			App.State.Prop.AuroraLastPage = entry.Key;
			App.State.SaveDeferred();
		}
	}

	private Page GetOrCreatePage(AuroraNavEntry entry)
	{
		if (_pages.TryGetValue(entry.Key, out Page? page))
			return page;

		page = TryCreatePage(entry.PageType) ?? new AuroraMissingPage(entry.Title);
		_pages[entry.Key] = page;
		return page;
	}

	private static Page? TryCreatePage(Type? pageType)
	{
		if (pageType == null)
			return null;

		try
		{
			return Activator.CreateInstance(pageType) as Page;
		}
		catch (Exception ex)
		{
			App.Logger?.WriteLine("AuroraShell::TryCreatePage", "Could not create " + pageType.Name + ": " + ex.Message);
			return null;
		}
	}

	private void RootFrame_Navigated(object sender, NavigationEventArgs e)
	{
		UpdateNavigationButtons();

		if (_suppressNavigation)
			return;

		Type? source = RootFrame.Content?.GetType() ?? e.Content?.GetType();
		AuroraNavEntry? match = _entries.FirstOrDefault(entry => entry.PageType == source);
		if (match != null)
		{
			SetSelection(match);
			UpdateHeader(match);
			return;
		}

		if (RootFrame.Content is Page page && !string.IsNullOrWhiteSpace(page.Title))
		{
			PageTitle.Text = page.Title;
			PageSubtitle.Visibility = Visibility.Collapsed;
		}
	}

	private void SetSelection(AuroraNavEntry entry)
	{
		_suppressSelection = true;
		try
		{
			NavList.SelectedItem = entry;
			NavList.ScrollIntoView(entry);
		}
		finally
		{
			_suppressSelection = false;
		}
	}

	private void UpdateHeader(AuroraNavEntry entry)
	{
		PageTitle.Text = entry.Title;
		PageSubtitle.Visibility = Visibility.Collapsed;
	}

	private void UpdateNavigationButtons()
	{
		if (BackButton != null)
			BackButton.IsEnabled = RootFrame.CanGoBack;
		if (ForwardButton != null)
			ForwardButton.IsEnabled = RootFrame.CanGoForward;
	}

	private void Back_Click(object sender, RoutedEventArgs e)
	{
		if (RootFrame.CanGoBack)
			RootFrame.GoBack();
	}

	private void Forward_Click(object sender, RoutedEventArgs e)
	{
		if (RootFrame.CanGoForward)
			RootFrame.GoForward();
	}

	// ------------------------------------------------------------------ actions

	private async void Launch_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			await SaveAsync(false);
			LaunchHandler.LaunchRoblox(LaunchMode.Player);
		}
		catch (Exception ex)
		{
			App.Logger?.WriteException("AuroraShell::Launch", ex);
		}
	}

	private async void Studio_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			await SaveAsync(false);
			LaunchHandler.LaunchRoblox(LaunchMode.Studio);
		}
		catch (Exception ex)
		{
			App.Logger?.WriteException("AuroraShell::Studio", ex);
		}
	}

	private void Palette_Click(object sender, RoutedEventArgs e)
	{
		SearchBox.Focus();
		SearchBox.SelectAll();
	}

	// ------------------------------------------------------------------ saving

	/// <summary>
	/// Mirrors the legacy save pipeline: flush the three stores, then run the queued setting
	/// tasks (cursor, font, emoji and shortcut presets write files when they are executed).
	/// </summary>
	private async Task<bool> SaveAsync(bool notify)
	{
		try
		{
			App.Settings.SaveDeferred();
			App.State.Save();
			App.FastFlags.SaveDeferred();
		}
		catch (Exception ex)
		{
			App.Logger?.WriteException("AuroraShell::Save", ex);
			SetSaveState("Failed");
			return false;
		}

		int failed = 0;
		foreach (var pair in App.PendingSettingTasks.ToArray())
		{
			if (!pair.Value.Changed)
			{
				App.PendingSettingTasks.Remove(pair.Key);
				continue;
			}

			try
			{
				await pair.Value.ExecuteAsync();
				App.PendingSettingTasks.Remove(pair.Key);
			}
			catch (Exception ex)
			{
				failed++;
				App.Logger?.WriteException("AuroraShell::Save", ex);
			}
		}

		SetSaveState(failed == 0 ? "Saved" : "Check logs");
		if (notify && failed == 0)
			StatusText.Text = "Saved";

		return failed == 0;
	}

	private async void Save_Click(object sender, RoutedEventArgs e)
	{
		SaveButton.IsEnabled = false;
		try
		{
			await SaveAsync(true);
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}

	private void SetSaveState(string label)
	{
		if (SaveButtonLabel != null)
			SaveButtonLabel.Text = label;
	}

	private void FlushOnClose()
	{
		try
		{
			App.Settings.SaveDeferred();
			App.State.Save();
			App.FastFlags.SaveDeferred();
		}
		catch (Exception ex)
		{
			App.Logger?.WriteException("AuroraShell::FlushOnClose", ex);
		}
	}

	// ------------------------------------------------------------------ chrome

	private void Caption_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (FindAncestor<Button>(e.OriginalSource as DependencyObject) != null)
			return;

		if (e.ClickCount == 2)
		{
			ToggleMaximised();
			return;
		}

		if (e.ButtonState != MouseButtonState.Pressed)
			return;

		try
		{
			DragMove();
		}
		catch (InvalidOperationException)
		{
			// The drag ended before the window could follow the pointer.
		}
	}

	private void Minimize_Click(object sender, RoutedEventArgs e)
	{
		WindowState = System.Windows.WindowState.Minimized;
	}

	private void Maximize_Click(object sender, RoutedEventArgs e)
	{
		ToggleMaximised();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void ToggleMaximised()
	{
		WindowState = WindowState == System.Windows.WindowState.Maximized
			? System.Windows.WindowState.Normal
			: System.Windows.WindowState.Maximized;
	}

	private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
	{
		while (node != null)
		{
			if (node is T match)
				return match;

			try
			{
				node = System.Windows.Media.VisualTreeHelper.GetParent(node);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		return null;
	}

	// ------------------------------------------------------------------ keyboard

	private void Shell_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.K && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			Palette_Click(this, new RoutedEventArgs());
			e.Handled = true;
			return;
		}

		if (e.Key == Key.Escape && SearchBox.IsKeyboardFocusWithin)
		{
			SearchBox.Text = string.Empty;
			RootFrame.Focus();
			e.Handled = true;
			return;
		}

		if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			Save_Click(this, new RoutedEventArgs());
			e.Handled = true;
			return;
		}

		if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key >= Key.D1 && e.Key <= Key.D9)
		{
			int index = (int)e.Key - (int)Key.D1;
			if (index < _entries.Count)
			{
				AuroraNavEntry entry = _entries[index];
				SetSelection(entry);
				NavigateTo(entry);
				e.Handled = true;
			}
		}
	}

	// ------------------------------------------------------------------ INavigationWindow

	public Frame GetFrame() => RootFrame;

	public INavigation GetNavigation() => null!;

	public bool Navigate(Type pageType)
	{
		AuroraNavEntry? entry = _entries.FirstOrDefault(item => item.PageType == pageType);
		if (entry != null)
		{
			SetSelection(entry);
			NavigateTo(entry);
			return true;
		}

		Page? page = TryCreatePage(pageType);
		if (page == null)
			return false;

		_suppressNavigation = true;
		try
		{
			RootFrame.Navigate(page);
		}
		finally
		{
			_suppressNavigation = false;
		}

		UpdateNavigationButtons();
		return true;
	}

	public void SetPageService(IPageService pageService)
	{
	}

	public void ShowWindow()
	{
		Show();
	}

	public void CloseWindow()
	{
		Close();
	}
}
