using Nancy;

namespace MusicBeePlugin
{
    public class PostPlayPause
    {
        private Plugin.MusicBeeApiInterface temp;
        private static readonly object _syncLock = new object();
        private static PostPlayPause _instance;

        public static PostPlayPause Instance
        {
            get
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                        _instance = new PostPlayPause();
                    return _instance;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _instance.temp = value.temp;
                }
            }
        }

        public Plugin.MusicBeeApiInterface Value { get; set; }
    }

    public class PostPlayModule : NancyModule
    {
        public PostPlayModule()
        {
            Post["/play-pause"] = _ =>
            {
                Plugin.MusicBeeApiInterface mbApi = PostPlayPause.Instance.Value;
                mbApi.Player_PlayPause();
                return mbApi.Player_GetPlayState().ToString();
            };
        }
    }
}