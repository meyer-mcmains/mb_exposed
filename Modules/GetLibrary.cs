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
            string[] library = null;
            string[] tags = null;
            mbApi.Library_QueryFilesEx("domain=Library", ref library);
            Array.Sort(library, (x, y) => String.Compare(x, y));

            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist };

            List<string> artists = new List<string>();

            List<Band> bands = new List<Band>();

            foreach (var item in library.Select((value, index) => new { value, index }))
            {
                mbApi.Library_GetFileTags(item.value, meta, ref tags);
                if (artists.Find(c => c == tags[5]) == null)
                {
                    bands.Add(new Band { Artist = tags[5], Albums = GetAlbums(tags[5]) });
                    artists.Add(tags[5]);
                }
            }
            return JsonSerializer.ToJsonString(bands);
        }

        private Album[] GetAlbums(string artist)
        {
            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist, MetaDataType.Artist };
            MetaDataType[] albumMeta = new MetaDataType[] { MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist };
            List<Album> albums = new List<Album>();
            string[] albumInfo = null;
            string[] tracks = null;
            string[] trackInfo = null;

            // Get all Tracks that belong to an artist
            mbApi.Library_QueryFilesEx($"Artist={artist}", ref tracks);
            List<string> albumTitles = new List<string>();

            // Use list of tracks to find all albums that belong to an artist
            foreach (var track in tracks.Select((value, index) => new { value, index }))
            {
                mbApi.Library_GetFileTags(track.value, albumMeta, ref albumInfo);
                if (!albumTitles.Contains(albumInfo[0]))
                {
                    albumTitles.Add(albumInfo[0]);
                }
            }

            // Use list of albums to get tracks for each album
            foreach (var singleAlbum in albumTitles.Select((value, index) => new { value, index }))
            {
                mbApi.Library_QueryFilesEx($"Artist={artist}\0Album={singleAlbum.value}", ref tracks);
                mbApi.Library_GetFileTags(tracks[0], albumMeta, ref albumInfo);
                List<Track> albumTracks = new List<Track>();

                // Use the list of tracks to get information for each track
                foreach (var albumTrack in tracks.Select((value, index) => new { value, index }))
                {
                    mbApi.Library_GetFileTags(albumTrack.value, meta, ref trackInfo);
                    string length = mbApi.Library_GetFileProperty(albumTrack.value, FilePropertyType.Duration);
                    // not all tracks have a disk number
                    if (trackInfo[2] != "")
                    {
                        albumTracks.Add(new Track { Artist = trackInfo[6], Disk = Convert.ToInt16(trackInfo[2]), Length = length, Name = trackInfo[0], Number = Convert.ToInt16(trackInfo[1]), Path = albumTrack.value });
                    }
                    else
                    {
                        albumTracks.Add(new Track { Artist = trackInfo[6], Disk = null, Length = length, Name = trackInfo[0], Number = Convert.ToInt16(trackInfo[1]), Path = albumTrack.value });
                    }
                }
                // Sort to ensure correct order of disks and tracks
                albumTracks.Sort((x, y) => x.Disk == y.Disk ?
                x.Number.CompareTo(y.Number) :
                (x.Disk < y.Disk ? -1 : 1));
                albums.Add(new Album { Artist = albumInfo[2], Title = albumInfo[0], Tracks = albumTracks.ToArray(), Year = albumInfo[1] });
            }

            return albums.ToArray();
        }
    }
}