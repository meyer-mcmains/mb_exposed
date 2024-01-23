using EmbedIO.WebSockets;
using MusicBeePlugin.Model;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin;

public class WebSocketNotifier : WebSocketModule
{
    private MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;

    public WebSocketNotifier(string urlPath) : base(urlPath, true)
    {
    }

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        PlayState playstate = mbApi.Player_GetPlayState();
        if (playstate == PlayState.Playing || playstate == PlayState.Paused)
        {
            // when a client connects and an albums is already playing, dispatch a message
            // so the client can act accordingly (update now playing track etc)
            UpdateMessage(NotificationType.PlayingTracksChanged);
        }

        return Task.CompletedTask;
    }

    protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
    {
        return Task.CompletedTask;
    }

    public void UpdateMessage(NotificationType type)
    {
        string playState = mbApi.Player_GetPlayState().ToString();
        int position = mbApi.Player_GetPosition();
        int duration = mbApi.NowPlaying_GetDuration();
        string url = mbApi.NowPlaying_GetFileUrl();

        MetaDataType[] fields = [MetaDataType.TrackTitle, MetaDataType.Album, MetaDataType.AlbumArtist, MetaDataType.DiscNo, MetaDataType.TrackNo];
        string[] tags;
        mbApi.NowPlaying_GetFileTags(fields, out tags);
        string length = mbApi.Library_GetFileProperty(url, FilePropertyType.Duration);

        // Track track = new Track { Title = tags[0], Album = tags[1], Artist = tags[2],  };

        Message message = new Message { Notification = type.ToString(), PlayState = playState, Position = position, Track = null };

        if (tags != null && Array.Exists(tags, element => element != null))
        {
            Track track = new Track
            {
                Title = tags[0],
                Album = tags[1],
                Artist = tags[2],
                Duration = duration,
                Length = length,
                Disk = tags[3] == "" ? null : Convert.ToInt16(tags[3]),
                Number = Convert.ToInt16(tags[4]),
                Uri = url
            };

            message.Track = track;
        }

        BroadcastAsync(JsonConvert.SerializeObject(message));
    }
}