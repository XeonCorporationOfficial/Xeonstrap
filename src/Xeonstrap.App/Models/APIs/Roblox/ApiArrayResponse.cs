using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Xeonstrap.Models.APIs.Roblox;

public class ApiArrayResponse<T>
{
	[JsonPropertyName("data")]
	public IEnumerable<T> Data { get; set; } = null!;
}
