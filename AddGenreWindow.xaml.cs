using MySql.Data.MySqlClient;
using System;
using System.Windows;

namespace MusicCollectionApp
{
    public partial class AddGenreWindow : Window
    {
        MySqlConnection connection;
        string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        int userId;
        private GenresUserControl parentControl;

        public AddGenreWindow(GenresUserControl parent)
        {
            InitializeComponent();
            parentControl = parent;
            LoadUserId();
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
            string genreTitle = genreTitleTextBox.Text;
            if (string.IsNullOrWhiteSpace(genreTitle))
            {
                MessageBox.Show("Заполните все поля для добавления жанра!");
                return;
            }

            try
            {
                MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM GENRES WHERE genre_title=@genre_title AND user_id=@user_id", connection);
                checkCommand.Parameters.AddWithValue("@genre_title", genreTitle);
                checkCommand.Parameters.AddWithValue("@user_id", userId);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    MessageBox.Show("Такой жанр уже существует в вашей коллекции!");
                    return;
                }

                MySqlCommand insertCommand = new MySqlCommand("INSERT INTO GENRES (genre_title, user_id) VALUES (@genre_title, @user_id)", connection);
                insertCommand.Parameters.AddWithValue("@genre_title", genreTitle);
                insertCommand.Parameters.AddWithValue("@user_id", userId);

                insertCommand.ExecuteNonQuery();
                parentControl.RefreshGenres();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при добавлении жанра");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
