using Xeonstrap.Models.Attributes;

namespace Xeonstrap.Enums.FlagPresets;

public enum ProfileMode
{
	[EnumName(FromTranslation = "Common.Automatic")]
	Default,
	[EnumName(StaticName = "Xeonstraps Official")]
	Xeonstrap,
	[EnumName(StaticName = "Stoofs")]
	Stoof
}
