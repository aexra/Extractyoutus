using Extractyoutus.Controls;
using System.Collections.ObjectModel;

namespace Extractyoutus.Helpers;

public class DataHelper
{
    public static ObservableCollection<DownloadControl> Downloads { get; set; } = new();
}
