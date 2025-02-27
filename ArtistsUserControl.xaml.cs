using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicCollectionApp
{
    public partial class ArtistsUserControl : UserControl
    {
        MySqlConnection connection;
        string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        int userId;
        private ObservableCollection<ArtistModel> _artists;
        public IEnumerable<ArtistModel> Artists => _artists;

        public ArtistsUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _artists = new ObservableCollection<ArtistModel>();
            LoadUserId();
            LoadArtists();
        }

        private void DeleteArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ArtistModel artist)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить исполнителя '{artist.Nickname}'?","Удаление исполнителя", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(mySqlCon))
                        {
                            connection.Open();
                            MySqlCommand command = new MySqlCommand("DELETE FROM ARTISTS WHERE artist_id = @artist_id and user_id=@user_id", connection);
                            command.Parameters.AddWithValue("@artist_id", artist.Id);
                            command.Parameters.AddWithValue("@user_id", userId);
                            command.ExecuteNonQuery();
                        }

                        _artists.Remove(artist);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении");
                    }
                }
            }
        }

        private void EditArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ArtistModel artist)
            {
                EditArtistWindow editWindow = new EditArtistWindow(this, artist);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshArtists();
                }
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

        private void LoadArtists()
        {
            _artists.Clear();
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT artist_id, artist_nickname, path_to_artist_photo, " + "(SELECT COUNT(*) FROM TRACK_ARTISTS WHERE TRACK_ARTISTS.artist_id = ARTISTS.artist_id) AS track_count " + "FROM ARTISTS WHERE user_id=@user_id ORDER BY artist_nickname", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string imagePath = reader.IsDBNull(reader.GetOrdinal("path_to_artist_photo")) ? null : reader.GetString("path_to_artist_photo");
                    _artists.Add(new ArtistModel(
                        reader.GetInt32("artist_id"),
                        reader.GetString("artist_nickname"),
                        reader.IsDBNull(reader.GetOrdinal("path_to_artist_photo")) ? "" : reader.GetString("path_to_artist_photo"),
                        reader.GetInt32("track_count")));
                }
            }
            connection.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddArtistWindow addArtistWindow = new AddArtistWindow(this);
            addArtistWindow.Show();
        }

        public void RefreshArtists()
        {
            LoadArtists();
        }

        private void TrackCount_Click(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
