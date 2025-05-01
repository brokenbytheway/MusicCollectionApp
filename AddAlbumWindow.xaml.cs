using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MusicCollectionApp
{
    public partial class AddAlbumWindow : Window
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private AlbumsUserControl parentControl;
        private string selectedImagePath = null;
        private List<int> selectedArtistIds = new List<int>();
        private List<CheckBox> allArtistCheckboxes = new List<CheckBox>();

        public AddAlbumWindow(AlbumsUserControl parent)
        {
            InitializeComponent();
            parentControl = parent;
            LoadUserId();
            LoadArtists();
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

        private void LoadArtists()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT artist_id, artist_nickname FROM ARTISTS WHERE user_id=@user_id", connection);
            command.Parameters.AddWithValue("@user_id", userId);
            MySqlDataReader reader = command.ExecuteReader();

            allArtistCheckboxes.Clear();
            artistsListBox.Items.Clear();

            List<CheckBox> checkBoxes = new List<CheckBox>();
            while (reader.Read())
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = reader["artist_nickname"].ToString(),
                    Tag = reader["artist_id"].ToString()
                };
                checkBox.Checked += ArtistCheckbox_Checked;
                checkBox.Unchecked += ArtistCheckbox_Unchecked;

                checkBoxes.Add(checkBox);
            }
            reader.Close();

            checkBoxes = checkBoxes.OrderBy(cb => cb.Content.ToString()).ToList(); // Сортируем в алфавитном порядке

            allArtistCheckboxes.AddRange(checkBoxes);
            foreach (var checkBox in checkBoxes)
            {
                artistsListBox.Items.Add(checkBox);
            }
        }

        private void ArtistCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                selectedArtistIds.Add(Convert.ToInt32(checkBox.Tag));
            }
        }

        private void ArtistCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                selectedArtistIds.Remove(Convert.ToInt32(checkBox.Tag));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();
            artistsListBox.Items.Clear();

            foreach (CheckBox checkBox in allArtistCheckboxes)
            {
                if (checkBox.Content.ToString().ToLower().Contains(searchText))
                {
                    artistsListBox.Items.Add(checkBox);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string albumTitle = albumTitleTextBox.Text;
            int releaseYear;

            if (string.IsNullOrWhiteSpace(albumTitle) || selectedArtistIds.Count == 0)
            {
                MessageBox.Show("Заполните все поля и выберите хотя бы одного исполнителя для добавления альбома!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(albumReleaseYearTextBox.Text, out releaseYear))
            {
                MessageBox.Show("Введите корректный год выпуска альбома!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedImagePath))
            {
                MessageBox.Show("Выберите обложку для альбома!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM ALBUMS WHERE album_title=@album_title AND user_id=@user_id", connection);
                checkCommand.Parameters.AddWithValue("@album_title", albumTitle);
                checkCommand.Parameters.AddWithValue("@user_id", userId);

                int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                if (count > 0)
                {
                    MessageBox.Show("Такой альбом уже существует в вашей коллекции!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MySqlCommand insertCommand = new MySqlCommand("INSERT INTO ALBUMS (album_title, album_release_year, path_to_album_cover, user_id) VALUES (@album_title, @album_release_year, @path_to_album_cover, @user_id)", connection);
                insertCommand.Parameters.AddWithValue("@album_title", albumTitle);
                insertCommand.Parameters.AddWithValue("@album_release_year", releaseYear);
                insertCommand.Parameters.AddWithValue("@path_to_album_cover", selectedImagePath);
                insertCommand.Parameters.AddWithValue("@user_id", userId);
                insertCommand.ExecuteNonQuery();

                int albumId = (int)insertCommand.LastInsertedId;
                foreach (int artistId in selectedArtistIds)
                {
                    MySqlCommand linkCommand = new MySqlCommand("INSERT INTO ALBUM_ARTISTS (album_id, artist_id) VALUES (@album_id, @artist_id)", connection);
                    linkCommand.Parameters.AddWithValue("@album_id", albumId);
                    linkCommand.Parameters.AddWithValue("@artist_id", artistId);
                    linkCommand.ExecuteNonQuery();
                }

                parentControl.RefreshAlbums();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при добавлении альбома");
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
                Filter = "Файлы изображений (*.jpg;*.jpeg;*.png;*.bmp) (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите обложку альбома"
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
                        albumCover.Source = null;
                        return;
                    }
                    albumCover.Source = new BitmapImage(new Uri(selectedImagePath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при загрузке изображения");
                }
            }
        }
    }
}
