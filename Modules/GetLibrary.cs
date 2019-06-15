using Nancy;
using System.Collections.Generic;
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

            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist };

            List<string> artists = new List<string>();

            writer.WriteBeginObject();
            foreach (var item in library)
            {
                mbApi.Library_GetFileTags(item, meta, ref tags);
                if (artists.Find(c => c == tags[5]) == null)
                {
                    writer.WritePropertyName(tags[5]);
                    GetAlbum(tags[5], ref writer);
                    writer.WriteEndObject();
                    writer.WriteValueSeparator();
                    artists.Add(tags[5]);
                }
            }
            writer.WriteEndObject();

            return writer.ToString();
        }

        private void GetAlbum(string artist, ref JsonWriter writer)
        {
            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist, MetaDataType.Artist };
            string[] tracks = null;
            string[] album = null;
            string[] albumTracks = null;
            string[] trackInfo = null;
            string currentAlbum = null;
            MetaDataType[] albumMeta = new MetaDataType[] { MetaDataType.Album, MetaDataType.Year };

            mbApi.Library_QueryFilesEx("Artist=" + artist, ref tracks);
            writer.WriteBeginObject();
            foreach (var track in tracks)
            {
                mbApi.Library_GetFileTags(track, albumMeta, ref album);
                if (album[0] != currentAlbum)
                {
                    currentAlbum = album[0];
                    mbApi.Library_QueryFilesEx("Album=" + album[0], ref albumTracks);

                    writer.WritePropertyName(album[0]);
                    writer.WriteBeginObject();
                    writer.WritePropertyName("Year");
                    writer.WriteString(album[1]);
                    writer.WriteValueSeparator();
                    writer.WritePropertyName("tracks");
                    writer.WriteBeginArray();
                    foreach (var albumTrack in albumTracks)
                    {
                        mbApi.Library_GetFileTags(albumTrack, meta, ref trackInfo);
                        string length = mbApi.Library_GetFileProperty(albumTrack, FilePropertyType.Duration);
                        if (trackInfo[5] == artist)
                        {
                            BuildTrackJson(trackInfo, length, ref writer);
                        }
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteValueSeparator();
                }
            }
        }

        private void BuildTrackJson(string[] track, string length, ref JsonWriter writer)
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
            writer.WriteValueSeparator();
        }
    }
}