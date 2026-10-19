using System.Text.Json.Serialization;

namespace XeonstrapClient.WebServer;

internal class AssetLocation
{
	[JsonPropertyName("location")]
	public string? Location { get; set; }
}
