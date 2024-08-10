using Microsoft.UI.Xaml.Controls;

namespace Extractyoutus.Controls;
public sealed partial class DownloadControl : UserControl
{
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public string AuthorImageSource { get; set; }
    public string ImageSource { get; set; }

    public double Progress
    {
        get => PB.Value;
        set => PB.Value = value;
    }

    public DownloadControl()
    {
        this.InitializeComponent();
    }
}
