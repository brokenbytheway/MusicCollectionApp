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
    public partial class GenresUserControl : UserControl
    {
        MySqlConnection connection;
        string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        int userId;
        private ObservableCollection<GenreModel> _genres;
        public IEnumerable<GenreModel> Genres => _genres;

        public GenresUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _genres = new ObservableCollection<GenreModel>();
            LoadUserId();
            LoadGenres();
        }

        private void DeleteGenre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GenreModel genre)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить жанр '{genre.Title}'?", "Удаление жанра", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(mySqlCon))
                        {
                            connection.Open();
                            MySqlCommand command = new MySqlCommand("DELETE FROM GENRES WHERE genre_id=@genre_id and user_id=@user_id", connection);
                            command.Parameters.AddWithValue("@genre_id", genre.Id);
                            command.Parameters.AddWithValue("@user_id", userId);
                            command.ExecuteNonQuery();
                        }

                        _genres.Remove(genre);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении");
                    }
                }
            }
        }

        private void EditGenre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GenreModel genre)
            {
                EditGenreWindow editWindow = new EditGenreWindow(this, genre);
                editWindow.Show();
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

        private void LoadGenres()
        {
            _genres.Clear();
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT genre_id, genre_title, " + "(SELECT COUNT(*) FROM TRACKS WHERE TRACKS.genre_id = GENRES.genre_id) AS track_count " + "FROM GENRES WHERE user_id=@user_id ORDER BY genre_title", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    _genres.Add(new GenreModel(
                        reader.GetInt32("genre_id"),
                        reader.GetString("genre_title"),
                        reader.GetInt32("track_count")));
                }
            }
            connection.Close();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddGenreWindow addGenreWindow = new AddGenreWindow(this);
            addGenreWindow.Show();
        }

        public void RefreshGenres()
        {
            LoadGenres();
        }

        private void TrackCount_Click(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
