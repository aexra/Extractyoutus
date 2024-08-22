using Extractyoutus.Dialogs;
using Extractyoutus.Helpers;
using Extractyoutus.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;

namespace Extractyoutus.Views;

public sealed partial class MainPage : Page
{
    private Extractor Extractor => Extractor.GetInstance();

    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }

    private async void URIBox_Paste(object sender, TextControlPasteEventArgs e)
    {
        var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        for (var i = 0; i < 10; i++)
        {
            try
            {
                if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
                {
                    var text = await dataPackageView.GetTextAsync();

                    await Download(text);

                    return;
                }
            }
            catch (Exception ex)
            {
                ShellPage.Notify("Error", ex.Message);
                return;
            }
            System.Threading.Thread.Sleep(10);
        }
    }
    private async void URIBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await Download((sender as TextBox).Text);
        }
    }

    public async Task Download(string url)
    {
        var playlistId = Extractor.IsPlaylist(url);
        var videoId = Extractor.IsVideo(url);

        if (playlistId != null || videoId != null)
        {
            URIBox.Text = string.Empty;
        }

        // DOWNLOAD PLAYLIST
        if (playlistId != null)
        {
            var meta = await Extractor.GetPlaylistInfoAsync(playlistId.Value);

            if (await ShowDownloadPlaylistDialog(meta))
            {
                await Extractor.EnqueuePlaylist(playlistId.Value);
                return;
            }
        }
        // DOWNLOAD VIDEO
        if (videoId != null)
        {

        }
    }

    private async Task<bool> ShowDownloadPlaylistDialog(Playlist meta)
    {
        var dialog = new ContentDialog();
        var content = new FoundYTObjectContent();

        content.Title = meta.Title;
        content.Description = meta.Description;
        content.AuthorName = meta.Author.Title;
        content.ImageSource = meta.Thumbnails.GetWithHighestResolution().Url;
        content.AuthorImageSource = (await Extractor.GetChannelAsync(meta.Author.ChannelId)).Thumbnails.GetWithHighestResolution().Url;

        dialog.XamlRoot = this.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "FoundPlaylist".GetLocalized();
        dialog.PrimaryButtonText = "Download".GetLocalized();
        dialog.CloseButtonText = "Cancel".GetLocalized();
        dialog.Content = content;
        dialog.DefaultButton = ContentDialogButton.Primary;

        var result = await dialog.ShowAsync();

        return result == ContentDialogResult.Primary;
    }
}
