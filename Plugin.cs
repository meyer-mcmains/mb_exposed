using EmbedIO;
using EmbedIO.WebApi;
using MusicBeePlugin.Controller;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private WebSocketNotifier webSocketNotifier;

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "BeExposed";
            about.Description = "An embeded API for musicbee.";
            about.Author = "Meyer McMains";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
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
                    MbApiInstance.Instance.MusicBeeApiInterface = mbApiInterface;

                    // perform startup initialisation
                    using (var ctSource = new CancellationTokenSource())
                    {
                        RunWebServerAsync(ctSource.Token);
                    }

                    webSocketNotifier.UpdateMessage(sourceFileUrl, type);
                    break;

                case NotificationType.TrackChanged:
                case NotificationType.PlayStateChanged:
                    webSocketNotifier.UpdateMessage(sourceFileUrl, type);
                    break;
            }
        }


        private async Task RunWebServerAsync(CancellationToken cancellationToken)
        {
            var url = "http://*:1200";
            using var server = CreateWebServer(url);
            await server.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        private WebServer CreateWebServer(string url)
        {
            webSocketNotifier = new WebSocketNotifier("/notification");

            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                // https://github.com/unosquare/embedio/pull/501
                .WithWebApi("/api", ResponseSerializer.None(false), m => m
                    .WithController<Playback>()
                    .WithController<Artwork>()
                    .WithController<Library>()
                 )
                .WithModule(webSocketNotifier);

            return server;
        }
    }
}