using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using MusicBeePlugin.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
            string[] tracks;
            string[] trackInfo;
            MetaDataType[] meta = new MetaDataType[] { MetaDataType.DiscNo, MetaDataType.TrackNo };
            mbApi.Library_QueryFilesEx($"AlbumArtist={artist}\0Album={album}", out tracks);

            var albumTracks = new List<(string SourceFile, int DiskNo, int TrackNo)>();

            // Use the list of tracks to get information for each track
            foreach (var albumTrack in tracks)
            {
                mbApi.Library_GetFileTags(albumTrack, meta, out trackInfo);
                // not all tracks have a disk number
                if (trackInfo[0] == "")
                {
                    albumTracks.Add((albumTrack, 0, Convert.ToInt16(trackInfo[1])));
                }
                else
                {
                    albumTracks.Add((albumTrack, Convert.ToInt16(trackInfo[0]), Convert.ToInt16(trackInfo[1])));
                }
            }

            // Sort to ensure correct oarder of disks and tracks
            albumTracks.Sort((x, y) => x.DiskNo == y.DiskNo
                ? x.TrackNo.CompareTo(y.TrackNo)
                : (x.DiskNo < y.DiskNo ? -1 : 1));


            var trackToQueue = albumTracks.Select(track => track.SourceFile).ToArray();

            mbApi.NowPlayingList_Clear();
            mbApi.NowPlayingList_QueueFilesNext(trackToQueue);
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

        // Play the previous track
        // This will respond to
        // POST http://localhost:1200/api/previous-track
        [Route(HttpVerbs.Post, "/previous-track")]
        public void PreviousTrack()
        {
            mbApi.Player_PlayPreviousTrack();
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
                using var memoryStream = new MemoryStream(image);
                using var originalImage = new Bitmap(memoryStream);

                var resized = new Bitmap(400, 400);
                using var graphics = Graphics.FromImage(resized);

                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(originalImage, 0, 0, 400, 400);

                using var stream = new MemoryStream();
                resized.Save(stream, ImageFormat.Jpeg);

                return stream.ToArray();
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
            mbApi.Library_QueryFilesEx($"ALbumArtist={artist}", out tracks);
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
                mbApi.Library_QueryFilesEx($"AlbumArtist={artist}\0Album={singleAlbum.value}", out tracks);
                mbApi.Library_GetFileTags(tracks[0], albumMeta, out albumInfo);
                List<Track> albumTracks = new List<Track>();

                // Use the list of tracks to get information for each track
                foreach (var albumTrack in tracks.Select((value, index) => new { value, index }))
                {
                    mbApi.Library_GetFileTags(albumTrack.value, meta, out trackInfo);
                    string length = mbApi.Library_GetFileProperty(albumTrack.value, FilePropertyType.Duration);

                    Track track = new Track
                    {
                        Artist = trackInfo[6],
                        Disk = trackInfo[2] == "" ? null : Convert.ToInt16(trackInfo[2]),
                        Length = length,
                        Title = trackInfo[0],
                        Number = Convert.ToInt16(trackInfo[1]),
                        Uri = albumTrack.value
                    };

                    albumTracks.Add(track);
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