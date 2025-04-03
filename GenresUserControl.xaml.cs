using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicCollectionApp
{
    public partial class GenresUserControl : UserControl
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private ObservableCollection<GenreModel> _genres;
        public ObservableCollection<GenreModel> Genres => _genres;
        private List<GenreModel> _allGenres; // Хранит все жанры для поиска
        public event Action<GenreModel> GenreSelected;

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
                        connection.Open();

                        using (MySqlCommand command = new MySqlCommand("DELETE FROM GENRES WHERE genre_id=@genre_id and user_id=@user_id", connection))
                        {
                            command.Parameters.AddWithValue("@genre_id", genre.Id);
                            command.Parameters.AddWithValue("@user_id", userId);
                            command.ExecuteNonQuery();
                        }

                        _genres.Remove(genre);
                        _allGenres.Remove(genre);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении жанра");
                    }
                }
            }
        }

        private void EditGenre_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GenreModel genre)
            {
                EditGenreWindow editGenreWindow = new EditGenreWindow(this, genre);
                editGenreWindow.ShowDialog();
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
            _allGenres = new List<GenreModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT g.genre_id, g.genre_title, (SELECT COUNT(*) FROM TRACKS t WHERE t.genre_id = g.genre_id) AS track_count FROM GENRES g WHERE g.user_id = @user_id ORDER BY g.genre_title", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int genreId = reader.GetInt32("genre_id");
                    string title = reader.GetString("genre_title");
                    int trackCount = reader.GetInt32("track_count");

                    _genres.Add(new GenreModel(genreId, title, trackCount));
                    _allGenres.Add(new GenreModel(genreId, title, trackCount));
                }
            }
            connection.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddGenreWindow addGenreWindow = new AddGenreWindow(this);
            addGenreWindow.ShowDialog();
        }

        public void RefreshGenres()
        {
            LoadGenres();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _genres.Clear(); // Очищаем коллекцию

            var filteredGenres = string.IsNullOrWhiteSpace(searchText)
                ? _allGenres // Если строка пустая — вернуть все жанры
                : _allGenres.Where(genre => genre.Title.ToLower().Contains(searchText)).ToList();

            foreach (var genre in filteredGenres)
            {
                _genres.Add(genre);
            }
        }

        private void Genre_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is GenreModel genre)
            {
                GenreSelected?.Invoke(genre);
            }
        }
    }
}
