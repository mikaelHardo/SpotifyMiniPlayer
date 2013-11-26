using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;
using SpotifyRemote.Model;
using LocalApiClient = Slyngelstat.Spotify.LocalApi.LocalApiClient;

namespace SpotifyRemote
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _dispatcherTimer;

        private string _title;
        private bool _isDragging;
        private DateTime _songStart;
        private readonly WebClient _webClient;
        private readonly System.Windows.Forms.NotifyIcon _myNotifyIcon;

        private LocalApiClient spotifyClient;
        
        private const int WM_APPCOMMAND = 0x319;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 0xE0000;
        private const int APPCOMMAND_NEXT = 720896;
        private const int APPCOMMAND_PREV = 786432;

        public Process Spotify
        {
            get
            {
                return Process.GetProcessesByName("spotify").FirstOrDefault();
            }
        }

        public double CurrentSeconds
        {
            get
            {
                return (DateTime.Now - _songStart).TotalSeconds;
            }
            set
            {
                _songStart = DateTime.Now.AddSeconds(-value);
            }
        }

        public int SongLength { get; set; }

        public double Volume { get; set; }

        public IntPtr Handle
        {
            get { return new WindowInteropHelper(this).Handle; }
        }

        public MainWindow()
        {
            MouseDown += delegate { DragMove(); };
            InitializeComponent();

            _webClient = new WebClient();

            // Set Origin and Referer to get around spotify's attempts to protect the service
            _webClient.Headers.Add("Origin", "https://embed.spotify.com");
            _webClient.Headers.Add("Referer", "https://embed.spotify.com/?uri=spotify:track:1R2SZUOGJqqBiLuvwKOT2Y");

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            _dispatcherTimer.Start();

            var iconHandle = Properties.Resources.Grey_Spotify;

            _myNotifyIcon = new System.Windows.Forms.NotifyIcon();

            _myNotifyIcon.MouseDoubleClick += MyNotifyIcon_MouseDoubleClick;
            _myNotifyIcon.Icon = iconHandle;
            //ShowInTaskbar = true;
            _myNotifyIcon.Visible = true;
        }

        void MyNotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            mainWindow.Activate();
        }

        [DllImport("user32")]
        static extern bool keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (spotifyClient == null || string.IsNullOrEmpty(spotifyClient.OAuthToken))
            {
                spotifyClient = new LocalApiClient();
           
            }

            var currentTrack = spotifyClient.CurrentStatus;

            if (currentTrack == null || currentTrack.Track == null)
            {
                spotifyClient = null;
                return;
            }

            CurrentSeconds = currentTrack.PlayingPosition;
            SongLength = currentTrack.Track.Length;
            Volume = currentTrack.Volume;

            var title = currentTrack.Track.TrackResource.Name;

            var theUrl = currentTrack.Playing ? new Uri("Pause.png", UriKind.RelativeOrAbsolute) : new Uri("Play.png", UriKind.RelativeOrAbsolute);

            var image = new BitmapImage(theUrl);
            playPause.Source = image;

            if (!_isDragging)
            {
                volume.Value = Volume * 10;
            }

            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            if (SongLength != 0)
            {
                var percentage = CurrentSeconds / SongLength * 100;
                progressBar.Value = percentage;
            }

            if (title != _title)
            {
                _title = title;
                Spinner.Visibility = Visibility.Visible;

                await UpdateInfo(currentTrack.Track.TrackResource.Name, currentTrack.Track.ArtistResource.Name, currentTrack.Track.TrackResource.Uri);

                await Task.Delay(TimeSpan.FromSeconds(1.5));
                Spinner.Visibility = Visibility.Hidden;
            }
        }

        private async Task UpdateInfo(string title, string artist, string spotifyUri)
        {
            NowPlayingArtist.Text = artist;
            NowPlayingTitle.Text = title;

            await ShowAlbumArt(spotifyUri);

            _myNotifyIcon.BalloonTipTitle = @"Now playing:";
            _myNotifyIcon.BalloonTipText = title;
            _myNotifyIcon.ShowBalloonTip(400);
        }

        private void PlayPauseClick(object sender, RoutedEventArgs e)
        {
            SendMessageW(Handle, WM_APPCOMMAND, Handle, (IntPtr)APPCOMMAND_MEDIA_PLAY_PAUSE);
        }

        private void NextClick(object sender, RoutedEventArgs e)
        {
            SendMessageW(Handle, WM_APPCOMMAND, Handle, (IntPtr)APPCOMMAND_NEXT);
        }

        private void PrevClick(object sender, RoutedEventArgs e)
        {
            SendMessageW(Handle, WM_APPCOMMAND, Handle, (IntPtr)APPCOMMAND_PREV);
        }

        private void VolUp()
        {
            var handle = Spotify.MainWindowHandle;

            const int keyDown = 0x100;
            const int keyUp = 2;

            const byte uparrow = 0x26;
            const byte vkCtrl = 0xA2;


            keybd_event(vkCtrl, 0x8f, keyDown, 0); // CTRL Press
            PostMessage(handle, 256, (IntPtr)uparrow, IntPtr.Zero);

            Thread.Sleep(50);
            keybd_event(vkCtrl, 0x8f, keyUp, 0); // CTRL Release
        } 

        private void VolDown()
        {
            var handle = Spotify.MainWindowHandle;

            const int keyDown = 0x100;
            const int keyUp = 2;

            const byte downArrow = 0x28;
            const byte vkCtrl = 0xA2;


            keybd_event(vkCtrl, 0x8f, keyDown, 0); // CTRL Press
            PostMessage(handle, 256, (IntPtr)downArrow, IntPtr.Zero);

            Thread.Sleep(50);
            keybd_event(vkCtrl, 0x8f, keyUp, 0); // CTRL Release

        }

        private async Task ShowAlbumArt(string trackId)
        {
            try
            {
                trackId = trackId.Substring(14, 22);

                var trackUrl = "http://open.spotify.com/track/" + trackId;

                string htmlString = string.Empty;

                // Get HTML data 
                var client1 = new WebClient();
                var data1 = client1.OpenRead(trackUrl);
                var reader1 = new StreamReader(data1);
                htmlString = await reader1.ReadToEndAsync();
                data1.Close();
                var imageLink = htmlString.Substring(htmlString.IndexOf("http://o.scdn.co", StringComparison.Ordinal), 63).Replace("image", "300");

                gridBackground.ImageSource = coverArt.Source;

                var image = new BitmapImage(new Uri(imageLink, UriKind.Absolute));
                coverArt.Source = image;

                client1.Dispose();
            }
            catch (Exception)
            {
                gridBackground.ImageSource = null;
                coverArt.Source = null;
                Spinner.Visibility = Visibility.Hidden;
            }
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleSoundBar(object sender, RoutedEventArgs e)
        {
            soundBar.Visibility = soundBar.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
        }

        private void PinClicked(object sender, RoutedEventArgs e)
        {
            if (mainWindow.Topmost)
            {
                var image = new BitmapImage(new Uri("unpin.png", UriKind.Relative));
                pin.Source = image;
                mainWindow.Topmost = false;
            }
            else
            {
                var image = new BitmapImage(new Uri("pin.png", UriKind.Relative));
                pin.Source = image;
                mainWindow.Topmost = true;
            }
        }

        private void volume_DragCompleted_1(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var status = spotifyClient.CurrentStatus;
            if (status == null)
            {
                return;
            }

            var desiredVolume = Math.Round(volume.Value, 0);
            var currentVolume = Math.Round(status.Volume * 10, 0);

            var i = 0;

            while (desiredVolume != currentVolume && i < 20)
            {
                if (desiredVolume + 1 > currentVolume)
                {
                    VolUp();
                }
                else if (desiredVolume - 1 < currentVolume)
                {
                    VolDown();
                }

                Thread.Sleep(50);

                desiredVolume = Math.Round(volume.Value, 0);
                status = spotifyClient.CurrentStatus;
                currentVolume = Math.Round(status.Volume * 10, 0);
                i++;
            }

            _isDragging = false;
        }

        private void volume_DragDelta_1(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            _isDragging = true;
        }

        private void MouseLeaveSoundBar(object sender, MouseEventArgs e)
        {
            soundBar.Visibility = Visibility.Hidden;
        }
         
        private void mainWindow_MouseEnter_1(object sender, MouseEventArgs e)
        {
            var stbMove = (Storyboard)FindResource("ShowControlBar");
            var volumeMove = (Storyboard)FindResource("ShowVolume");
            stbMove.Begin();
            volumeMove.Begin();
        }

        private void mainWindow_MouseLeave_1(object sender, MouseEventArgs e)
        {
            var stbMove = (Storyboard)FindResource("HideControlBar");
            var volumeMove = (Storyboard)FindResource("HideVolume");
            stbMove.Begin();
            volumeMove.Begin();
        }
    }
}
