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
    public int ProcessedCount => DownloadControls.Where(c => c.State != Enums.DownloadState.Idle).Count();
    public int SkippedCount => DownloadControls.Where(c => c.State == Enums.DownloadState.Failure).Count();
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
        return await ExecuteAsync(async (c) => await c.Playlists.GetAsync(id));
    }
    public async Task<Channel> GetChannelAsync(ChannelId id)
    {
        return await ExecuteAsync(async (c) => await c.Channels.GetAsync(id));
    }
    public async Task<Video> GetVideoAsync(VideoId id)
    {
        return await ExecuteAsync(async (c) => await c.Videos.GetAsync(id));
    }
    public async Task<StreamManifest> GetStreamManifestAsync(VideoId id)
    {
        return await ExecuteAsync(async (c) => await c.Videos.Streams.GetManifestAsync(id));
    }
    public async Task CopyToAsync(AudioOnlyStreamInfo streamInfo, StorageFile file, IProgress<double>? progress = null)
    {
        using var stream = await file.OpenStreamForWriteAsync();
        await ExecuteAsync(async (c) => await c.Videos.Streams.CopyToAsync(streamInfo, stream, progress));
    }

    public async Task EnqueuePlaylist(PlaylistId playlistId)
    {
        var videos = await _client.Playlists.GetVideosAsync(playlistId);

        var accessibleVideosCount = 0;

        foreach (var video in videos)
        {
            if (video != null)
            {
                DownloadQueue.Enqueue(video);
                accessibleVideosCount++;
            }
        }

        DownloadsCount += accessibleVideosCount;

        if (!IsDownloading)
        {
            Task.Run(() => DispatcherQueue.TryEnqueue(() => DownloadLoop()));
        }
    }
    public async Task ForceExtract(IVideo video, DownloadControl? downloadControl = null)
    {
        await SelectFolderIfNotPickedAsync();
        var path = (string)ApplicationData.Current.LocalSettings.Values["extractor_folder"];

        DispatcherQueue.TryEnqueue(async () =>
        {
            await ExtractAudio(path, video, downloadControl);
        });
    }

    private async Task<int> ExtractAudio(string path, IVideo video, DownloadControl? downloadControl = null)
    {
        var reload = true;

        if (downloadControl == null)
        {
            downloadControl = new DownloadControl();
            downloadControl.Title = video.Title;
            downloadControl.AuthorName = video.Author.Title;
            downloadControl.ImageSource = video.Thumbnails.GetWithHighestResolution().Url;
            downloadControl.Video = video;

            reload = false;
        }

        try
        {
            if (!reload)
            {
                downloadControl.AuthorImageSource = (await ExecuteAsync(async (c) => await c.Channels.GetAsync(video.Author.ChannelId))).Thumbnails.GetWithHighestResolution().Url;
                DownloadControls.Insert(0, downloadControl);
            }
            
            var manifest = await GetStreamManifestAsync(video.Id);
            var audioStreamInfo = manifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();

            var folder = await StorageFolder.GetFolderFromPathAsync(path);

            var file = await folder.CreateFileAsync($"{FileNameHelper.MakeValidFileName(video.Title)}.mp3", CreationCollisionOption.ReplaceExisting);
            downloadControl.File = file;

            await CopyToAsync(audioStreamInfo, file, new Progress<double>((progress) =>
            {
                downloadControl.Progress = progress * 100;
            }));

            NotifyPropertyChanged(nameof(ProcessedCount));
            NotifyPropertyChanged(nameof(SkippedCount));

            return 0;
        }
        catch (Exception ex)
        {
#if DEBUG
            ShellPage.Notify("Error", ex.Message);
#endif
            downloadControl.ThrowFailure();

            NotifyPropertyChanged(nameof(ProcessedCount));
            NotifyPropertyChanged(nameof(SkippedCount));

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

    private async Task DownloadLoop()
    {
        IsDownloading = true;

        while (DownloadQueue.Count != 0)
        {
            await SelectFolderIfNotPickedAsync();
            var path = (string)ApplicationData.Current.LocalSettings.Values["extractor_folder"];

            var video = DownloadQueue.Dequeue();

            var result = await ExtractAudio(path, video);

            if (result == 1)
            {
                await ExtractAudio(path, video);
            }
        }

        IsDownloading = false;
    }

    private async Task ExecuteAsync(Func<YoutubeClient, Task> request)
    {
        IsLoading = true;
        await request.Invoke(_client);
        IsLoading = false;
    }
    private async Task<T> ExecuteAsync<T>(Func<YoutubeClient, Task<T>> request)
    {
        IsLoading = true;
        var response = await request.Invoke(_client);
        IsLoading = false;
        return response;
    }

    private async Task SelectFolderIfNotPickedAsync()
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
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
