using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Xeonstrap.UI.Elements.Controls;
using Xeonstrap.UI.ViewModels.Settings;
using Wpf.Ui.Controls;

namespace Xeonstrap.UI.Elements.Settings.Pages;

public partial class BehaviourPage : UiPage{

	private int _settingsRevision;

	public BehaviourPage()
	{
		base.DataContext = new BehaviourViewModel();
		InitializeComponent();
		MtuOption.DataContext = ((BehaviourViewModel)base.DataContext).Mtu;
		_settingsRevision = App.Settings.Revision;
		base.Loaded += OnPageLoaded;
		base.Unloaded += OnPageUnloaded;
	}

	private void OnPageLoaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is BehaviourViewModel behaviourViewModel)
		{
			if (App.Settings.Revision != _settingsRevision)
			{
				behaviourViewModel.OnPropertyChanged(string.Empty);
			}
			behaviourViewModel.RefreshExcludedGames();
			if (behaviourViewModel.Mtu.RefreshCommand.CanExecute(null))
				behaviourViewModel.Mtu.RefreshCommand.Execute(null);
		}
	}

	private void OnPageUnloaded(object sender, RoutedEventArgs e)
	{
		_settingsRevision = App.Settings.Revision;
		if (base.DataContext is BehaviourViewModel behaviourViewModel)
			behaviourViewModel.Mtu.RefreshCommand.Cancel();
	}
}
