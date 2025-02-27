using System;
using System.Collections.Generic;
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

namespace MusicCollectionApp
{
    public partial class HomePageWindow : Window
    {
        private readonly HomePageViewModel _viewModel;

        public HomePageWindow()
        {
            InitializeComponent();
            _viewModel = new HomePageViewModel();
            DataContext = _viewModel;
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string tag = button.Tag?.ToString();
                if (tag == null) return;

                switch (tag)
                {
                    case "Home":
                        _viewModel.SwitchView(new HomeUserControl(), "Home");
                        break;
                    case "Playlists":
                        _viewModel.SwitchView(new PlaylistsUserControl(), "Playlists");
                        break;
                    //case "Songs":
                    //    _viewModel.SwitchView(new SongsUserControl(), "Songs");
                    //    break;
                    case "Albums":
                        _viewModel.SwitchView(new AlbumsUserControl(), "Albums");
                        break;
                    case "Artists":
                        _viewModel.SwitchView(new ArtistsUserControl(), "Artists");
                        break;
                    case "Genres":
                        _viewModel.SwitchView(new GenresUserControl(), "Genres");
                        break;
                    //case "Settings":
                    //    _viewModel.SwitchView(new SettingsUserControl(), "Settings");
                    //    break;
                }
            }
        }
    }
}
