using Xeonstrap.Models.Attributes;

namespace Xeonstrap.Enums;

public enum BootstrapperStyle
{
	VistaDialog,
	LegacyDialog2008,
	LegacyDialog2011,
	ProgressDialog,
	ClassicFluentDialog,
	ByfronDialog,
	[EnumName(StaticName = "Xeonstrap")]
	FluentDialog,
	FluentAeroDialog,
	CustomDialog
}
