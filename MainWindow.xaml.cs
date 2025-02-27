using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;

namespace MusicCollectionApp
{
    public partial class MainWindow : Window
    {
        MySqlConnection connection = new MySqlConnection("Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;");
        public MainWindow()
        {
            InitializeComponent();
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
            }

            else if (password.Length < 5)
            {
                passBox.ToolTip = "Пароль должен состоять минимум из 5 символов!";
                passBox.Background = Brushes.LightPink;
            }

            else if (password != password2)
            {
                passBox2.ToolTip = "Пароли не совпадают!";
                passBox2.Background = Brushes.LightPink;
            }
            else
            {
                textBoxLogin.ToolTip = "";
                textBoxLogin.Background = Brushes.Transparent;
                passBox.ToolTip = "";
                passBox.Background = Brushes.Transparent;
                passBox2.ToolTip = "";
                passBox2.Background = Brushes.Transparent;

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
                        MessageBox.Show("Пользователь с таким логином уже существует!");
                    }
                    else
                    {
                        MySqlCommand command = new MySqlCommand("INSERT INTO USERS (user_login, user_password) VALUES (@login, @password)", connection);
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        command.ExecuteNonQuery();
                        connection.Close();
                        MessageBox.Show("Вы успешно зарегистрировались!");
                        AuthWindow authWindow = new AuthWindow();
                        authWindow.Show();
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
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
