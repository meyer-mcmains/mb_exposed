using Nancy;

namespace MusicBeePlugin
{
    public class GetNowPlaying
    {
        private string temp;
        private static readonly object _syncLock = new object();
        private static GetNowPlaying _instance;

        public static GetNowPlaying Instance
        {
            get
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                        _instance = new GetNowPlaying();
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

        public string Value { get; set; }
    }

    public class GetNowPlayingModule : NancyModule
    {
        public GetNowPlayingModule()
        {
            Get["/now-playing"] = _ => GetNowPlaying.Instance.Value;
        }
    }
}