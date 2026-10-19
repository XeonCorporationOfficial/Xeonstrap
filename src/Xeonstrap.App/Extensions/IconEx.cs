using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xeonstrap.Enums;
using Xeonstrap.Resources;
using Xeonstrap.UI;

namespace Xeonstrap.Extensions;

public static class IconEx
{
	public static Icon GetSized(this Icon icon, int width, int height)
	{
		if (!Xeonstrap.Utility.Platform.IsWindows)
		{
			return icon;
		}
		return new Icon(icon, new Size(width, height));
	}

	public static ImageSource GetBootstrapperWindowIcon()
	{
		return GetIconSource(App.Settings.Prop.ActiveBootstrapperIcon);
	}

	internal static BitmapSource? LoadPortableIcon(BootstrapperIcon icon, int decodeWidth)
	{
		if (icon == BootstrapperIcon.IconCustom)
		{
			string custom = App.Settings.Prop.BootstrapperIconCustomLocation;
			if (!string.IsNullOrEmpty(custom) && File.Exists(custom))
			{
				try
				{
					BitmapSource? loaded = Xeonstrap.Utility.SafeImaging.FromBytes(File.ReadAllBytes(custom), decodeWidth);
					if (loaded != null)
					{
						return loaded;
					}
				}
				catch (Exception ex)
				{
					App.Logger?.WriteLine("IconEx::LoadPortableIcon", "Could not read the custom icon: " + ex.Message);
				}
			}
			icon = BootstrapperIcon.IconXeonstrap;
		}

		if (icon == BootstrapperIcon.IconXeonstrap)
		{
			BitmapSource? branded = Xeonstrap.Utility.Branding.LoadIcon(decodeWidth);
			if (branded != null)
			{
				return branded;
			}
		}

		string assembly = typeof(IconEx).Assembly.GetName().Name ?? "Xeonstrap";
		foreach (string candidate in new[]
		{
			"pack://application:,,,/Resources/" + icon + ".ico",
			"pack://application:,,,/" + assembly + ";component/Resources/" + icon + ".ico"
		})
		{
			BitmapSource? source = Xeonstrap.Utility.SafeImaging.FromUri(new Uri(candidate, UriKind.Absolute), decodeWidth);
			if (source != null)
			{
				return source;
			}
		}

		return null;
	}

	public static ImageSource GetIconSource(BootstrapperIcon icon)
	{
		if (!Xeonstrap.Utility.Platform.IsWindows)
		{
			BitmapSource? portable = LoadPortableIcon(icon, 128);
			if (portable != null)
			{
				return portable;
			}
		}

		if (icon == BootstrapperIcon.IconCustom)
		{
			string custom = App.Settings.Prop.BootstrapperIconCustomLocation;
			if (!string.IsNullOrEmpty(custom) && File.Exists(custom))
			{
				ImageSource? loaded = DecodeIcon(() => File.OpenRead(custom));
				if (loaded != null)
				{
					return loaded;
				}
			}
			icon = BootstrapperIcon.IconXeonstrap;
		}

		if (Xeonstrap.Utility.Platform.IsWindows)
		{
			try
			{
				return icon.GetIcon().GetImageSource();
			}
			catch (Exception ex)
			{
				App.Logger?.WriteLine("IconEx::GetIconSource", "System.Drawing fallback failed: " + ex.Message);
			}
		}

		Uri packUri = new("pack://application:,,,/Xeonstrap.png", UriKind.Absolute);
		if (!Xeonstrap.Utility.Platform.IsWindows)
		{
			ImageSource? decoded = DecodeIcon(() => System.Windows.Application.GetResourceStream(packUri)?.Stream);
			if (decoded != null)
			{
				return decoded;
			}
		}

		return Xeonstrap.Utility.SafeImaging.FromUri(packUri)!;
	}

	private static ImageSource? DecodeIcon(Func<Stream?> open)
	{
		try
		{
			using Stream? stream = open();
			if (stream == null)
			{
				return null;
			}
			using MemoryStream buffer = new MemoryStream();
			stream.CopyTo(buffer);
			buffer.Position = 0L;
			ImageSource source = GetLargestFrame(buffer);
			source.Freeze();
			return source;
		}
		catch
		{
			return null;
		}
	}

	public static ImageSource GetImageSource(this Icon icon, bool handleException = true)
	{
		using MemoryStream memoryStream = new MemoryStream();
		icon.Save(memoryStream);
		memoryStream.Position = 0L;
		if (handleException)
		{
			try
			{
				return GetLargestFrame(memoryStream);
			}
			catch (Exception ex)
			{
				App.Logger.WriteException("IconEx::GetImageSource", ex);
				Frontend.ShowMessageBox(string.Format(Strings.Dialog_IconLoadFailed, ex.Message));
				return BootstrapperIcon.IconXeonstrap.GetIcon().GetImageSource(handleException: false);
			}
		}
		return GetLargestFrame(memoryStream);
	}

	private static BitmapFrame GetLargestFrame(MemoryStream stream)
	{
		BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
		BitmapFrame best = decoder.Frames[0];
		foreach (BitmapFrame frame in decoder.Frames)
		{
			if (frame.PixelWidth > best.PixelWidth)
			{
				best = frame;
			}
		}
		return best;
	}
}
