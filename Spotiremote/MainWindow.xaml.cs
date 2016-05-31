using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;
using Spotiremote.Annotations;
using Spotiremote.Models;
using Button = System.Windows.Forms.Button;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;


namespace Spotiremote
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _dispatcherTimer;

        private DateTime _songStart;
        private readonly NotifyIcon _myNotifyIcon;
        private SpotifyLocalAPI _spotify;
        private KeyboardHook _hook = new KeyboardHook();
        private SettingsModel _settings = new SettingsModel();
        private ViewModel _viewModel;
        private ThicknessAnimation _doubleAnimation;

        public ViewModel ViewModel
        {
            get { return _viewModel ?? new ViewModel(); }
            set
            {
                if (Equals(value, _viewModel)) return;
                _viewModel = value;
                OnPropertyChanged();
            }
        }

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
            MouseDown += (sender, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    this.DragMove();
                }

                if (mainWindow.Top < 10)
                {
                    mainWindow.Top = 0;
                }

            };

            Loaded += Window_Loaded;

            InitializeComponent();

            StartMarquee();

            UpdateFromSettings();

            DataContext = ViewModel;

            mainWindow.Topmost = true;

            // register the event that is fired after the key press.
            _hook.KeyPressed += hook_KeyPressed;
            // register the control + alt + F12 combination as hot key.
            _hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift, Keys.Right);
            _hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift, Keys.Left);
            _hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift, Keys.Space);


            _dispatcherTimer = new DispatcherTimer();
            //_dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            _dispatcherTimer.Start();

            var iconHandle = Properties.Resources.Grey_Spotify;

            _myNotifyIcon = new System.Windows.Forms.NotifyIcon();

            _myNotifyIcon.MouseDoubleClick += MyNotifyIcon_MouseDoubleClick;
            _myNotifyIcon.Icon = iconHandle;
            //ShowInTaskbar = true;
            _myNotifyIcon.Visible = true;
        }

        private async void Window_Loaded(object s, RoutedEventArgs e)
        {
            _spotify = new SpotifyLocalAPI();

            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                SpotifyLocalAPI.RunSpotify();
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                SpotifyLocalAPI.RunSpotifyWebHelper();
            }

            _spotify.OnPlayStateChange += (sender, args) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    UpdatePlayState(args.Playing);
                });
            };

            _spotify.OnTrackChange += OnTrackChanged;
            _spotify.OnTrackTimeChange += (sender, args) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    UpdateTime(args.TrackTime);
                });
            };
            
            //           _spotify.OnVolumeChange += _spotify_OnVolumeChange;
            _spotify.SynchronizingObject = new Button();

            StatusResponse status = null;

            for (int i = 0; i < 10; i++)
            {
                _spotify.Connect();
                _spotify.ListenForEvents = true;

                status = _spotify.GetStatus();

                if (status == null)
                {
                    await Task.Delay(1000);
                    continue;
                }

                break;
            }

            if (status == null)
            {
                return;
            }

            UpdateInfo(status.Track, status.Playing);
            UpdatePlayState(status.Playing);

        }

        private void StartMarquee()
        {
            StartMarquee(null, null);
        }

        private async void StartMarquee(object sender, EventArgs e)
        {
            await Task.Delay(5000);

            var margin = NowPlaying.Margin;
            margin.Left = 0;

            _doubleAnimation = new ThicknessAnimation();

            _doubleAnimation.From = margin;

            // we want to scroll the differents between the length of the text - the width of the container (and 5 added for margin)
            margin.Left = -NowPlaying.ActualWidth + NowPlayingContainer.Width;

            _doubleAnimation.To = margin;
            _doubleAnimation.AutoReverse = true;
            //_doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            _doubleAnimation.Duration = new Duration(TimeSpan.Parse("0:0:3"));
            _doubleAnimation.Completed += StartMarquee;

            NowPlaying.BeginAnimation(Canvas.MarginProperty, _doubleAnimation);
        }

        private void UpdatePlayState(bool playing)
        {
            var theUrl = playing ? new Uri(ViewModel.PlayButton.ImageNameSecondary, UriKind.RelativeOrAbsolute) : new Uri(ViewModel.PlayButton.ImageName, UriKind.RelativeOrAbsolute);

            var image = new BitmapImage(theUrl);
            playPause.Source = image;
        }

        private void UpdateFromSettings()
        {
            _settings.SelectedTheme = Properties.Settings.Default.SelectedTheme;
            var theme = _settings.SelectedTheme;
            var xml = $"Themes/{theme}/theme.xml";

            var serialiser = new XmlSerializer(typeof(ViewModel));
            ViewModel = serialiser.Deserialize(File.OpenRead(xml)) as ViewModel;
            DataContext = ViewModel;
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (e.Key == Keys.Left)
            {
                PrevClick(null, null);
            }
            else if (e.Key == Keys.Right)
            {
                NextClick(null, null);
            }
            else
            {
                PlayPauseClick(null, null);
            }
        }

        private void UpdateTime(double trackTime)
        {
            var fullMinutes = trackTime / 60;

            var minutes = Math.Floor(fullMinutes);

            var seconds = Math.Floor((fullMinutes - minutes) * 60);

            CurrentTime.Text = minutes + ":" + seconds.ToString("00");

            var length = _spotify.GetStatus().Track.Length;

            var percentage = trackTime / length * 100;
            progressBar.Value = percentage;
        }

        private void OnTrackChanged(object sender, TrackChangeEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                UpdateInfo(e.NewTrack, _spotify.GetStatus().Playing);
            });
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

        private void UpdateInfo(Track track, bool isPlaying)
        {
            if (track == null)
            {
                return;
            }

            NowPlaying.Text = track.ArtistResource.Name + " - " + track.TrackResource.Name;

            AlbumArt.Source = new BitmapImage(new Uri(track.GetAlbumArtUrl(AlbumArtSize.Size160)));

            //            _myNotifyIcon.BalloonTipTitle = @"Now playing:";
            //           _myNotifyIcon.BalloonTipText = track.TrackResource.Name;
            //          _myNotifyIcon.ShowBalloonTip(400);
        }

        private void PlayPauseClick(object sender, RoutedEventArgs e)
        {
            if (_spotify.GetStatus().Playing)
            {
                _spotify.Pause();
            }
            else
            {
                _spotify.Play();
            }
        }

        private void NextClick(object sender, RoutedEventArgs e)
        {
            _spotify.Skip();
        }

        private void PrevClick(object sender, RoutedEventArgs e)
        {
            _spotify.Previous();
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

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PinClicked(object sender, RoutedEventArgs e)
        {
            if (mainWindow.Topmost)
            {
                var image = new BitmapImage(new Uri(ViewModel.PinButton.ImageNameSecondary, UriKind.Relative));
                pin.Source = image;
                mainWindow.Topmost = false;
            }
            else
            {
                var image = new BitmapImage(new Uri(ViewModel.PinButton.ImageName, UriKind.Relative));
                pin.Source = image;
                mainWindow.Topmost = true;
            }
        }

        private void mainWindow_MouseEnter_1(object sender, MouseEventArgs e)
        {
            //var stbMove = (Storyboard)FindResource("ShowControlBar");
            //var volumeMove = (Storyboard)FindResource("ShowVolume");
            //stbMove.Begin();
            //volumeMove.Begin();
        }

        private void mainWindow_MouseLeave_1(object sender, MouseEventArgs e)
        {
            //var stbMove = (Storyboard)FindResource("HideControlBar");
            //var volumeMove = (Storyboard)FindResource("HideVolume");
            //stbMove.Begin();
            //volumeMove.Begin();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow();
            settings.Show();
            settings.Closed += (o, args) =>
            {
                UpdateFromSettings();
            };

            UpdateFromSettings();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
