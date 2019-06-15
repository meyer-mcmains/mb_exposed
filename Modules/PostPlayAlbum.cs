using Nancy;

namespace MusicBeePlugin
{
    public class PostPlayAlbum : NancyModule
    {
        public PostPlayAlbum()
        {
            Post["/play-album"] = _ =>
            {
                Plugin.MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;
                string[] album = null;
                mbApi.Library_QueryFilesEx($"Artist={Request.Query.artist}\0Album={Request.Query.album}", ref album);
                mbApi.NowPlayingList_Clear();
                mbApi.NowPlayingList_QueueFilesNext(album);
                mbApi.Player_PlayNextTrack();
                return album;
            };
        }
    }
}