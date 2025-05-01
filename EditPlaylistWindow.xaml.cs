using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MusicCollectionApp
{
    public partial class EditPlaylistWindow : Window
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private PlaylistsUserControl parentControl;
        private PlaylistModel playlist;
        private string selectedImagePath = null;

        public EditPlaylistWindow(PlaylistsUserControl parent, PlaylistModel playlistToEdit)
        {
            InitializeComponent();
            parentControl = parent;
            playlist = playlistToEdit;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();

            // Заносим текущие данные в окно
            playlistTitleTextBox.Text = playlist.Title;
            playlistDescriptionTextBox.Text = playlist.Description;
            if (!string.IsNullOrEmpty(playlist.PathToPlaylistCover))
            {
                try
                {
                    playlistCover.Source = new BitmapImage(new Uri(playlist.PathToPlaylistCover, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Файл с изображением был удалён или перемещён!");
                }
                selectedImagePath = playlist.PathToPlaylistCover;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string newTitle = playlistTitleTextBox.Text;
            string newDescription = playlistDescriptionTextBox.Text;

            if (string.IsNullOrWhiteSpace(newTitle) || string.IsNullOrWhiteSpace(newDescription))
            {
                MessageBox.Show("Заполните все поля для изменения плейлиста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedImagePath))
            {
                MessageBox.Show("Выберите обложку для плейлиста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MySqlCommand command = new MySqlCommand("UPDATE PLAYLISTS SET playlist_title=@new_title, playlist_description=@new_description, path_to_playlist_cover=@new_path_to_cover WHERE playlist_id=@playlist_id", connection);
                command.Parameters.AddWithValue("@new_title", newTitle);
                command.Parameters.AddWithValue("@new_description", newDescription);
                command.Parameters.AddWithValue("@new_path_to_cover", selectedImagePath);
                command.Parameters.AddWithValue("@playlist_id", playlist.Id);
                command.ExecuteNonQuery();

                parentControl.RefreshPlaylists();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при обновлении плейлиста");
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
                        MessageBox.Show("Изображение не является квадратным! Пожалуйста, выберите другое.");
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
