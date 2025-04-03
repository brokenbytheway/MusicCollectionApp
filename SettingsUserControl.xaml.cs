using MySql.Data.MySqlClient;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MusicCollectionApp
{
    public partial class SettingsUserControl : UserControl, INotifyPropertyChanged
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private string _login;

        public string Login
        {
            get => _login;
            set
            {
                if (_login != value)
                {
                    _login = value;
                    OnPropertyChanged(nameof(Login)); // Уведомляем о смене логина
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SettingsUserControl()
        {
            InitializeComponent();
            DataContext = this;
            Login = AuthWindow.login;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            EditUserWindow editUserWindow = new EditUserWindow(this, AuthWindow.login);
            editUserWindow.ShowDialog();
        }

        private void ChangeAccount_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите сменить аккаунт?", "Смена аккаунта", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                AuthWindow authWindow = new AuthWindow();
                authWindow.Show();
                Window.GetWindow(this)?.Close();
            }
        }
    }
}
