using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MusicCollectionApp
{
    public partial class EditArtistWindow : Window
    {
        private ArtistsUserControl parentControl;
        private ArtistModel artist;
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private string selectedImagePath = null;

        public EditArtistWindow(ArtistsUserControl parent, ArtistModel artistToEdit)
        {
            InitializeComponent();
            parentControl = parent;
            artist = artistToEdit;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();

            // Заносим текущие данные в окно
            artistNicknameTextBox.Text = artist.Nickname;
            if (!string.IsNullOrEmpty(artist.PathToArtistPhoto))
            {
                try
                {
                    artistImage.Source = new BitmapImage(new Uri(artist.PathToArtistPhoto, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Файл с изображением был удалён или перемещён!");
                }
                selectedImagePath = artist.PathToArtistPhoto;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string newNickname = artistNicknameTextBox.Text;

            if (string.IsNullOrWhiteSpace(newNickname))
            {
                MessageBox.Show("Заполните все поля для изменения исполнителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedImagePath))
            {
                MessageBox.Show("Выберите изображение для исполнителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MySqlCommand command = new MySqlCommand("UPDATE ARTISTS SET artist_nickname=@new_nickname, path_to_artist_photo=@new_path_to_photo WHERE artist_id = @artist_id", connection);
                command.Parameters.AddWithValue("@new_nickname", newNickname);
                command.Parameters.AddWithValue("@new_path_to_photo", selectedImagePath);
                command.Parameters.AddWithValue("@artist_id", artist.Id);
                command.ExecuteNonQuery();

                parentControl.RefreshArtists();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при обновлении исполнителя");
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
                Filter = "Файлы изображений|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение исполнителя"
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
                        artistImage.Source = null;
                        return;
                    }
                    artistImage.Source = new BitmapImage(new Uri(selectedImagePath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при загрузке изображения");
                }
            }
        }
    }
}
