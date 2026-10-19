using System.Text.Json.Serialization;

namespace Xeonstrap.Models.XeonstrapRPC;

internal sealed class RichPresenceImage
{
	[JsonPropertyName("assetId")]
	public ulong? AssetId { get; set; }

	[JsonPropertyName("hoverText")]
	public string? HoverText { get; set; }

	[JsonPropertyName("clear")]
	public bool Clear { get; set; }

	[JsonPropertyName("reset")]
	public bool Reset { get; set; }

	public string? CustomKey { get; set; }
}
