using Nancy;

namespace MusicBeePlugin
{
    public class GetLibrary
    {
        private string temp;
        private static readonly object _syncLock = new object();
        private static GetLibrary _instance;

        public static GetLibrary Instance
        {
            get
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                        _instance = new GetLibrary();
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

    public class GetLibraryModule : NancyModule
    {
        public GetLibraryModule()
        {
            Get["/library"] = _ => GetLibrary.Instance.Value;
        }
    }
}