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
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";

        public EditGenreWindow(GenresUserControl parent, GenreModel genreToEdit)
        {
            InitializeComponent();
            parentControl = parent;
            genre = genreToEdit;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();

            genreTitleTextBox.Text = genre.Title; // Заносим текущие данные в окно
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
