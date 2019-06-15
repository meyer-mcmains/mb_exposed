using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Utf8Json;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "BeExposed";
            about.Description = "Musicbee Server";
            about.Author = "Meyer McMains";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:

                    StartServer();
                    ExportLibrary();
                    PostPlayPause.Instance.Value = mbApiInterface;

                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                        case PlayState.Paused:
                            // ...
                            break;
                    }
                    break;

                case NotificationType.TrackChanged:
                    NowPlaying();
                    break;
            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        {
            return null;
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            //Return Convert.ToBase64String(artworkBinaryData)
            return null;
        }

        public void StartServer()
        {
            Nancy.Json.JsonSettings.MaxJsonLength = int.MaxValue;
            // perform startup initialisation
            HostConfiguration hostConfigs = new HostConfiguration();
            hostConfigs.UrlReservations.CreateAutomatically = true;

            Uri uri = new Uri("http://localhost:1234");
            var host = new NancyHost(hostConfigs, uri);
            host.Start();

            MessageBox.Show("Running on http://localhost:1234");
        }

        public void ExportLibrary()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            JsonWriter writer = new JsonWriter();
            string[] library = null;
            string[] tags = null;
            mbApiInterface.Library_QueryFilesEx("domain=Library", ref library);

            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist };

            List<string> artists = new List<string>();

            writer.WriteBeginObject();
            foreach (var item in library)
            {
                mbApiInterface.Library_GetFileTags(item, meta, ref tags);
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

            GetLibrary.Instance.Value = writer.ToString();
            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            MessageBox.Show("RunTime " + elapsedTime);
        }

        public void GetAlbum(string artist, ref JsonWriter writer)
        {
            MetaDataType[] meta = new MetaDataType[] { MetaDataType.TrackTitle, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.Album, MetaDataType.Year, MetaDataType.AlbumArtist, MetaDataType.Artist };
            string[] tracks = null;
            string[] album = null;
            string[] albumTracks = null;
            string[] trackInfo = null;
            string currentAlbum = null;
            MetaDataType[] albumMeta = new MetaDataType[] { MetaDataType.Album, MetaDataType.Year };

            mbApiInterface.Library_QueryFilesEx("Artist=" + artist, ref tracks);
            writer.WriteBeginObject();
            foreach (var track in tracks)
            {
                mbApiInterface.Library_GetFileTags(track, albumMeta, ref album);
                if (album[0] != currentAlbum)
                {
                    currentAlbum = album[0];
                    mbApiInterface.Library_QueryFilesEx("Album=" + album[0], ref albumTracks);

                    writer.WritePropertyName(album[0]);
                    writer.WriteBeginObject();
                    writer.WritePropertyName("Year");
                    writer.WriteString(album[1]);
                    writer.WriteValueSeparator();
                    writer.WritePropertyName("tracks");
                    writer.WriteBeginArray();
                    foreach (var albumTrack in albumTracks)
                    {
                        mbApiInterface.Library_GetFileTags(albumTrack, meta, ref trackInfo);
                        string length = mbApiInterface.Library_GetFileProperty(albumTrack, FilePropertyType.Duration);
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

        public void BuildTrackJson(string[] track, string length, ref JsonWriter writer)
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

        public void NowPlaying() => GetNowPlaying.Instance.Value = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);

        public void PlayPause()
        {
            MessageBox.Show(mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Album));
        }
    }
}