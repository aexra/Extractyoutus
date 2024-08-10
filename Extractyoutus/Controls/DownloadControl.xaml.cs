using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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

    private void PB_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (Progress == 100)
        {
            var bar = (ProgressBar)sender;
            bar.Foreground = new SolidColorBrush(Colors.LightGreen);
        }
    }
}
