namespace MusicBeePlugin
{
    public class MbApiInstance
    {
        private Plugin.MusicBeeApiInterface temp;
        private static readonly object _syncLock = new object();
        private static MbApiInstance _instance;

        public static MbApiInstance Instance
        {
            get
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                        _instance = new MbApiInstance();
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

        public Plugin.MusicBeeApiInterface MusicBeeApiInterface { get; set; }
    }
}