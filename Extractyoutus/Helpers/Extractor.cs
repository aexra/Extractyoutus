using Extractyoutus.Views;
using Windows.Storage;
using Windows.Storage.Pickers;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Extractyoutus.Controls;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace Extractyoutus.Helpers;
public class Extractor : INotifyPropertyChanged
{
    private static Extractor Instance = null;

    public ObservableCollection<DownloadControl> DownloadControls { get; set; } = new();

    public Queue<IVideo> DownloadQueue { get; set; } = new();

    public int downloadsCount = 0;
    public int processedCount = 0;
    public int skippedCount = 0;
    public bool isLoading = false;
    public bool isDownloading = false;

    public int DownloadsCount
    {
        get => downloadsCount;
        set
        {
            if (downloadsCount != value)
            {
                downloadsCount = value;
                NotifyPropertyChanged();
            }
        }
    }
    public int ProcessedCount
    {
        get => processedCount;
        set
        {
            if (processedCount != value)
            {
                processedCount = value;
                NotifyPropertyChanged();
            }
        }
    }
    public int SkippedCount
    {
        get => skippedCount;
        set
        {
            if (skippedCount != value)
            {
                skippedCount = value;
                NotifyPropertyChanged();
            }
        }
    }
    public bool IsLoading
    {
        get => isLoading;
        set
        {
            if (isLoading != value)
            {
                isLoading = value;
                NotifyPropertyChanged();
            }
        }
    }
    public bool IsDownloading
    {
        get => isDownloading;
        set
        {
            if (isDownloading != value)
            {
                isDownloading = value;
                NotifyPropertyChanged();
            }
        }
    }

    private static YoutubeClient _client;
    private static HttpClient _httpClient;

    private DispatcherQueue DispatcherQueue;

    public event PropertyChangedEventHandler PropertyChanged;

    public static Extractor GetInstance() => Instance ??= new Extractor();

    private Extractor()
    {
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Init();
    }

    public void Init()
    {
        _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _client = new YoutubeClient(_httpClient);
    }

    public void Restart()
    {
        Init();
    }

    public PlaylistId? IsPlaylist(string url)
    {
        return PlaylistId.TryParse(url);
    }

    public VideoId? IsVideo(string url)
    {
        return VideoId.TryParse(url);
    }

    public async Task<Playlist> GetPlaylistInfoAsync(PlaylistId id)
    {
        return await _client.Playlists.GetAsync(id);
    }

    public async Task<Channel> GetChannelAsync(ChannelId id)
    {
        return await _client.Channels.GetAsync(id);
    }

    public async Task<Video> GetVideoAsync(VideoId id)
    {
        return await _client.Videos.GetAsync(id);
    }

    public async Task<StreamManifest> GetStreamManifestAsync(VideoId id)
    {
        return await _client.Videos.Streams.GetManifestAsync(id);
    }

    public async Task Download(AudioOnlyStreamInfo streamInfo, string path, IProgress<double>? progress = null)
    {
        await _client.Videos.Streams.DownloadAsync(streamInfo, path, progress);
    }

    public async Task Download(AudioOnlyStreamInfo streamInfo, StorageFile file, IProgress<double>? progress = null)
    {
        using var stream = await file.OpenStreamForWriteAsync();
        await _client.Videos.Streams.CopyToAsync(streamInfo, stream, progress);
    }

    public async Task EnqueuePlaylist(PlaylistId playlistId)
    {
        IsLoading = true;
        var videos = await _client.Playlists.GetVideosAsync(playlistId);
        IsLoading = false;

        foreach (var video in videos)
        {
            if (video != null)
            {
                DownloadQueue.Enqueue(video);
            }
            else
            {
                ProcessedCount++;
                SkippedCount++;
            }
        }

        if (!IsDownloading)
        {
            Task.Run(() => DispatcherQueue.TryEnqueue(() => DownloadLoop()));
        }
    }

    private async Task DownloadLoop()
    {
        IsDownloading = true;

        while (DownloadQueue.Count != 0)
        {
            await SelectFolderIfNotPicked();
            var path = (string)ApplicationData.Current.LocalSettings.Values["extractor_folder"];

            var video = DownloadQueue.Dequeue();

            var result = await ExtractAudio(path, video);

            if (result == 1)
            {
                result = await ExtractAudio(path, video);
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

        IsDownloading = false;
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

    private async Task<int> ExtractAudio(string path, IVideo video)
    {
        DownloadControl downloadControl = null;

        downloadControl = new();
        downloadControl.Title = video.Title;
        downloadControl.AuthorName = video.Author.Title;
        downloadControl.ImageSource = video.Thumbnails.GetWithHighestResolution().Url;
        downloadControl.AuthorImageSource = (await GetChannelAsync(video.Author.ChannelId)).Thumbnails.GetWithHighestResolution().Url;

        DownloadControls.Insert(0, downloadControl);
        
        try
        {
            var manifest = await GetStreamManifestAsync(video.Id);
            var audioStreamInfo = manifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            var folder = await StorageFolder.GetFolderFromPathAsync(path);

            var file = await folder.CreateFileAsync($"{FileNameHelper.MakeValidFileName(video.Title)}.mp3", CreationCollisionOption.ReplaceExisting);

            await Download(audioStreamInfo, file, new Progress<double>((progress) =>
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
                Restart();
                return 1;
            }

            return 2;

            // HERE I RETURN:
            // 0 IF EVERYTHING IS OK
            // 1 IF TIMEOUTEXCEPTION
            // 2 IF ANY OTHER EXCEPTION
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
