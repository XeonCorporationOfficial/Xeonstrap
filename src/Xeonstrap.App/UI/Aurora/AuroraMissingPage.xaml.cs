using System.Windows.Controls;

namespace Xeonstrap.UI.Aurora;

/// <summary>
/// Fallback surface shown when a rail entry has no page behind it yet. It keeps the shell
/// usable while the individual pages are migrated instead of failing silently.
/// </summary>
public partial class AuroraMissingPage : Page
{
	public AuroraMissingPage()
		: this("Not available")
	{
	}

	public AuroraMissingPage(string heading)
	{
		InitializeComponent();
		Title = heading;
		Heading.Text = heading;
	}
}
