using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace MusicCollectionApp
{
    public partial class PlaylistsUserControl : UserControl
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private ObservableCollection<PlaylistModel> _playlists;
        public ObservableCollection<PlaylistModel> Playlists => _playlists;
        private List<PlaylistModel> _allPlaylists; // Хранит все плейлисты для поиска
        public event Action<PlaylistModel> PlaylistSelected;

        public PlaylistsUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _playlists = new ObservableCollection<PlaylistModel>();
            LoadUserId();
            LoadPlaylists();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlaylistModel playlist)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить плейлист '{playlist.Title}'?", "Удаление плейлиста", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.Open();

                        using (MySqlCommand deletePlaylistCommand = new MySqlCommand("DELETE FROM PLAYLISTS WHERE playlist_id=@playlist_id AND user_id=@user_id", connection))
                        {
                            deletePlaylistCommand.Parameters.AddWithValue("@playlist_id", playlist.Id);
                            deletePlaylistCommand.Parameters.AddWithValue("@user_id", userId);
                            deletePlaylistCommand.ExecuteNonQuery();
                        }

                        _playlists.Remove(playlist);
                        _allPlaylists.Remove(playlist);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении плейлиста");
                    }
                }
            }
        }

        private void EditPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlaylistModel playlist)
            {
                EditPlaylistWindow editPlaylistWindow = new EditPlaylistWindow(this, playlist);
                editPlaylistWindow.ShowDialog();
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

        private void LoadPlaylists()
        {
            _playlists.Clear();
            _allPlaylists = new List<PlaylistModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT playlist_id, playlist_title, playlist_description, path_to_playlist_cover FROM PLAYLISTS WHERE user_id=@user_id ORDER BY playlist_title", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int playlistId = reader.GetInt32("playlist_id");
                    string title = reader.GetString("playlist_title");
                    string description = reader.GetString("playlist_description");
                    string coverPath = reader.IsDBNull(reader.GetOrdinal("path_to_playlist_cover")) ? "" : reader.GetString("path_to_playlist_cover");

                    _playlists.Add(new PlaylistModel(playlistId, title, description, coverPath));
                    _allPlaylists.Add(new PlaylistModel(playlistId, title, description, coverPath));
                }
            }
            connection.Close();
        }

        public void RefreshPlaylists()
        {
            LoadPlaylists();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddPlaylistWindow addPlaylistWindow = new AddPlaylistWindow(this);
            addPlaylistWindow.ShowDialog();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _playlists.Clear(); // Очищаем коллекцию

            var filteredPlaylists = string.IsNullOrWhiteSpace(searchText)
                ? _allPlaylists // Если строка пустая — вернуть все плейлисты
                : _allPlaylists.Where(playlist => playlist.Title.ToLower().Contains(searchText)).ToList();

            foreach (var playlist in filteredPlaylists)
            {
                _playlists.Add(playlist);
            }
        }

        private void Playlist_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PlaylistModel playlist)
            {
                PlaylistSelected?.Invoke(playlist);
            }
        }
    }
}
