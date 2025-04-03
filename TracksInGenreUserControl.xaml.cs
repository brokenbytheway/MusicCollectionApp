using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MusicCollectionApp
{
    public partial class TracksInGenreUserControl : UserControl
    {
        private GenresUserControl parentControl;
        private GenreModel genre;
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private readonly MusicPlayerService _musicPlayer;
        private ObservableCollection<TrackModel> _genreTracks;
        public ObservableCollection<TrackModel> GenreTracks => _genreTracks;
        public GenreModel Genre => genre;
        public event Action BackButtonClick;
        private List<TrackModel> _allGenreTracks; // Хранит все треки определённого жанра для поиска

        public TracksInGenreUserControl(GenresUserControl parent, GenreModel genreToOpen)
        {
            InitializeComponent();
            parentControl = parent;
            genre = genreToOpen;
            _musicPlayer = MusicPlayerService.Instance;
            _genreTracks = new ObservableCollection<TrackModel>();
            DataContext = this;
            connection = new MySqlConnection(mySqlCon);
            LoadGenreTracks();
        }

        private void LoadGenreTracks()
        {
            _genreTracks.Clear();
            _allGenreTracks = new List<TrackModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT track_id, track_title, album_id, number_in_album, genre_id, track_release_year, path_to_track_mp3_file, path_to_track_cover, is_single FROM TRACKS WHERE genre_id=@genre_id ORDER BY track_title", connection);
            command.Parameters.AddWithValue("@genre_id", genre.Id);

            List<string> trackPaths = new List<string>();
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int trackId = reader.GetInt32("track_id");
                    string title = reader.GetString("track_title");
                    int albumId = reader.GetInt32("album_id");
                    int numberInAlbum = reader.GetInt32("number_in_album");
                    int genreId = reader.GetInt32("genre_id");
                    int releaseYear = reader.GetInt32("track_release_year");
                    string coverPath = reader.IsDBNull(reader.GetOrdinal("path_to_track_cover")) ? "" : reader.GetString("path_to_track_cover");
                    string mp3Path = reader.IsDBNull(reader.GetOrdinal("path_to_track_mp3_file")) ? "" : reader.GetString("path_to_track_mp3_file");
                    int isSingle = reader.GetInt32("is_single");

                    string genre = GetGenreTitle(genreId);
                    List<string> artists = LoadTrackArtists(trackId);

                    _genreTracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));
                    _allGenreTracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));

                    if (!string.IsNullOrEmpty(mp3Path))
                    {
                        trackPaths.Add(mp3Path);
                    }
                }
            }
            connection.Close();

            _musicPlayer.SetPlaylist(trackPaths); // Устанавливаем плейлист
        }

        private string GetGenreTitle(int genreId)
        {
            string genre = "";

            using (MySqlConnection connection = new MySqlConnection(mySqlCon))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand("SELECT genre_title FROM GENRES WHERE genre_id=@genre_id", connection);
                command.Parameters.AddWithValue("@genre_id", genreId);
                object result = command.ExecuteScalar();

                if (result != null)
                {
                    genre = result.ToString();
                }
            }
            return genre;
        }

        private List<string> LoadTrackArtists(int trackId)
        {
            List<string> artists = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(mySqlCon))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand("SELECT ARTISTS.artist_nickname FROM ARTISTS JOIN TRACK_ARTISTS ON ARTISTS.artist_id=TRACK_ARTISTS.artist_id WHERE TRACK_ARTISTS.track_id=@track_id", connection);
                command.Parameters.AddWithValue("@track_id", trackId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        artists.Add(reader.GetString("artist_nickname"));
                    }
                }
            }
            return artists;
        }

        private void PlayTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pathToMP3File)
            {
                _musicPlayer.PlayTrack(pathToMP3File);
            }
        }

        private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TrackModel track)
            {
                var window = new ChoosePlaylistWindow(track);
                window.ShowDialog();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackButtonClick?.Invoke();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _genreTracks.Clear(); // Очищаем коллекцию

            var filteredTracks = string.IsNullOrWhiteSpace(searchText)
                ? _allGenreTracks // Если строка пустая — вернуть все треки
                : _allGenreTracks.Where(track => track.Title.ToLower().Contains(searchText)).ToList();

            foreach (var track in filteredTracks)
            {
                _genreTracks.Add(track);
            }
        }
    }
}
