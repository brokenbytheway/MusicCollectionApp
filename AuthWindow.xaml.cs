using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace MusicCollectionApp
{
    public partial class AuthWindow : Window
    {
        public static string login, password;
        private MySqlConnection connection = new MySqlConnection("Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;");
        private string userSalt;

        public AuthWindow()
        {
            InitializeComponent();
        }

        private void LoadUserSalt()
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT user_salt FROM USERS WHERE user_login=@login", connection);
            command.Parameters.AddWithValue("@login", login);
            userSalt = Convert.ToString(command.ExecuteScalar());
        }

        private string GenerateHash(string password, string salt)
        {
            var sha256 = new SHA256CryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private void Button_Auth_Click(object sender, RoutedEventArgs e)
        {
            login = textBoxLogin.Text.Trim();
            password = passBox.Password.Trim();

            if (login.Length < 5)
            {
                textBoxLogin.ToolTip = "Логин должен состоять минимум из 5 символов!";
                textBoxLogin.Background = Brushes.LightPink;
                return;
            }

            if (password.Length < 8)
            {
                passBox.ToolTip = "Пароль должен состоять минимум из 8 символов!";
                passBox.Background = Brushes.LightPink;
                return;
            }

            textBoxLogin.ToolTip = null;
            textBoxLogin.Background = Brushes.Transparent;
            passBox.ToolTip = null;
            passBox.Background = Brushes.Transparent;

            // Хэширование введённого пароля с солью из БД
            LoadUserSalt();
            string hashedInputPassword = GenerateHash(password, userSalt);
            password = hashedInputPassword;

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            try
            {
                MySqlCommand command = new MySqlCommand("SELECT COUNT(1) FROM USERS WHERE user_login=@login AND user_password=@password", connection);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", password);
                int count = Convert.ToInt32(command.ExecuteScalar());

                if (count == 1)
                {
                    MessageBox.Show("Вы успешно вошли в аккаунт!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    connection.Close();
                    HomePageWindow homePageWindow = new HomePageWindow();
                    homePageWindow.Show();
                    Close();
                }

                else
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при входе в аккаунт");
            }
        }

        private void Button_Window_Reg_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }
    }
}
