using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xeonstrap.Integrations;
using Xeonstrap.Integrations.Nvidia;
using Xeonstrap.Models;

namespace Xeonstrap.UI.Elements.Settings.Pages
{
    internal static class NvidiaApplyFlow
    {
        public static async Task<bool> RunAsync(List<NvidiaEditorEntry> entries)
        {
            if (OperatingSystem.IsLinux())
                return await RunLinuxAsync();

            if (!NvidiaProfileInspector.IsAvailable)
            {
                Frontend.ShowMessageBox(
                    "The NVIDIA driver is not available on this system. " + NvidiaProfileInspector.UnavailableReason,
                    MessageBoxImage.Exclamation);
                return false;
            }

            NvidiaApplyResult result = Xeonstrap.Utility.ProcessElevation.IsAdministrator()
                ? await Task.Run(() => NvidiaProfileManager.ApplyToDriver(entries))
                : await NvidiaProfileManager.ApplyElevatedAsync(entries);

            Frontend.ShowMessageBox(
                Describe(result),
                result.Ok ? MessageBoxImage.Asterisk : MessageBoxImage.Exclamation);
            return result.Ok;
        }

        private static async Task<bool> RunLinuxAsync()
        {
            Xeonstrap.Utility.LinuxNvidiaSettings.Choices choices = Xeonstrap.Utility.LinuxNvidiaSettings.Read();
            string layer = Xeonstrap.Platform.Linux.LinuxEffectLayers.MangoHudLayerId;
            if (Xeonstrap.Utility.LinuxNvidiaSettings.OverlayConfiguration(choices.Overlay) is not null
                && !await Task.Run(() => Xeonstrap.Platform.Linux.LinuxEffectLayers.IsInstalled(layer)))
            {
                if (Xeonstrap.Utility.Platform.RuntimeHost is not { } host)
                {
                    Frontend.ShowMessageBox("The benchmark overlay needs the MangoHud Vulkan layer, which could not be installed from here.", MessageBoxImage.Exclamation);
                    return false;
                }

                if (Frontend.ShowMessageBox("The benchmark overlay needs the MangoHud Vulkan layer. Install it now?", MessageBoxImage.Question, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Xeonstrap.Platform.OperationResult installed = await Xeonstrap.Platform.Linux.LinuxEffectLayers.InstallAsync(host.Processes, layer);
                    if (!installed.Succeeded)
                    {
                        Frontend.ShowMessageBox("The MangoHud layer could not be installed.\n\n" + (installed.Failure?.Message ?? "The installation did not complete"), MessageBoxImage.Exclamation);
                        return false;
                    }
                }
            }

            Frontend.ShowMessageBox("The NVIDIA settings were saved. They apply the next time Roblox starts.", MessageBoxImage.Asterisk);
            return true;
        }

        public static string Describe(NvidiaApplyResult result)
        {
            if (result.Failures.Count == 0)
                return result.Message;

            StringBuilder text = new StringBuilder(result.Message);
            text.Append('\n');
            int shown = 0;
            foreach (string failure in result.Failures)
            {
                if (shown++ == 6)
                {
                    text.Append("\n... and ").Append(result.Failures.Count - 6).Append(" more");
                    break;
                }
                text.Append("\n- ").Append(failure);
            }
            return text.ToString();
        }
    }
}
