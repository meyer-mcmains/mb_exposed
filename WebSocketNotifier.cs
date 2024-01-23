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

    public void UpdateMessage(string sourceFile, NotificationType type)
    {
        string playState = mbApi.Player_GetPlayState().ToString();
        int position = mbApi.Player_GetPosition();
        int duration = mbApi.NowPlaying_GetDuration();

        MetaDataType[] fields = [MetaDataType.TrackTitle, MetaDataType.Album, MetaDataType.AlbumArtist];
        string[] tags;
        mbApi.NowPlaying_GetFileTags(fields, out tags);

        Message message = new Message { Notification = type.ToString(), SourceFile = sourceFile, PlayState = playState, Position = position, Duration = duration, Track = "", Album = "", Artist = "" };

        if (tags != null && Array.Exists(tags, element => element != null))
        {
            message.Track = tags[0];
            message.Album = tags[1];
            message.Artist = tags[2];
        }

        //lastMessage = new Message { Notification = type.ToString(), SourceFile = sourceFile, PlayState = playState, Position = position, Duration = duration, SoundGraph = retrieveGraph ? currentSoundGraph : new float[0], Track = tags[0], Album = tags[1], Artist = tags[2] };
        BroadcastAsync(JsonConvert.SerializeObject(message));
    }
}