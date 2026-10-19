using System;

namespace Xeonstrap.Models.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class EnumNameAttribute : Attribute
{
	public string? StaticName { get; set; }

	public string? FromTranslation { get; set; }
}
