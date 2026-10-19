using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Xeonstrap.UI.ViewModels.Pages;

namespace Xeonstrap.UI.Elements.Settings.Pages;

public partial class HistoryPage : Page{
	private readonly HistoryPageViewModel _viewModel;

	public HistoryPage()
	{
		_viewModel = new HistoryPageViewModel();
		base.DataContext = _viewModel;
		InitializeComponent();
	}
}
