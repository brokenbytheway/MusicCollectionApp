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
    public partial class TracksInPlaylistUserControl : UserControl
    {
        private PlaylistsUserControl parentControl;
        private PlaylistModel playlist;
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private readonly MusicPlayerService _musicPlayer;
        private ObservableCollection<TrackModel> _playlistTracks;
        public ObservableCollection<TrackModel> PlaylistTracks => _playlistTracks;
        public PlaylistModel Playlist => playlist;
        public event Action BackButtonClick;
        private List<TrackModel> _allPlaylistTracks; // Хранит все треки в плейлисте для поиска

        public TracksInPlaylistUserControl(PlaylistsUserControl parent, PlaylistModel playlistToOpen)
        {
            InitializeComponent();
            parentControl = parent;
            playlist = playlistToOpen;
            _musicPlayer = MusicPlayerService.Instance;
            _playlistTracks = new ObservableCollection<TrackModel>();
            DataContext = this;
            connection = new MySqlConnection(mySqlCon);
            LoadPlaylistTracks();
        }

        private void LoadPlaylistTracks()
        {
            _playlistTracks.Clear();
            _allPlaylistTracks = new List<TrackModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT t.track_id, t.track_title, t.album_id, t.number_in_album, t.genre_id, t.track_release_year, t.path_to_track_mp3_file, t.path_to_track_cover, t.is_single, pt.number_in_playlist, pt.last_modified_time FROM PLAYLIST_TRACKS pt JOIN TRACKS t ON pt.track_id=t.track_id WHERE pt.playlist_id=@playlist_id ORDER BY pt.number_in_playlist ASC", connection);
            command.Parameters.AddWithValue("@playlist_id", playlist.Id);

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

                    _playlistTracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));
                    _allPlaylistTracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));

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

        private void DeleteTrackFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TrackModel track)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить трек '{track.Title}' из плейлиста?", "Удаление трека из плейлиста", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.Open();

                        using (MySqlCommand command = new MySqlCommand("DELETE FROM PLAYLIST_TRACKS WHERE playlist_id=@playlist_id AND track_id=@track_id", connection))
                        {
                            command.Parameters.AddWithValue("@playlist_id", playlist.Id);
                            command.Parameters.AddWithValue("@track_id", track.Id);
                            command.ExecuteNonQuery();
                        }

                        _playlistTracks.Remove(track);
                        _allPlaylistTracks.Remove(track);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении трека из плейлиста");
                    }
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _playlistTracks.Clear(); // Очищаем коллекцию

            var filteredTracks = string.IsNullOrWhiteSpace(searchText)
                ? _allPlaylistTracks // Если строка пустая — вернуть все треки
                : _allPlaylistTracks.Where(track => track.Title.ToLower().Contains(searchText) || track.ArtistsString.ToLower().Contains(searchText)).ToList();

            foreach (var track in filteredTracks)
            {
                _playlistTracks.Add(track);
            }
        }
    }
}
