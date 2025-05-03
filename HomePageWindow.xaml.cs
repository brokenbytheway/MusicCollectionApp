using System;
using System.Windows;
using System.Windows.Controls;

namespace MusicCollectionApp
{
    public partial class HomePageWindow : Window
    {
        private readonly HomePageViewModel _viewModel;
        private readonly MusicPlayerService _musicPlayer;

        public HomePageWindow()
        {
            InitializeComponent();
            _viewModel = new HomePageViewModel();
            _musicPlayer = MusicPlayerService.Instance;
            DataContext = _viewModel;
            _musicPlayer.TrackChanged += UpdateTrackUI;

            // Инициализация таймера для обновления слайдера
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); // Обновление каждую секунду
            timer.Tick += (s, e) => UpdateSlider();
            timer.Start();
        }

        private void UpdateTrackUI(string trackPath)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentTrackLabel.Text = System.IO.Path.GetFileName(trackPath); // Показываем имя файла
            });
        }

        private void UpdateSlider()
        {
            // Получаем текущее время и длительность трека
            var currentTime = _musicPlayer.GetCurrentTime();
            var duration = _musicPlayer.GetDuration();

            // Обновляем слайдер, если длительность трека не равна 0
            if (duration > 0)
            {
                ProgressSlider.Value = currentTime;
                ProgressSlider.Maximum = duration;

                // Обновление меток времени
                CurrentTimeLabel.Text = TimeSpan.FromSeconds(currentTime).ToString(@"mm\:ss");
                DurationLabel.Text = TimeSpan.FromSeconds(duration).ToString(@"mm\:ss");
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            _musicPlayer.ResumeTrack();
        }

        private void PrevTrack_Click(object sender, RoutedEventArgs e)
        {
            _musicPlayer.PlayPrevTrack();
        }

        private void NextTrack_Click(object sender, RoutedEventArgs e)
        {
            _musicPlayer.PlayNextTrack();
        }

        // Обработка изменения позиции на слайдере
        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Когда пользователь изменяет значение слайдера, перемещаем проигрыватель
            if (e.NewValue >= 0 && e.NewValue <= ProgressSlider.Maximum)
            {
                _musicPlayer.SeekToTime(e.NewValue); // Метод для перемещения проигрывателя
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Изменяем громкость плеера при изменении значения слайдера
            if (_musicPlayer != null)
            {
                _musicPlayer.SetVolume(e.NewValue);
            }
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
                        _viewModel.SwitchView(new HomeUserControl(), "Welcome!");
                        break;
                    case "Playlists":
                        var playlistsControl = new PlaylistsUserControl();

                        playlistsControl.PlaylistSelected += playlist =>
                        {
                            var tracksControl = new TracksInPlaylistUserControl(playlistsControl, playlist);

                            tracksControl.BackButtonClick += () =>
                            {
                                _viewModel.SwitchView(playlistsControl, "Playlists");
                            };

                            _viewModel.SwitchView(tracksControl, playlist.Title);
                        };

                        _viewModel.SwitchView(playlistsControl, "Playlists");
                        break;
                    case "Tracks":
                        _viewModel.SwitchView(new TracksUserControl(), "Tracks");
                        break;
                    case "Albums":
                        var albumsControl = new AlbumsUserControl();

                        albumsControl.AlbumSelected += album =>
                        {
                            var tracksControl = new TracksInAlbumUserControl(albumsControl, album);

                            tracksControl.BackButtonClick += () =>
                            {
                                _viewModel.SwitchView(albumsControl, "Albums");
                            };

                            _viewModel.SwitchView(tracksControl, album.Title);
                        };

                        _viewModel.SwitchView(albumsControl, "Albums");
                        break;
                    case "Artists":
                        var artistsControl = new ArtistsUserControl();

                        artistsControl.ArtistSelected += artist =>
                        {
                            var tracksControl = new TracksInArtistUserControl(artistsControl, artist);

                            tracksControl.BackButtonClick += () =>
                            {
                                _viewModel.SwitchView(artistsControl, "Artists");
                            };

                            _viewModel.SwitchView(tracksControl, artist.Nickname);
                        };

                        _viewModel.SwitchView(artistsControl, "Artists");
                        break;
                    case "Genres":
                        var genresControl = new GenresUserControl();

                        genresControl.GenreSelected += genre =>
                        {
                            var tracksControl = new TracksInGenreUserControl(genresControl, genre);

                            tracksControl.BackButtonClick += () =>
                            {
                                _viewModel.SwitchView(genresControl, "Genres");
                            };

                            _viewModel.SwitchView(tracksControl,genre.Title);
                        };

                        _viewModel.SwitchView(genresControl, "Genres");
                        break;
                    case "Settings":
                        _viewModel.SwitchView(new SettingsUserControl(), "Settings");
                        break;
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Руководство пользователя.docx");

            if (System.IO.File.Exists(helpFilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = helpFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при открытии руководства пользователя");
                }
            }
            else
            {
                MessageBox.Show("Файл с руководством пользователя не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
