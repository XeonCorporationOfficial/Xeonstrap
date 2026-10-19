using System;

namespace Xeonstrap.Models.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class EnumSortAttribute : Attribute
{
	public int Order { get; set; }
}
