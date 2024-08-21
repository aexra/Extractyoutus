using Extractyoutus.Controls;
using System.Collections.ObjectModel;

namespace Extractyoutus.Helpers;

public class DataHelper
{
    public static ObservableCollection<DownloadControl> Downloads { get; set; } = new();
    public static int DownloadsCount = 0;
    public static int ProcessedCount = 0;
    public static int SkippedCount = 0;
    public static bool IsLoading = false;
}
