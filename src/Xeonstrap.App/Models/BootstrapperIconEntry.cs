using System.Windows.Media;
using Xeonstrap.Enums;
using Xeonstrap.Extensions;

namespace Xeonstrap.Models;

public class BootstrapperIconEntry
{
	public BootstrapperIcon IconType { get; set; }

	public ImageSource ImageSource => IconEx.GetIconSource(IconType);
}
