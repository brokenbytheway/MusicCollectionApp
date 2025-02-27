using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace MusicCollectionApp
{
    public partial class AuthWindow : Window
    {
        public static string login, password;
        MySqlConnection connection = new MySqlConnection("Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;");

        public AuthWindow()
        {
            InitializeComponent();
        }

        private void Button_Auth_Click(object sender, RoutedEventArgs e)
        {
            login = textBoxLogin.Text.Trim();
            password = passBox.Password.Trim();

            if (login.Length < 5)
            {
                textBoxLogin.ToolTip = "Логин должен состоять минимум из 5 символов!";
                textBoxLogin.Background = Brushes.LightPink;
            }

            else if (password.Length < 5)
            {
                passBox.ToolTip = "Пароль должен состоять минимум из 5 символов!";
                passBox.Background = Brushes.LightPink;
            }

            else
            {
                textBoxLogin.ToolTip = "";
                textBoxLogin.Background = Brushes.Transparent;
                passBox.ToolTip = "";
                passBox.Background = Brushes.Transparent;

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
                        MessageBox.Show("Вы успешно вошли в аккаунт!");
                        connection.Close();
                        HomePageWindow homePage = new HomePageWindow();
                        homePage.Show();
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
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
