using Nancy;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public class GetNowPlaying : NancyModule
    {
        public GetNowPlaying()
        {
            Get["/now-playing"] = _ =>
            {
                MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;
                return mbApi.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
            };
        }
    }
}