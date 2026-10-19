using System.Text.Json.Serialization;

namespace Xeonstrap.Models.APIs.Roblox;

public class UniverseIdResponse
{
	[JsonPropertyName("universeId")]
	public long UniverseId { get; set; }
}
