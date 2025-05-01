using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MusicCollectionApp
{
    public partial class AddPlaylistWindow : Window
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private PlaylistsUserControl parentControl;
        private string selectedImagePath = null;

        public AddPlaylistWindow(PlaylistsUserControl parent)
        {
            InitializeComponent();
            parentControl = parent;
            LoadUserId();
        }

        public AddPlaylistWindow(PlaylistsUserControl parent, string title) : this(parent)
        {
            playlistTitleTextBox.Text = title;
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
            string playlistTitle = playlistTitleTextBox.Text;
            string playlistDescription = playlistDescriptionTextBox.Text;

            if (string.IsNullOrWhiteSpace(playlistTitle) || string.IsNullOrWhiteSpace(playlistDescription))
            {
                MessageBox.Show("Заполните все поля для добавления плейлиста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedImagePath))
            {
                MessageBox.Show("Выберите обложку для плейлиста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM PLAYLISTS WHERE playlist_title=@playlist_title AND user_id=@user_id", connection);
                checkCommand.Parameters.AddWithValue("@playlist_title", playlistTitle);
                checkCommand.Parameters.AddWithValue("@user_id", userId);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    MessageBox.Show("Такой плейлист уже существует в вашей коллекции!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MySqlCommand insertCommand = new MySqlCommand("INSERT INTO PLAYLISTS (playlist_title, playlist_description, path_to_playlist_cover, user_id) VALUES (@playlist_title, @playlist_description, @path_to_playlist_cover, @user_id)", connection);
                insertCommand.Parameters.AddWithValue("@playlist_title", playlistTitle);
                insertCommand.Parameters.AddWithValue("@playlist_description", playlistDescription);
                insertCommand.Parameters.AddWithValue("@path_to_playlist_cover", selectedImagePath);
                insertCommand.Parameters.AddWithValue("@user_id", userId);
                insertCommand.ExecuteNonQuery();

                parentControl.RefreshPlaylists();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при добавлении плейлиста");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Файлы изображений (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите обложку плейлиста"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedImagePath = openFileDialog.FileName;
                imagePathTextBlock.Text = System.IO.Path.GetFileName(selectedImagePath);

                try
                {
                    Bitmap originalImage = new Bitmap(selectedImagePath);
                    int width = originalImage.Width;
                    int height = originalImage.Height;

                    if (width != height)
                    {
                        MessageBox.Show("Изображение не является квадратным! Пожалуйста, выберите другое.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        selectedImagePath = null;
                        imagePathTextBlock.Text = string.Empty;
                        playlistCover.Source = null;
                        return;
                    }
                    playlistCover.Source = new BitmapImage(new Uri(selectedImagePath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при загрузке изображения");
                }
            }
        }
    }
}
