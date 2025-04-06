using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicCollectionApp
{
    public partial class AlbumsUserControl : UserControl
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private ObservableCollection<AlbumModel> _albums;
        public ObservableCollection<AlbumModel> Albums => _albums;
        private List<AlbumModel> _allAlbums; // Хранит все альбомы для поиска
        public event Action<AlbumModel> AlbumSelected;

        public AlbumsUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _albums = new ObservableCollection<AlbumModel>();
            LoadUserId();
            LoadAlbums();
        }

        private void DeleteAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AlbumModel album)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить альбом '{album.Title}'?\n\nВсе треки из этого альбома тоже будут удалены.", "Удаление альбома", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.Open();

                        using (MySqlCommand deleteAlbumCommand = new MySqlCommand("DELETE FROM ALBUMS WHERE album_id=@album_id AND user_id=@user_id", connection))
                        {
                            deleteAlbumCommand.Parameters.AddWithValue("@album_id", album.Id);
                            deleteAlbumCommand.Parameters.AddWithValue("@user_id", userId);
                            deleteAlbumCommand.ExecuteNonQuery();
                        }

                        _albums.Remove(album);
                        _allAlbums.Remove(album);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении альбома");
                    }
                }
            }
        }

        private void EditAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AlbumModel album)
            {
                EditAlbumWindow editAlbumWindow = new EditAlbumWindow(this, album);
                editAlbumWindow.ShowDialog();
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
        }

        private void LoadAlbums()
        {
            _albums.Clear();
            _allAlbums = new List<AlbumModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT album_id, album_title, album_release_year, path_to_album_cover FROM ALBUMS WHERE user_id=@user_id ORDER BY album_title", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int albumId = reader.GetInt32("album_id");
                    string title = reader.GetString("album_title");
                    int releaseYear = reader.GetInt32("album_release_year");
                    string coverPath = reader.IsDBNull(reader.GetOrdinal("path_to_album_cover")) ? "" : reader.GetString("path_to_album_cover");

                    List<string> artists = LoadAlbumArtists(albumId);

                    _albums.Add(new AlbumModel(albumId, title, releaseYear, coverPath, artists));
                    _allAlbums.Add(new AlbumModel(albumId, title, releaseYear, coverPath, artists));
                }
            }
            connection.Close();
        }

        private List<string> LoadAlbumArtists(int albumId)
        {
            List<string> artists = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(mySqlCon))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand("SELECT ARTISTS.artist_nickname FROM ARTISTS JOIN ALBUM_ARTISTS ON ARTISTS.artist_id=ALBUM_ARTISTS.artist_id WHERE ALBUM_ARTISTS.album_id=@album_id", connection);
                command.Parameters.AddWithValue("@album_id", albumId);

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

        public void RefreshAlbums()
        {
            LoadAlbums();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddAlbumWindow addAlbumWindow = new AddAlbumWindow(this);
            addAlbumWindow.ShowDialog();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _albums.Clear(); // Очищаем коллекцию

            var filteredAlbums = string.IsNullOrWhiteSpace(searchText)
                ? _allAlbums // Если строка пустая — вернуть все альбомы
                : _allAlbums.Where(album => album.Title.ToLower().Contains(searchText) || album.ArtistsString.ToLower().Contains(searchText)).ToList();

            foreach (var album in filteredAlbums)
            {
                _albums.Add(album);
            }
        }

        private void Album_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is AlbumModel album)
            {
                AlbumSelected?.Invoke(album);
            }
        }
    }
}
