using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using Utf8Json;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public class GetLibrary : NancyModule
    {
        private MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;

        public GetLibrary()
        {
            Get["/library"] = _ => ExportLibrary();
        }

        private string ExportLibrary()
        {
            JsonWriter writer = new JsonWriter();
            string[] library = null;
            string[] tags = null;
            mbApi.Library_QueryFilesEx("domain=Library", ref library);
            Array.Sort(library, (x, y) => String.Compare(x, y));

            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist };

            List<string> artists = new List<string>();

            writer.WriteBeginObject();
            foreach (var item in library.Select((value, index) => new { value, index }))
            {
                mbApi.Library_GetFileTags(item.value, meta, ref tags);
                if (artists.Find(c => c == tags[5]) == null)
                {
                    writer.WritePropertyName(tags[5]);
                    int trackCount = GetAlbum(tags[5], ref writer);
                    writer.WriteEndObject();
                    if (item.index + trackCount < library.Length)
                        writer.WriteValueSeparator();
                    artists.Add(tags[5]);
                }
            }
            writer.WriteEndObject();

            return writer.ToString();
        }

        private int GetAlbum(string artist, ref JsonWriter writer)
        {
            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist, MetaDataType.Artist };
            MetaDataType[] albumMeta = new MetaDataType[] { MetaDataType.Album, MetaDataType.Year };
            List<string> albums = new List<string>();
            List<string> albumYears = new List<string>();
            string[] albumInfo = null;
            string[] albumTracks = null;
            string[] tracks = null;
            string[] trackInfo = null;

            mbApi.Library_QueryFilesEx("Artist=" + artist, ref tracks);
            writer.WriteBeginObject();
            int totalTracks = 0;

            foreach (var track in tracks.Select((value, index) => new { value, index }))
            {
                mbApi.Library_GetFileTags(track.value, albumMeta, ref albumInfo);
                if (!albums.Contains(albumInfo[0]))
                {
                    albums.Add(albumInfo[0]);
                    albumYears.Add(albumInfo[1]);
                }
            }

            foreach (var singleAlbum in albums.Select((value, index) => new { value, index }))
            {
                mbApi.Library_QueryFilesEx($"Artist={artist}\0Album={singleAlbum.value}", ref albumTracks);
                Array.Reverse(albumTracks);
                totalTracks += albumTracks.Length;

                writer.WritePropertyName(singleAlbum.value);
                writer.WriteBeginObject();
                writer.WritePropertyName("year");
                writer.WriteString(albumYears[singleAlbum.index]);
                writer.WriteValueSeparator();
                writer.WritePropertyName("tracks");
                writer.WriteBeginArray();

                System.Array.Reverse(albumTracks);
                foreach (var albumTrack in albumTracks.Select((value, index) => new { value, index }))
                {
                    mbApi.Library_GetFileTags(albumTrack.value, meta, ref trackInfo);
                    string length = mbApi.Library_GetFileProperty(albumTrack.value, FilePropertyType.Duration);
                    BuildTrackJson(trackInfo, length, albumTrack.index != albumTracks.Length - 1, ref writer);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                if (singleAlbum.index < albums.Count - 1)
                    writer.WriteValueSeparator();
            }

            return totalTracks;
        }

        private void BuildTrackJson(string[] track, string length, bool writeSeparator, ref JsonWriter writer)
        {
            writer.WriteBeginObject();
            writer.WritePropertyName("artist");
            writer.WriteString(track[6]);
            writer.WriteValueSeparator();
            writer.WritePropertyName("length");
            writer.WriteString(length);
            writer.WriteValueSeparator();
            writer.WritePropertyName("name");
            writer.WriteString(track[0]);
            writer.WriteValueSeparator();
            writer.WritePropertyName("number");
            writer.WriteString(track[1]);
            writer.WriteEndObject();
            if (writeSeparator)
                writer.WriteValueSeparator();
        }
    }
}