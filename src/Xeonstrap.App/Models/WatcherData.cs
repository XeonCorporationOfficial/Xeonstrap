using System.Collections.Generic;

namespace Xeonstrap.Models;

internal sealed class WatcherData
{
	public int ProcessId { get; set; }

	public string? LogFile { get; set; }

	public List<int>? AutoclosePids { get; set; }
}
