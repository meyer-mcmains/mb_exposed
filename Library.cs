using System.Runtime.Serialization;

namespace MusicBeePlugin
{
    public partial class Band
    {
        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "albums")]
        public Album[] Albums { get; set; }
    }

    public partial class Album
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "year")]
        public string Year { get; set; }

        [DataMember(Name = "tracks")]
        public Track[] Tracks { get; set; }
    }

    public partial class Track
    {
        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "disk")]
        public long? Disk { get; set; }

        [DataMember(Name = "length")]
        public string Length { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "number")]
        public long Number { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }
    }
}