using System.Windows;
using System.Windows.Data;
using Xeonstrap.Resources;
using Wpf.Ui.Controls;

namespace Xeonstrap.UI;

internal static class BreadcrumbHeader
{
	public static void SetLocalized(DependencyObject page, string key)
	{
		BindingOperations.SetBinding(page, Breadcrumb.HeaderProperty, new Binding($"[{key}]") { Source = LocalizedStrings.Instance, Mode = BindingMode.OneWay });
	}

	public static void Set(DependencyObject page, string? text)
	{
		BindingOperations.ClearBinding(page, Breadcrumb.HeaderProperty);
		Breadcrumb.SetHeader(page, text);
	}

	public static void Clear(DependencyObject page)
	{
		BindingOperations.ClearBinding(page, Breadcrumb.HeaderProperty);
		page.ClearValue(Breadcrumb.HeaderProperty);
	}
}
