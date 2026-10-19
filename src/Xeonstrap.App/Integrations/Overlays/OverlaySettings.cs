using Xeonstrap.Integrations.AntiAliasing;
using Xeonstrap.Integrations.FrameGeneration;
using Xeonstrap.Integrations.MotionBlur;
using Xeonstrap.Integrations.RiShade;

namespace Xeonstrap.Integrations.Overlays
{
    public static class OverlaySettings
    {
        public static bool GameEffectsEnabled =>
            (App.Settings.Prop.RiShadeEnabled && RiShadeSettings.Current.HasVisibleEffects)
            || AntiAliasingSettings.MethodIndex > 0
            || MotionBlurSettings.StrengthIndex > 0
            || FrameGenSettings.ModeIndex > 0;

        public static bool HomepageBackgroundEnabled
		{
			get
			{
				if (!App.Settings.Prop.HomepageBackgroundOverlayEnabled)
					return false;
				if (!Xeonstrap.Utility.Platform.IsLinux || HomepageBackgroundMode != "Media")
					return true;
				string path = App.Settings.Prop.HomepageBackgroundOverlayMediaPath ?? string.Empty;
				return IsHomepageVideo(path) || Xeonstrap.Utility.LinuxHomepageShader.IsHighResolutionImage(path);
			}
		}

		public static readonly string[] HomepageVideoExtensions = [".mp4", ".m4v", ".webm", ".avi", ".mov", ".wmv", ".mpeg", ".mpg", ".mkv"];

		public static bool IsHomepageVideo(string? path)
		{
			return System.Linq.Enumerable.Contains(HomepageVideoExtensions, System.IO.Path.GetExtension(path ?? string.Empty), System.StringComparer.OrdinalIgnoreCase);
		}

		public static string HomepageBackgroundMode
		{
			get
			{
				string mode = App.Settings.Prop.HomepageBackgroundOverlayMode ?? "Auto";
				if (mode is "Solid" or "Gradient" or "Media")
					return mode;
				if (!string.IsNullOrWhiteSpace(App.Settings.Prop.HomepageBackgroundOverlayMediaPath))
					return "Media";
				return App.Settings.Prop.HomepageBackgroundOverlayGradientEnabled ? "Gradient" : "Solid";
			}
		}

        public static bool AnyEnabled => OverlayHub.InGame ? GameEffectsEnabled : HomepageBackgroundEnabled;

		public static bool RequiresLinuxX11Session => Xeonstrap.Utility.Platform.IsLinux
			&& (App.Settings.Prop.FakeExclusiveFullscreen
				|| App.Settings.Prop.FakeBorderlessFullscreen
				|| App.Settings.Prop.SoberAutoFullscreen
				|| App.Settings.Prop.SnapTapEnabled
				|| App.Settings.Prop.DuckRobloxAudioOnUnfocus
				|| App.Settings.Prop.SessionDockEnabled
				|| (App.Settings.Prop.XeonNotify && App.Settings.Prop.NotificationWindowShow && App.Settings.Prop.NotifyGameJoins)
				|| HomepageBackgroundEnabled
				|| App.Settings.Prop.SoberPreferXWayland);

		public static bool LinuxSessionIsWayland => Xeonstrap.Utility.Platform.IsLinux
			&& (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
				|| string.Equals(System.Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", System.StringComparison.OrdinalIgnoreCase));

		public static bool SoberHasX11Window => Xeonstrap.Utility.Platform.IsLinux
			&& (!LinuxSessionIsWayland
				|| (RequiresLinuxX11Session && !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("DISPLAY"))));

		public static string DescribeLinuxSoberSession(bool forcedX11)
		{
			if (forcedX11)
				return App.Settings.Prop.SoberPreferXWayland
					? "Run Roblox through XWayland is on, starting Sober on X11 so custom cursors and the Roblox window title and icon work"
					: "Window helpers or overlays are on, starting Sober on X11 so Xeonstrap can display overlays and receive the dock shortcut";
			if (LinuxSessionIsWayland && (LinuxRobloxWindow.IsEnabled || LinuxCustomCursorNeedsX11))
				return "Starting Sober natively on Wayland so the camera keeps the mouse locked, the custom cursor and Roblox window title need Run Roblox through XWayland";
			return "Starting Sober in its default display mode";
		}

		public static bool LinuxCustomCursorNeedsX11 => Xeonstrap.Utility.Platform.IsLinux
			&& App.Settings.Prop.CursorType != Xeonstrap.Enums.CursorType.Default
			&& App.Settings.Prop.ModApplyTarget is Xeonstrap.Enums.ModApplyTarget.Both or Xeonstrap.Enums.ModApplyTarget.Player;
    }
}
