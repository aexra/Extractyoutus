using System.Collections.ObjectModel;
using Extractyoutus.Controls;
using Extractyoutus.Dialogs;
using Extractyoutus.Helpers;
using Extractyoutus.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using Windows.Storage;
using Windows.Storage.Pickers;
using YoutubeExplode.Videos.Streams;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Extractyoutus.Views;

public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    public ObservableCollection<DownloadControl> Downloads => DataHelper.Downloads;
    public event PropertyChangedEventHandler PropertyChanged;

    private int DownloadsCount
    {
        get => DataHelper.DownloadsCount;
        set
        {
            if (value != DataHelper.DownloadsCount)
            {
                DataHelper.DownloadsCount = value;
                NotifyPropertyChanged();
            }
        }
    }
    private int ProcessedCount
    {
        get => DataHelper.ProcessedCount;
        set
        {
            if (value != DataHelper.ProcessedCount)
            {
                DataHelper.ProcessedCount = value;
                NotifyPropertyChanged();
            }
        }
    }
    private int SkippedCount
    {
        get => DataHelper.SkippedCount;
        set
        {
            if (value != DataHelper.SkippedCount)
            {
                DataHelper.SkippedCount = value;
                NotifyPropertyChanged();
            }
        }
    }
    private bool IsLoading
    {
        get => DataHelper.IsLoading;
        set
        {
            if (DataHelper.IsLoading != value)
            {
                DataHelper.IsLoading = value;
                NotifyPropertyChanged();
            }
        }
    }

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

    private async Task Download(string url)
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
                DownloadsCount += meta.Count ?? 0;
                await DownloadPlaylist(playlistId.Value);
                return;
            }
        }
        // DOWNLOAD VIDEO
        if (videoId != null)
        {

        }
    }

    private async Task DownloadPlaylist(PlaylistId playlistId)
    {
        IsLoading = true;
        var playlist = await Extractor.GetPlaylistVideos(playlistId);
        IsLoading = false;

        foreach (var video in playlist)
        {
            if (video != null)
            {
                await SelectFolderIfNotPicked();
                var path = (string)ApplicationData.Current.LocalSettings.Values["extractor_folder"];

                var result = await DownloadPlaylistVideo(path, video);

                if (result == 1)
                {
                    result = await DownloadPlaylistVideo(path, video);
                }

                if (result == 0)
                {
                    ProcessedCount++;
                }
                else
                {
                    ProcessedCount++;
                    SkippedCount++;
                }
            }
            else
            {
                SkippedCount++;
            }
        }
    }

    private async Task SelectFolderIfNotPicked()
    {
        if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("extractor_folder"))
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add("*");

            var hwnd = App.MainWindow.GetWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            ApplicationData.Current.LocalSettings.Values["extractor_folder"] = folder.Path;
        }
    }

    private async Task<int> DownloadPlaylistVideo(string path, PlaylistVideo video)
    {
        var downloadControl = new DownloadControl();
        downloadControl.Title = video.Title;
        downloadControl.AuthorName = video.Author.Title;
        downloadControl.ImageSource = video.Thumbnails.GetWithHighestResolution().Url;
        downloadControl.AuthorImageSource = (await Extractor.GetChannelAsync(video.Author.ChannelId)).Thumbnails.GetWithHighestResolution().Url;

        Downloads.Insert(0, downloadControl);

        try
        {
            var manifest = await Extractor.GetStreamManifestAsync(video.Id);
            var audioStreamInfo = manifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            var folder = await StorageFolder.GetFolderFromPathAsync(path);

            var file = await folder.CreateFileAsync($"{FileNameHelper.MakeValidFileName(video.Title)}.mp3", CreationCollisionOption.ReplaceExisting);

            await Extractor.Download(audioStreamInfo, file, new Progress<double>((progress) =>
            {
                downloadControl.Progress = progress * 100;
            }));

            return 0;
        }
        catch (Exception ex)
        {
#if DEBUG
            ShellPage.Notify("Error", ex.Message);
#endif

            downloadControl.ThrowFailure();

            if (ex is TimeoutException)
            {
                Extractor.Restart();
                return 1;
            }

            return 2;

            // HERE I RETURN:
            // 0 IF EVERYTHING IS OK
            // 1 IF TIMEOUTEXCEPTION
            // 2 IF ANY OTHER EXCEPTION
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

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
