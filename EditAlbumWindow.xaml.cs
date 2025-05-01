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
    public partial class EditAlbumWindow : Window
    {
        private AlbumsUserControl parentControl;
        private AlbumModel album;
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private string selectedImagePath = null;
        private List<int> selectedArtistIds = new List<int>();
        private List<CheckBox> allArtistCheckboxes = new List<CheckBox>();

        public EditAlbumWindow(AlbumsUserControl parent, AlbumModel albumToEdit)
        {
            InitializeComponent();
            parentControl = parent;
            album = albumToEdit;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();

            // Заносим текущие данные в окно
            albumTitleTextBox.Text = album.Title;
            albumReleaseYearTextBox.Text = album.ReleaseYear.ToString();
            if (!string.IsNullOrEmpty(album.PathToAlbumCover))
            {
                try
                {
                    albumCover.Source = new BitmapImage(new Uri(album.PathToAlbumCover, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Файл с изображением был удалён или перемещён!");
                }
                selectedImagePath = album.PathToAlbumCover;
            }

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
            List<int> currentAlbumArtists = GetAlbumArtists();
            selectedArtistIds.Clear();

            // Загрузка списка исполнителей и отметка уже связанных с альбомом
            MySqlCommand command = new MySqlCommand("SELECT artist_id, artist_nickname FROM ARTISTS WHERE user_id=@user_id", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                allArtistCheckboxes.Clear();
                artistsListBox.Items.Clear();

                while (reader.Read())
                {
                    int artistId = Convert.ToInt32(reader["artist_id"]);
                    CheckBox checkBox = new CheckBox
                    {
                        Content = reader["artist_nickname"].ToString(),
                        Tag = artistId,
                        IsChecked = currentAlbumArtists.Contains(artistId)
                    };

                    checkBox.Checked += ArtistCheckbox_Checked;
                    checkBox.Unchecked += ArtistCheckbox_Unchecked;

                    allArtistCheckboxes.Add(checkBox);
                    if (checkBox.IsChecked == true)
                    {
                        selectedArtistIds.Add(artistId);
                    }
                }
            }

            foreach (var checkBox in allArtistCheckboxes.OrderBy(cb => cb.Content.ToString())) // Сортируем в алфавитном порядке
            {
                artistsListBox.Items.Add(checkBox);
            }
        }

        private List<int> GetAlbumArtists()
        {
            List<int> artistIds = new List<int>();
            MySqlCommand command = new MySqlCommand("SELECT artist_id FROM ALBUM_ARTISTS WHERE album_id=@album_id", connection);
            command.Parameters.AddWithValue("@album_id", album.Id);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    artistIds.Add(Convert.ToInt32(reader["artist_id"]));
                }
            }
            return artistIds;
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
            string newTitle = albumTitleTextBox.Text;
            int releaseYear;

            if (string.IsNullOrWhiteSpace(newTitle) || selectedArtistIds.Count == 0)
            {
                MessageBox.Show("Заполните все поля и выберите хотя бы одного исполнителя для изменения альбома!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // Обновляем альбом
                MySqlCommand updateCommand = new MySqlCommand("UPDATE ALBUMS SET album_title=@new_title, album_release_year=@new_release_year, path_to_album_cover=@new_path_to_cover WHERE album_id=@album_id", connection);
                updateCommand.Parameters.AddWithValue("@new_title", newTitle);
                updateCommand.Parameters.AddWithValue("@new_release_year", releaseYear);
                updateCommand.Parameters.AddWithValue("@new_path_to_cover", selectedImagePath);
                updateCommand.Parameters.AddWithValue("@album_id", album.Id);
                updateCommand.ExecuteNonQuery();

                // Обновляем связи с исполнителями
                MySqlCommand deleteArtistsCommand = new MySqlCommand("DELETE FROM ALBUM_ARTISTS WHERE album_id=@album_id", connection);
                deleteArtistsCommand.Parameters.AddWithValue("@album_id", album.Id);
                deleteArtistsCommand.ExecuteNonQuery();

                foreach (int artistId in selectedArtistIds)
                {
                    MySqlCommand insertArtistCommand = new MySqlCommand("INSERT INTO ALBUM_ARTISTS (album_id, artist_id) VALUES (@album_id, @artist_id)", connection);
                    insertArtistCommand.Parameters.AddWithValue("@album_id", album.Id);
                    insertArtistCommand.Parameters.AddWithValue("@artist_id", artistId);
                    insertArtistCommand.ExecuteNonQuery();
                }

                parentControl.RefreshAlbums();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при обновлении альбома");
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
                        MessageBox.Show("Изображение не является квадратным! Выберите другое.");
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
