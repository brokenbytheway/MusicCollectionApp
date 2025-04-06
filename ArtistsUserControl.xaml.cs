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
    public partial class ArtistsUserControl : UserControl
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private ObservableCollection<ArtistModel> _artists;
        public ObservableCollection<ArtistModel> Artists => _artists;
        private List<ArtistModel> _allArtists; // Хранит всех исполнителей для поиска
        public event Action<ArtistModel> ArtistSelected;

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
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить исполнителя '{artist.Nickname}'?\n\nВсе альбомы и треки этого исполнителя тоже будут удалены.", "Удаление исполнителя", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.Open();

                        using (MySqlCommand deleteArtistcommand = new MySqlCommand("DELETE FROM ARTISTS WHERE artist_id=@artist_id and user_id=@user_id", connection))
                        {
                            deleteArtistcommand.Parameters.AddWithValue("@artist_id", artist.Id);
                            deleteArtistcommand.Parameters.AddWithValue("@user_id", userId);
                            deleteArtistcommand.ExecuteNonQuery();
                        }

                        _artists.Remove(artist);
                        _allArtists.Remove(artist);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении исполнителя");
                    }
                }
            }
        }

        private void EditArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ArtistModel artist)
            {
                EditArtistWindow editArtistWindow = new EditArtistWindow(this, artist);
                editArtistWindow.ShowDialog();
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
            _allArtists = new List<ArtistModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT artist_id, artist_nickname, path_to_artist_photo, (SELECT COUNT(*) FROM TRACK_ARTISTS WHERE TRACK_ARTISTS.artist_id = ARTISTS.artist_id) AS track_count FROM ARTISTS WHERE user_id=@user_id ORDER BY artist_nickname", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int artistId = reader.GetInt32("artist_id");
                    string nickname = reader.GetString("artist_nickname");
                    string coverPath = reader.IsDBNull(reader.GetOrdinal("path_to_artist_photo")) ? "" : reader.GetString("path_to_artist_photo");
                    int trackCount = reader.GetInt32("track_count");

                    _artists.Add(new ArtistModel(artistId, nickname, coverPath, trackCount));
                    _allArtists.Add(new ArtistModel(artistId, nickname, coverPath, trackCount));
                }
            }
            connection.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddArtistWindow addArtistWindow = new AddArtistWindow(this);
            addArtistWindow.ShowDialog();
        }

        public void RefreshArtists()
        {
            LoadArtists();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _artists.Clear(); // Очищаем коллекцию

            var filteredArtists = string.IsNullOrWhiteSpace(searchText)
                ? _allArtists // Если строка пустая — вернуть всех исполнителей
                : _allArtists.Where(artist => artist.Nickname.ToLower().Contains(searchText)).ToList();

            foreach (var artist in filteredArtists)
            {
                _artists.Add(artist);
            }
        }

        private void Artist_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ArtistModel artist)
            {
                ArtistSelected?.Invoke(artist);
            }
        }
    }
}
