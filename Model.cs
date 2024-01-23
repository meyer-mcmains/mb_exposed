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
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Year { get; set; }

        public Track[] Tracks { get; set; }
    }

    public partial class Track
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Album { get; set; }

        public string Artist { get; set; }

        public long? Disk { get; set; }

        public string Length { get; set; }

        public long Duration { get; set; }

        public string Title { get; set; }

        public long Number { get; set; }

        public string Uri { get; set; }
    }


    public class Message
    {
        public string Notification { get; set; }

        public int Position { get; set; }

        public string PlayState { get; set; }

        public Track Track { get; set; }

        public float[] SoundGraph { get; set; }
    }
}