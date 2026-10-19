using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Xeonstrap.Models.APIs.Roblox;

public class ClientFlagSettings
{
	[JsonPropertyName("applicationSettings")]
	public Dictionary<string, string>? ApplicationSettings { get; set; }
}
