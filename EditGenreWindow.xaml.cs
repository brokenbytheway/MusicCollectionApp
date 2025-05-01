using DocumentFormat.OpenXml.Spreadsheet;
using MySql.Data.MySqlClient;
using System;
using System.Windows;

namespace MusicCollectionApp
{
    public partial class EditGenreWindow : Window
    {
        private MySqlConnection connection;
        private GenresUserControl parentControl;
        private GenreModel genre;
        private int userId;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";

        public EditGenreWindow(GenresUserControl parent, GenreModel genreToEdit)
        {
            InitializeComponent();
            parentControl = parent;
            genre = genreToEdit;
            LoadUserId();
            connection = new MySqlConnection(mySqlCon);
            connection.Open();

            genreTitleTextBox.Text = genre.Title; // Заносим текущие данные в окно
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

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string newTitle = genreTitleTextBox.Text;

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                MessageBox.Show("Заполните все поля для изменения жанра!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM GENRES WHERE genre_title=@genre_title AND user_id=@user_id", connection);
                checkCommand.Parameters.AddWithValue("@genre_title", newTitle);
                checkCommand.Parameters.AddWithValue("@user_id", userId);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    MessageBox.Show("Такой жанр уже существует в вашей коллекции!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MySqlCommand command = new MySqlCommand("UPDATE GENRES SET genre_title=@new_title WHERE genre_id=@genre_id", connection);
                command.Parameters.AddWithValue("@new_title", newTitle);
                command.Parameters.AddWithValue("@genre_id", genre.Id);
                command.ExecuteNonQuery();

                parentControl.RefreshGenres();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при обновлении жанра");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
