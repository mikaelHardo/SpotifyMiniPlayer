using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpotifyRemote
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public ObservableCollection<string> Themes { get; set; }

        public string SelectedTheme { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();

            DataContext = this;

            Themes = new ObservableCollection<string>();

            var themes = Directory.GetDirectories("Themes").Select(s => s.Replace("Themes\\", ""));

            foreach (var theme in themes)
            {
                Themes.Add(theme);
            }

            SelectedTheme = Properties.Settings.Default.SelectedTheme;

        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SelectedTheme = SelectedTheme;
            Properties.Settings.Default.Save();
            Close();
        }
    }
}
