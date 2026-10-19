using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Xeonstrap.UI.Elements.Base;
using Xeonstrap.UI.Elements.Controls;
using Windows.Win32;
using Windows.Win32.Foundation;
using Wpf.Ui.Controls;

namespace Xeonstrap.UI.Elements.Dialogs;

public partial class ConnectivityDialog : WpfUiWindow{

	public ConnectivityDialog(string title, string description, MessageBoxImage image, Exception exception)
	{
		InitializeComponent();
		string? text = null;
		SystemSound? systemSound = null;
		switch (image)
		{
		case MessageBoxImage.Hand:
			text = "Error";
			systemSound = Xeonstrap.Utility.SafeSystemSounds.Get(MessageBoxImage.Hand);
			break;
		case MessageBoxImage.Question:
			text = "Question";
			systemSound = Xeonstrap.Utility.SafeSystemSounds.Get(MessageBoxImage.Question);
			break;
		case MessageBoxImage.Exclamation:
			text = "Warning";
			systemSound = Xeonstrap.Utility.SafeSystemSounds.Get(MessageBoxImage.Exclamation);
			break;
		case MessageBoxImage.Asterisk:
			text = "Information";
			systemSound = Xeonstrap.Utility.SafeSystemSounds.Get(MessageBoxImage.Asterisk);
			break;
		}
		if (text == null)
		{
			IconImage.Visibility = Visibility.Collapsed;
		}
		else
		{
			IconImage.Source = Xeonstrap.Utility.SafeImaging.FromUri(new Uri("pack://application:,,,/Resources/MessageBox/" + text + ".png"));
		}
		TitleTextBlock.Text = title;
		DescriptionTextBlock.MarkdownText = description;
		AddException(exception);
		Xeonstrap.UI.LinuxRichTextFallback.Apply(ErrorRichTextBox);
		CloseButton.Click += OnCloseButtonClick;
		Xeonstrap.Utility.SafeSystemSounds.Play(systemSound);
		base.Loaded += OnLoaded;
		base.Closed += OnClosed;
	}

	private void OnCloseButtonClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (Xeonstrap.Utility.Platform.IsWindows) { Windows.Win32.PInvoke.FlashWindow((HWND)new WindowInteropHelper(this).Handle, true); }
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		CloseButton.Click -= OnCloseButtonClick;
		base.Loaded -= OnLoaded;
		base.Closed -= OnClosed;
	}

	private void AddException(Exception exception, bool inner = false)
	{
		if (!inner)
		{
			ErrorRichTextBox.Selection.Text = $"{exception.GetType()}: {exception.Message}";
		}
		if (exception.InnerException != null)
		{
			ErrorRichTextBox.Selection.Text += $"\n\n[Inner Exception]\n{exception.InnerException.GetType()}: {exception.InnerException.Message}";
			AddException(exception.InnerException, inner: true);
		}
	}
}
