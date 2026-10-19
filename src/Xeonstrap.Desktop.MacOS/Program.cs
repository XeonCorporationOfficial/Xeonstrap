using Avalonia;
using Xeonstrap.Desktop;
using Xeonstrap.Platform.MacOS;

namespace Xeonstrap.Desktop.MacOS;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		DesktopRuntime.Configure(new MacOSPlatformHost(), args);
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<DesktopApplication>().UsePlatformDetect();
	}
}
