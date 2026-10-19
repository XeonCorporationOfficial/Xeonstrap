using System.Collections.Generic;

namespace Xeonstrap.UI.ViewModels.ContextMenu;

internal sealed class GamePassResponse
{
	public List<GamePassData> GamePasses { get; set; } = new List<GamePassData>();
}
