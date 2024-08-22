using Extractyoutus.Enums;
using Extractyoutus.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.UI;
using YoutubeExplode.Videos;

namespace Extractyoutus.Controls;
public sealed partial class DownloadControl : UserControl
{
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public string AuthorImageSource { get; set; }
    public string ImageSource { get; set; }

    public StorageFile File { get; set; }
    public IVideo Video { get; set; }

    public DownloadState State { get; set; } = DownloadState.Idle;

    public double Progress
    {
        get => PB.Value;
        set => PB.Value = value;
    }

    public DownloadControl()
    {
        this.InitializeComponent();
    }

    public void ResetProgress()
    {
        State = DownloadState.Idle;
        PB.Value = 0;

        var accentColor = (Color)Application.Current.Resources["SystemAccentColor"];
        PB.Foreground = new SolidColorBrush(accentColor);
    }
    public void ThrowFailure()
    {
        State = DownloadState.Failure;
        PB.Value = 100;
    }

    private void PB_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (Progress == 100)
        {
            var bar = (ProgressBar)sender;

            if (State == DownloadState.Failure)
            {
                bar.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                bar.Foreground = new SolidColorBrush(Colors.LightGreen);
                State = DownloadState.Success;
            }
        }
    }
    private async void ReloadFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ResetProgress();
        await Extractor.GetInstance().ForceExtract(Video, this);
    }
}
