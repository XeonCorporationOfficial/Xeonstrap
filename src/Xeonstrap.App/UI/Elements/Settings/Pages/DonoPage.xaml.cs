using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Wpf.Ui.Controls;

namespace Xeonstrap.UI.Elements.Settings.Pages;

public partial class DonoPage : UiPage{

	public DonoPage()
	{
		base.DataContext = new DonoPageViewModel();
		InitializeComponent();
	}
}
