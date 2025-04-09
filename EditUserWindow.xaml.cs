using MySql.Data.MySqlClient;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Media;

namespace MusicCollectionApp
{
    public partial class EditUserWindow : Window
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private SettingsUserControl parentControl;
        private string currentLogin;

        public EditUserWindow(SettingsUserControl parent, string login)
        {
            InitializeComponent();
            parentControl = parent;
            currentLogin = login;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();

            textBoxLogin.Text = login; // Отображаем текущий логин
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

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string newLogin = textBoxLogin.Text.Trim();
            string newPassword = passBox.Password.Trim();
            string confirmPassword = passBox2.Password.Trim();

            if (newLogin.Length < 5)
            {
                textBoxLogin.ToolTip = "Логин должен состоять минимум из 5 символов!";
                textBoxLogin.Background = Brushes.LightPink;
                return;
            }

            if (!IsValidPassword(newPassword))
            {
                passBox.ToolTip = "Пароль должен состоять минимум из 8 символов, включая заглавную букву, цифру и спецсимвол!";
                passBox.Background = Brushes.LightPink;
                return;
            }

            else if (newPassword != confirmPassword)
            {
                passBox2.ToolTip = "Пароли не совпадают!";
                passBox2.Background = Brushes.LightPink;
                return;
            }

            textBoxLogin.ToolTip = null;
            textBoxLogin.Background = Brushes.Transparent;
            passBox.ToolTip = null;
            passBox.Background = Brushes.Transparent;
            passBox2.ToolTip = null;
            passBox2.Background = Brushes.Transparent;

            // Хэширование введённого пароля с солью
            string salt = GenerateSalt();
            string hashedPassword = GenerateHash(newPassword, salt);
            newPassword = hashedPassword;

            try
            {
                MySqlCommand checkUserCommand = new MySqlCommand("SELECT COUNT(*) FROM USERS WHERE user_login = @login", connection);
                checkUserCommand.Parameters.AddWithValue("@login", newLogin);

                int userCount = Convert.ToInt32(checkUserCommand.ExecuteScalar());
                if (userCount > 0)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует! Пожалуйста, придумайте другой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                else
                {
                    MySqlCommand command = new MySqlCommand("UPDATE USERS SET user_login=@login, user_password=@password, user_salt=@salt WHERE user_login=@currentLogin", connection);
                    command.Parameters.AddWithValue("@login", newLogin);
                    command.Parameters.AddWithValue("@password", newPassword);
                    command.Parameters.AddWithValue("@salt", salt);
                    command.Parameters.AddWithValue("@currentLogin", currentLogin);
                    command.ExecuteNonQuery();

                    // Обновляем данные в окне авторизации
                    AuthWindow.login = newLogin;
                    AuthWindow.password = newPassword;

                    // Обновляем данные в UserControl
                    parentControl.Login = newLogin;

                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при обновлении пользователя");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
