using Windows.Storage;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Extractyoutus.Helpers;
public static class Extractor
{
    private static YoutubeClient _client;

    public static void Init()
    {
        _client = new YoutubeClient();
    }

    public static void Restart()
    {
        Init();
    }

    public static PlaylistId? IsPlaylist(string url)
    {
        return PlaylistId.TryParse(url);
    }

    public static VideoId? IsVideo(string url)
    {
        return VideoId.TryParse(url);
    }

    public static async Task<Playlist> GetPlaylistInfoAsync(PlaylistId id)
    {
        return await _client.Playlists.GetAsync(id);
    }

    public static async Task<IAsyncEnumerable<PlaylistVideo>> GetPlaylistVideos(PlaylistId id)
    {
        return _client.Playlists.GetVideosAsync(id);
    }

    public static async Task<Channel> GetChannelAsync(ChannelId id)
    {
        return await _client.Channels.GetAsync(id);
    }

    public static async Task<Video> GetVideoAsync(VideoId id)
    {
        return await _client.Videos.GetAsync(id);
    }

    public static async Task<StreamManifest> GetStreamManifestAsync(VideoId id)
    {
        return await _client.Videos.Streams.GetManifestAsync(id);
    }

    public static async Task Download(AudioOnlyStreamInfo streamInfo, string path, IProgress<double>? progress = null)
    {
        await _client.Videos.Streams.DownloadAsync(streamInfo, path, progress);
    }

    public static async Task Download(AudioOnlyStreamInfo streamInfo, StorageFile file, IProgress<double>? progress = null)
    {
        using var stream = await file.OpenStreamForWriteAsync();
        await _client.Videos.Streams.CopyToAsync(streamInfo, stream, progress);
    }
}
