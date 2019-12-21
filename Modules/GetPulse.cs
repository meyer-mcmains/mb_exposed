using Nancy;

namespace MusicBeePlugin
{
    public class GetPulse : NancyModule
    {
        public GetPulse()
        {
            Get["/pulse"] = _ => true;
        }
    }
}