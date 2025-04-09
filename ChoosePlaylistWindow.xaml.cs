using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MusicCollectionApp
{
    public partial class ChoosePlaylistWindow : Window
    {
        MySqlConnection connection;
        string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        int userId;
        private List<int> selectedPlaylistIds = new List<int>();
        private List<CheckBox> allPlaylistCheckboxes = new List<CheckBox>();
        private TrackModel _track;

        public ChoosePlaylistWindow(TrackModel track)
        {
            InitializeComponent();
            _track = track;
            LoadUserId();
            LoadPlaylists();
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
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT playlist_id, playlist_title FROM PLAYLISTS WHERE user_id=@user_id", connection);
            command.Parameters.AddWithValue("@user_id", userId);
            MySqlDataReader reader = command.ExecuteReader();

            allPlaylistCheckboxes.Clear();
            playlistsListBox.Items.Clear();

            List<CheckBox> checkBoxes = new List<CheckBox>();
            while (reader.Read())
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = reader["playlist_title"].ToString(),
                    Tag = reader["playlist_id"].ToString()
                };
                checkBox.Checked += PlaylistCheckbox_Checked;
                checkBox.Unchecked += PlaylistCheckbox_Unchecked;

                checkBoxes.Add(checkBox);
            }
            reader.Close();

            checkBoxes = checkBoxes.OrderBy(cb => cb.Content.ToString()).ToList(); // Сортируем в алфавитном порядке

            allPlaylistCheckboxes.AddRange(checkBoxes);
            foreach (var checkBox in checkBoxes)
            {
                playlistsListBox.Items.Add(checkBox);
            }
        }

        private void PlaylistCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                selectedPlaylistIds.Add(Convert.ToInt32(checkBox.Tag));
            }
        }

        private void PlaylistCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                selectedPlaylistIds.Remove(Convert.ToInt32(checkBox.Tag));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();
            playlistsListBox.Items.Clear();

            foreach (CheckBox checkBox in allPlaylistCheckboxes)
            {
                if (checkBox.Content.ToString().ToLower().Contains(searchText))
                {
                    playlistsListBox.Items.Add(checkBox);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один плейлист!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (int playlistId in selectedPlaylistIds)
                {
                    MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM PLAYLIST_TRACKS WHERE playlist_id=@playlist_id AND track_id=@track_id", connection);
                    checkCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                    checkCommand.Parameters.AddWithValue("@track_id", _track.Id);

                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count > 0)
                    {
                        MessageBox.Show("Такой трек уже есть в этом плейлисте!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Получаем максимальный number_in_playlist для текущего плейлиста
                    MySqlCommand getMaxNumberCommand = new MySqlCommand("SELECT IFNULL(MAX(number_in_playlist), 0) FROM PLAYLIST_TRACKS WHERE playlist_id = @playlist_id", connection);
                    getMaxNumberCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                    int maxNumber = Convert.ToInt32(getMaxNumberCommand.ExecuteScalar());
                    int newNumber = maxNumber + 1;

                    // Добавляем трек в плейлист
                    MySqlCommand insertCommand = new MySqlCommand("INSERT INTO PLAYLIST_TRACKS (playlist_id, track_id, number_in_playlist, last_modified_time) VALUES (@playlist_id, @track_id, @number_in_playlist, @modified)", connection);
                    insertCommand.Parameters.AddWithValue("@playlist_id", playlistId);
                    insertCommand.Parameters.AddWithValue("@track_id", _track.Id);
                    insertCommand.Parameters.AddWithValue("@number_in_playlist", newNumber);
                    insertCommand.Parameters.AddWithValue("@modified", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при добавлении трека в плейлист");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
