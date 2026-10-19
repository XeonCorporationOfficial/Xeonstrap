using Xeonstrap.Enums;

namespace Xeonstrap.Extensions;

internal static class CustomThemeTemplateEx
{
	public static string GetFileName(this CustomThemeTemplate template)
	{
		return $"CustomBootstrapperTemplate_{template}.xml";
	}
}
