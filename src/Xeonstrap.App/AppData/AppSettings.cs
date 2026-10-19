using Xeonstrap.Enums;

namespace Xeonstrap.AppData;

public class AppSettings
{
	public string CustomFontLocation { get; set; } = string.Empty;

	public CursorType CursorType { get; set; }

	public bool UseFastFlagManager { get; set; }

	public bool XeonstrapRPCReal { get; set; }

	public bool WPFSoftwareRender { get; set; }

	public string Locale { get; set; } = "nil";

	public string? SelectedCustomTheme { get; set; }
}
