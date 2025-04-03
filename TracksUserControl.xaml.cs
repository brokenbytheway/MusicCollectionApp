using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace MusicCollectionApp
{

    public partial class TracksUserControl : UserControl
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private ObservableCollection<TrackModel> _tracks;
        public ObservableCollection<TrackModel> Tracks => _tracks;
        private readonly MusicPlayerService _musicPlayer;
        private List<TrackModel> _allTracks; // Хранит все треки для поиска

        public TracksUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _tracks = new ObservableCollection<TrackModel>();
            _musicPlayer = MusicPlayerService.Instance;
            LoadUserId();
            LoadTracks();
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TrackModel track)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить трек '{track.Title}'?", "Удаление трека", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.Open();

                        using (MySqlCommand deleteTrackCommand = new MySqlCommand("DELETE FROM TRACKS WHERE track_id=@track_id", connection))
                        {
                            deleteTrackCommand.Parameters.AddWithValue("@track_id", track.Id);
                            deleteTrackCommand.ExecuteNonQuery();
                        }

                        // Проверяем, был ли трек синглом и остались ли ещё треки в альбоме
                        if (track.IsSingle == 1 && IsAlbumEmpty(track.AlbumId))
                        {
                            using (MySqlCommand deleteAlbumCommand = new MySqlCommand("DELETE FROM ALBUMS WHERE album_id=@album_id", connection))
                            {
                                deleteAlbumCommand.Parameters.AddWithValue("@album_id", track.AlbumId);
                                deleteAlbumCommand.ExecuteNonQuery();
                            }
                        }

                        _tracks.Remove(track);
                        _allTracks.Remove(track);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении трека");
                    }
                }
            }
        }

        private bool IsAlbumEmpty(int albumId)
        {
            using (MySqlCommand checkTracksCommand = new MySqlCommand("SELECT COUNT(*) FROM TRACKS WHERE album_id=@album_id", connection))
            {
                checkTracksCommand.Parameters.AddWithValue("@album_id", albumId);
                int trackCount = Convert.ToInt32(checkTracksCommand.ExecuteScalar());
                return trackCount == 0; // Если треков нет — альбом пустой
            }
        }

        private void LoadUserId()
        {
            connection = new MySqlConnection(mySqlCon);
            connection.Open();
            MySqlCommand command = new MySqlCommand("SELECT user_id FROM USERS WHERE user_login=@login AND user_password=@password", connection);
            command.Parameters.AddWithValue("@login", AuthWindow.login);
            command.Parameters.AddWithValue("@password", AuthWindow.password);
            userId = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
        }

        private void LoadTracks()
        {
            _tracks.Clear();
            _allTracks = new List<TrackModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT track_id, track_title, album_id, number_in_album, genre_id, track_release_year, path_to_track_mp3_file, path_to_track_cover, is_single FROM TRACKS WHERE album_id IN (SELECT album_id FROM ALBUMS WHERE user_id=@user_id) ORDER BY track_title", connection);
            command.Parameters.AddWithValue("@user_id", userId);

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

                    //string album = GetAlbumTitle(albumId);
                    string genre = GetGenreTitle(genreId);
                    List<string> artists = LoadTrackArtists(trackId);

                    _tracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));
                    _allTracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));
                    
                    if (!string.IsNullOrEmpty(mp3Path))
                    {
                        trackPaths.Add(mp3Path);
                    }
                }
            }
            connection.Close();

            _musicPlayer.SetPlaylist(trackPaths); // Устанавливаем плейлист
        }

        //private string GetAlbumTitle(int albumId)
        //{
        //    string album = "";

        //    using (MySqlConnection connection = new MySqlConnection(mySqlCon))
        //    {
        //        connection.Open();
        //        MySqlCommand command = new MySqlCommand("SELECT album_title FROM ALBUMS WHERE album_id=@album_id", connection);
        //        command.Parameters.AddWithValue("@album_id", albumId);
        //        object result = command.ExecuteScalar();

        //        if (result != null)
        //        {
        //            album = result.ToString();
        //        }
        //    }
        //    return album;
        //}

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

        public void RefreshTracks()
        {
            LoadTracks();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddTrackWindow addTrackWindow = new AddTrackWindow(this);
            addTrackWindow.ShowDialog();
        }

        private void PlayTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pathToMP3File)
            {
                _musicPlayer.PlayTrack(pathToMP3File);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _tracks.Clear(); // Очищаем коллекцию

            var filteredTracks = string.IsNullOrWhiteSpace(searchText)
                ? _allTracks // Если строка пустая — вернуть все треки
                : _allTracks.Where(track => track.Title.ToLower().Contains(searchText) || track.ArtistsString.ToLower().Contains(searchText)).ToList();

            foreach (var track in filteredTracks)
            {
                _tracks.Add(track);
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

        private void EditTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TrackModel track)
            {
                EditTrackWindow editTrackWindow = new EditTrackWindow(this, track);
                editTrackWindow.ShowDialog();
            }
        }
    }
}
