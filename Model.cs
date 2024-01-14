using System;

namespace MusicBeePlugin.Model
{
    public partial class Band
    {
        public string Artist { get; set; }

        public Album[] Albums { get; set; }
    }

    public partial class Album
    {
        public string AlbumId { get; set; } = Guid.NewGuid().ToString();

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Year { get; set; }

        public Track[] Tracks { get; set; }
    }

    public partial class Track
    {
        public string TrackId { get; set; } = Guid.NewGuid().ToString();

        public string Artist { get; set; }

        public long? Disk { get; set; }

        public string Length { get; set; }

        public string Name { get; set; }

        public long Number { get; set; }

        public string Path { get; set; }
    }


    public class Message
    {
        public string Notification { get; set; }

        public string SourceFile { get; set; }

        public int Position { get; set; }

        public int Duration { get; set; }

        public string PlayState { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string Track { get; set; }

        public float[] SoundGraph { get; set; }
    }
}