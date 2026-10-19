using System;
using System.Windows;
using Xeonstrap.Integrations;
using Xeonstrap.UI.Elements.Base;
using Xeonstrap.UI.ViewModels.ContextMenu;

namespace Xeonstrap.UI.Elements.ContextMenu;

public partial class OutputConsole : WpfUiWindow
{
	private readonly OutputConsoleViewModel _viewModel;

	public OutputConsole(ActivityWatcher watcher)
	{
		_viewModel = new OutputConsoleViewModel(watcher);
		_viewModel.RequestCloseEvent += OnRequestClose;
		DataContext = _viewModel;
		InitializeComponent();
		Closed += OnClosed;
	}

	private void OnRequestClose(object? sender, EventArgs e)
	{
		Close();
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		Closed -= OnClosed;
		_viewModel.RequestCloseEvent -= OnRequestClose;
		_viewModel.Dispose();
		DataContext = null;
	}
}
