using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using MusicBeePlugin.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin.Controller
{
    public sealed class Playback : WebApiController
    {
        private Plugin.MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;

        // Pause or Play the now playing track
        // This will respond to
        // POST http://localhost:1200/api/play-pause
        [Route(HttpVerbs.Post, "/play-pause")]
        public string PostPlayPause()
        {
            mbApi.Player_PlayPause();
            return mbApi.Player_GetPlayState().ToString();
        }

        // Play an album
        // This will respond to
        // POST http://localhost:1200/api/play-album
        [Route(HttpVerbs.Post, "/play-album")]
        public void PostPlayAlbum([FormField] string artist, [FormField] string album)
        {
            string[] albumRef = null;
            mbApi.Library_QueryFilesEx($"Artist={artist}\0Album={album}", out albumRef);
            mbApi.NowPlayingList_Clear();
            mbApi.NowPlayingList_QueueFilesNext(albumRef);
            mbApi.Player_PlayNextTrack();
        }

        // Play the next track
        // This will respond to
        // POST http://localhost:1200/api/next-track
        [Route(HttpVerbs.Post, "/next-track")]
        public void NextTrack()
        {
            mbApi.Player_PlayNextTrack();
        }
    }

    public sealed class Artwork : WebApiController
    {
        // Retrieve album cover art
        // This will respond to
        // POST http://localhost:1200/api/artwork
        [Route(HttpVerbs.Get, "/artwork")]
        public byte[] GetArtwork([FormField] string artist, [FormField] string album, [FormField] bool thumbnail)
        {
            HttpContext.Response.ContentType = "image/jpeg";

            Plugin.MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;
            PictureLocations pictureLocations = PictureLocations.None;

            string[] albumRef;
            mbApi.Library_QueryFilesEx($"AlbumArtist={artist}\0Album={album}", out albumRef);

            string pictureUrl;
            byte[] image;
            mbApi.Library_GetArtworkEx(albumRef[0], 0, true, out pictureLocations, out pictureUrl, out image);

            if (thumbnail)
            {
                Bitmap bitmap;
                using (var memoryStream = new MemoryStream(image))
                {
                    bitmap = new Bitmap(memoryStream);
                    Image thumbnailImage = bitmap.GetThumbnailImage(400, 400, null, IntPtr.Zero);

                    ImageConverter converter = new ImageConverter();
                    return (byte[])converter.ConvertTo(thumbnailImage, typeof(byte[]));
                }
            }

            return image;
        }
    }

    public sealed class Library : WebApiController
    {
        private MusicBeeApiInterface mbApi = MbApiInstance.Instance.MusicBeeApiInterface;

        // Retrieve the music library as json
        // This will respond to
        // POST http://localhost:1200/api/library
        [Route(HttpVerbs.Get, "/library")]
        public string GetLibrary()
        {
            string[] library = null;
            string[] tags = null;
            mbApi.Library_QueryFilesEx("domain=Library", out library);
            Array.Sort(library, String.Compare);

            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist };

            List<string> artists = new List<string>();

            List<Band> bands = new List<Band>();

            foreach (var item in library.Select((value, index) => new
            {
                value,
                index
            }))
            {
                mbApi.Library_GetFileTags(item.value, meta, out tags);
                if (artists.Find(c => c == tags[5]) == null)
                {
                    bands.Add(new Band { Artist = tags[5], Albums = GetAlbums(tags[5]) });
                    artists.Add(tags[5]);
                }
            }
            return JsonConvert.SerializeObject(bands);
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
            mbApi.Library_QueryFilesEx($"Artist={artist}", out tracks);
            List<string> albumTitles = new List<string>();

            // Use list of tracks to find all albums that belong to an artist
            foreach (var track in tracks.Select((value, index) => new { value, index }))
            {
                mbApi.Library_GetFileTags(track.value, albumMeta, out albumInfo);
                if (!albumTitles.Contains(albumInfo[0]))
                {
                    albumTitles.Add(albumInfo[0]);
                }
            }

            // Use list of albums to get tracks for each album
            foreach (var singleAlbum in albumTitles.Select((value, index) => new { value, index }))
            {
                mbApi.Library_QueryFilesEx($"Artist={artist}\0Album={singleAlbum.value}", out tracks);
                mbApi.Library_GetFileTags(tracks[0], albumMeta, out albumInfo);
                List<Track> albumTracks = new List<Track>();

                // Use the list of tracks to get information for each track
                foreach (var albumTrack in tracks.Select((value, index) => new { value, index }))
                {
                    mbApi.Library_GetFileTags(albumTrack.value, meta, out trackInfo);
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