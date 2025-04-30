using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace MusicCollectionApp
{
    public partial class MainWindow : Window
    {
        private MySqlConnection connection = new MySqlConnection("Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;");

        public MainWindow()
        {
            InitializeComponent();
        }

        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 && password.Any(char.IsUpper) && password.Any(char.IsDigit) && password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private string GenerateSalt()
        {
            byte[] salt = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        private string GenerateHash(string password, string salt)
        {
            var sha256 = new SHA256CryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private void Button_Reg_Click(object sender, RoutedEventArgs e)
        {
            string login = textBoxLogin.Text.Trim();
            string password = passBox.Password.Trim();
            string password2 = passBox2.Password.Trim();

            if (login.Length < 5)
            {
                textBoxLogin.ToolTip = "Логин должен состоять минимум из 5 символов!";
                textBoxLogin.Background = Brushes.LightPink;
                return;
            }
            else
            {
                textBoxLogin.ToolTip = null;
                textBoxLogin.Background = Brushes.Transparent;
            }

            if (!IsValidPassword(password))
            {
                passBox.ToolTip = "Пароль должен состоять минимум из 8 символов, включая заглавную букву, цифру и спецсимвол!";
                passBox.Background = Brushes.LightPink;
                return;
            }
            else
            {
                passBox.ToolTip = null;
                passBox.Background = Brushes.Transparent;
            }

            if (password != password2)
            {
                passBox2.ToolTip = "Пароли не совпадают!";
                passBox2.Background = Brushes.LightPink;
                return;
            }
            else
            {
                passBox2.ToolTip = null;
                passBox2.Background = Brushes.Transparent;
            }

            if (login.Length >= 5 && IsValidPassword(password) && password == password2)
            {
                // Хэширование введённого пароля с солью
                string salt = GenerateSalt();
                string hashedPassword = GenerateHash(password, salt);
                password = hashedPassword;

                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                try
                {
                    MySqlCommand checkUserCommand = new MySqlCommand("SELECT COUNT(*) FROM USERS WHERE user_login = @login", connection);
                    checkUserCommand.Parameters.AddWithValue("@login", login);

                    int userCount = Convert.ToInt32(checkUserCommand.ExecuteScalar());
                    if (userCount > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует! Пожалуйста, придумайте другой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    else
                    {
                        MySqlCommand command = new MySqlCommand("INSERT INTO USERS (user_login, user_password, user_salt) VALUES (@login, @password, @salt)", connection);
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", hashedPassword);
                        command.Parameters.AddWithValue("@salt", salt);
                        command.ExecuteNonQuery();

                        MessageBox.Show("Вы успешно зарегистрировались!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        connection.Close();
                        AuthWindow authWindow = new AuthWindow();
                        authWindow.Show();
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при регистрации аккаунта");
                }
            }
        }

        private void Button_Window_Auth_Click(object sender, RoutedEventArgs e)
        {
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            Close();
        }
    }
}
