using System;
using System.Collections.Generic;
using System.Windows;
using Wpf.Ui.Common;
using Xeonstrap.Resources;
using Xeonstrap.Utility;

namespace Xeonstrap.UI.Aurora;

/// <summary>
/// One row in the Aurora navigation rail. The rail groups entries by <see cref="Group"/>,
/// which becomes a sticky section header in the shell.
/// </summary>
public sealed class AuroraNavEntry
{
	public string Group { get; init; } = "Play";

	public string Title { get; init; } = string.Empty;

	/// <summary>Stable identifier, also used as the page cache key.</summary>
	public string Key { get; init; } = string.Empty;

	public SymbolRegular Icon { get; init; } = SymbolRegular.Document24;

	/// <summary>Page hosted in the shell frame. Null for command entries.</summary>
	public Type? PageType { get; init; }

	/// <summary>True when selecting the entry runs an action instead of navigating.</summary>
	public bool IsCommand { get; init; }

	public string Badge { get; init; } = string.Empty;

	public Visibility BadgeVisibility => string.IsNullOrEmpty(Badge) ? Visibility.Collapsed : Visibility.Visible;

	public override string ToString() => Title;
}

/// <summary>
/// Declarative model of the Aurora rail. Every item that existed in the legacy sidebar is
/// still reachable: the primary surfaces sit in four groups, the secondary ones are opened
/// from the command palette or from the pages that link to them.
/// </summary>
public static class AuroraNavigation
{
	public static IReadOnlyList<AuroraNavEntry> Create()
	{
		List<AuroraNavEntry> entries =
		[
			// Play: everything a user touches before a session starts.
			new AuroraNavEntry
			{
				Group = "Play",
				Key = "home",
				Title = Strings.Ui_Nav_Home,
				Icon = SymbolRegular.Home24,
				PageType = typeof(Xeonstrap.UI.Aurora.AuroraHomePage)
			},
			new AuroraNavEntry
			{
				Group = "Play",
				Key = "library",
				Title = Strings.Ui_Pages_Library,
				Icon = SymbolRegular.Apps24,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.LibraryPage)
			},
			new AuroraNavEntry
			{
				Group = "Play",
				Key = "news",
				Title = Strings.Ui_Nav_News,
				Icon = SymbolRegular.News20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.NewsPage)
			},

			// Tune: how Roblox looks, feels and connects.
			new AuroraNavEntry
			{
				Group = "Tune",
				Key = "integrations",
				Title = Strings.Menu_Integrations_Title,
				Icon = SymbolRegular.Add12,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.IntegrationsPage)
			},
			new AuroraNavEntry
			{
				Group = "Tune",
				Key = "deployment",
				Title = Strings.Common_Deployment,
				Icon = SymbolRegular.PlaySettings20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.BehaviourPage)
			},
			new AuroraNavEntry
			{
				Group = "Tune",
				Key = "appearance",
				Title = Strings.Menu_Appearance_Title,
				Icon = SymbolRegular.Color24,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.AppearancePage)
			},
			new AuroraNavEntry
			{
				Group = "Tune",
				Key = "mods",
				Title = Strings.Menu_Mods_Title,
				Icon = SymbolRegular.WrenchScrewdriver20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.ModsPage)
			},

			// Flags: the configuration surface, split the same way it is today.
			new AuroraNavEntry
			{
				Group = "Flags",
				Key = "fastflags",
				Title = Strings.Ui_Nav_FastFlagSettings,
				Icon = SymbolRegular.Settings28,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.FastFlagsPage)
			},
			new AuroraNavEntry
			{
				Group = "Flags",
				Key = "fastflags-editor",
				Title = Strings.Ui_Nav_FastFlagEditor,
				Icon = SymbolRegular.Flag28,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.FastFlagEditorPage)
			},
			new AuroraNavEntry
			{
				Group = "Flags",
				Key = "global",
				Title = Strings.Ui_Nav_Global,
				Icon = SymbolRegular.GlobeLocation20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.GBSEditorPage)
			},

			// System: install, extend, configure the app itself.
			new AuroraNavEntry
			{
				Group = "System",
				Key = "manager",
				Title = Strings.Ui_Nav_Manager,
				Icon = SymbolRegular.ArrowDownload24,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.DownloadsPage)
			},
			new AuroraNavEntry
			{
				Group = "System",
				Key = "extensions",
				Title = Strings.Ui_Nav_Extensions,
				Icon = SymbolRegular.CubeAdd20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.ExtensionPage)
			},
			new AuroraNavEntry
			{
				Group = "System",
				Key = "shortcuts",
				Title = Strings.Common_Shortcuts,
				Icon = SymbolRegular.Apps32,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.ShortcutsPage)
			},
			new AuroraNavEntry
			{
				Group = "System",
				Key = "settings",
				Title = Strings.Ui_Nav_Settings,
				Icon = SymbolRegular.Settings28,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.ChannelPage)
			},
			new AuroraNavEntry
			{
				Group = "System",
				Key = "about",
				Title = Strings.Menu_About_Title,
				Icon = SymbolRegular.QuestionCircle32,
				PageType = typeof(Xeonstrap.UI.Elements.About.Pages.AboutPage)
			}
		];

		if (Xeonstrap.Utility.Platform.IsLinux)
		{
			entries.Add(new AuroraNavEntry
			{
				Group = "System",
				Key = "sober",
				Title = Strings.Menu_Sober_Title,
				Icon = SymbolRegular.Settings20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.SoberPage)
			});
		}

		// Secondary surfaces. They used to live under the nested "More" menu; here they get
		// their own group so the shell can still reach every page the old sidebar exposed.
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "history",
			Title = "History",
			Icon = SymbolRegular.Document24,
			PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.HistoryPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "roblox-news",
			Title = "Roblox news",
			Icon = SymbolRegular.News20,
			PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.RobloxNewsPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "releases",
			Title = "Releases",
			Icon = SymbolRegular.ArrowDownload24,
			PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.ReleasesPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "help",
			Title = "Help",
			Icon = SymbolRegular.QuestionCircle32,
			PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.HelpPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "support",
			Title = "Support",
			Icon = SymbolRegular.Info24,
			PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.DonoPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "supporters",
			Title = "Supporters",
			Icon = SymbolRegular.People24,
			PageType = typeof(Xeonstrap.UI.Elements.About.Pages.SupportersPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "translators",
			Title = "Translators",
			Icon = SymbolRegular.Globe24,
			PageType = typeof(Xeonstrap.UI.Elements.About.Pages.TranslatorsPage)
		});
		entries.Add(new AuroraNavEntry
		{
			Group = "More",
			Key = "licenses",
			Title = "Licenses",
			Icon = SymbolRegular.Document24,
			PageType = typeof(Xeonstrap.UI.Elements.About.Pages.LicensesPage)
		});

		if (Xeonstrap.Utility.Platform.IsWindows)
		{
			entries.Add(new AuroraNavEntry
			{
				Group = "More",
				Key = "nvidia",
				Title = "NVIDIA settings",
				Icon = SymbolRegular.Settings28,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.NvidiaFastFlagsPage)
			});
			entries.Add(new AuroraNavEntry
			{
				Group = "More",
				Key = "overlays",
				Title = "Overlays",
				Icon = SymbolRegular.Color24,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.OverlaysPage)
			});
			entries.Add(new AuroraNavEntry
			{
				Group = "More",
				Key = "mod-generator",
				Title = "Mod generator",
				Icon = SymbolRegular.WrenchScrewdriver20,
				PageType = typeof(Xeonstrap.UI.Elements.Settings.Pages.ModGeneratorPage)
			});
		}

		return entries;
	}
}
