using Nancy;

namespace MusicBeePlugin
{
    public class PostPlayPause : NancyModule
    {
        public PostPlayPause()
        {
            Post["/play-pause"] = _ =>
            {
                Plugin.MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;
                mbApi.Player_PlayPause();
                return mbApi.Player_GetPlayState().ToString();
            };
        }
    }
}