using System;
using System.Text.Json.Serialization;

namespace Xeonstrap.Models.APIs.Roblox;

internal sealed class ThumbnailBatchResponse
{
	[JsonPropertyName("data")]
	public ThumbnailResponse[] Data { get; set; } = Array.Empty<ThumbnailResponse>();
}
