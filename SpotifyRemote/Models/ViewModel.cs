using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SpotifyRemote.Annotations;

namespace SpotifyRemote.Models
{
    public class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ViewModel : BaseModel
    {
        private int _height;
        private int _width;
        private NowPlaying _nowPlaying;
        private string _backgroundColor;
        private CoverArt _coverArt;
        private Progress _progressBar;

        public int Height
        {
            get { return _height; }
            set
            {
                if (value == _height) return;
                _height = value;
                OnPropertyChanged();
            }
        }

        public int Width
        {
            get { return _width; }
            set
            {
                if (value == _width) return;
                _width = value;
                OnPropertyChanged();
            }
        }

        public string BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (value == _backgroundColor) return;
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        public NowPlaying NowPlaying
        {
            get { return _nowPlaying; }
            set
            {
                if (Equals(value, _nowPlaying)) return;
                _nowPlaying = value;
                OnPropertyChanged();
            }
        }

        public CoverArt CoverArt
        {
            get { return _coverArt; }
            set
            {
                if (Equals(value, _coverArt)) return;
                _coverArt = value;
                OnPropertyChanged();
            }
        }

        public Progress Progress
        {
            get { return _progressBar; }
            set
            {
                if (Equals(value, _progressBar)) return;
                _progressBar = value;
                OnPropertyChanged();
            }
        }

        public ViewButton PreviousButton { get; set; }

        public ToggleButton PlayButton { get; set; }

        public ViewButton NextButton { get; set; }

        public ToggleButton PinButton { get; set; }

        public ViewButton CloseButton { get; set; }
    }

    public class CoverArt : BaseModel
    {
        private int _top;
        private int _left;
        private int _with;
        private int _height;
        private Thickness _margin;

        public int Top
        {
            get { return _top; }
            set
            {
                if (value == _top) return;
                _top = value;
                OnPropertyChanged();
            }
        }

        public int Left
        {
            get { return _left; }
            set
            {
                if (value == _left) return;
                _left = value;
                OnPropertyChanged();
            }
        }

        public int Width
        {
            get { return _with; }
            set
            {
                if (value == _with) return;
                _with = value;
                OnPropertyChanged();
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                if (value == _height) return;
                _height = value;
                OnPropertyChanged();
            }
        }

        public Thickness Margin
        {
            get { return _margin; }
            set
            {
                if (value.Equals(_margin)) return;
                _margin = value;
                OnPropertyChanged();
            }
        }
    }

    public class Progress : BaseModel
    {
        private Thickness _timeMargin;
        private Thickness _progressMargin;
        private int _progressHeight;
        private int _progressWidth;

        public Thickness TimeMargin
        {
            get { return _timeMargin; }
            set
            {
                if (value.Equals(_timeMargin)) return;
                _timeMargin = value;
                OnPropertyChanged();
            }
        }

        public Thickness ProgressMargin
        {
            get { return _progressMargin; }
            set
            {
                if (value.Equals(_progressMargin)) return;
                _progressMargin = value;
                OnPropertyChanged();
            }
        }

        public int ProgressHeight
        {
            get { return _progressHeight; }
            set
            {
                if (value == _progressHeight) return;
                _progressHeight = value;
                OnPropertyChanged();
            }
        }

        public int ProgressWidth
        {
            get { return _progressWidth; }
            set
            {
                if (value == _progressWidth) return;
                _progressWidth = value;
                OnPropertyChanged();
            }
        }
    }

    public class ViewButton : BaseModel
    {
        private int _width;
        private int _height;
        private string _imageName;
        private Thickness _margin;

        public int Width
        {
            get { return _width; }
            set
            {
                if (value == _width) return;
                _width = value;
                OnPropertyChanged();
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                if (value == _height) return;
                _height = value;
                OnPropertyChanged();
            }
        }

        public string ImageName
        {
            get { return _imageName; }
            set
            {
                if (value == _imageName) return;
                _imageName = value;
                OnPropertyChanged();
            }
        }

        public Thickness Margin
        {
            get { return _margin; }
            set
            {
                if (value.Equals(_margin)) return;
                _margin = value;
                OnPropertyChanged();
            }
        }
    }

    public class ToggleButton : ViewButton
    {
        private string _imageNameSecondary;

        public string ImageNameSecondary
        {
            get { return _imageNameSecondary; }
            set
            {
                if (value == _imageNameSecondary) return;
                _imageNameSecondary = value;
                OnPropertyChanged();
            }
        }
    }

    public class NowPlaying : BaseModel
    {
        private Thickness _margin;
        private int _width;

        public Thickness Margin
        {
            get { return _margin; }
            set
            {
                if (value.Equals(_margin)) return;
                _margin = value;
                OnPropertyChanged();
            }
        }

        public int Width
        {
            get { return _width; }
            set
            {
                if (value == _width) return;
                _width = value;
                OnPropertyChanged();
            }
        }
    }

}
