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
    public partial class AddTrackWindow : Window
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private TracksUserControl parentControl;
        private string selectedImagePath = null;
        private string selectedMp3Path = null;
        private List<int> selectedArtistIds = new List<int>();
        private int selectedGenreId = -1;
        private int selectedAlbumId = -1;
        private List<CheckBox> allArtistCheckboxes = new List<CheckBox>();
        private List<CheckBox> allGenreCheckboxes = new List<CheckBox>();
        private List<CheckBox> allAlbumCheckboxes = new List<CheckBox>();

        public AddTrackWindow(TracksUserControl parent)
        {
            InitializeComponent();
            parentControl = parent;
            LoadUserId();
            LoadGenres();
            LoadAlbums();
            LoadArtists();

            trackNoAlbumCheckBox.IsEnabled = false;
            trackReleaseYearTextBox.IsEnabled = false;
            selectImageButton.IsEnabled = false;
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

        private void LoadGenres()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT genre_id, genre_title FROM GENRES WHERE user_id=@user_id", connection);
            command.Parameters.AddWithValue("@user_id", userId);
            MySqlDataReader reader = command.ExecuteReader();

            allGenreCheckboxes.Clear();
            genresListBox.Items.Clear();

            List<CheckBox> checkBoxes = new List<CheckBox>();
            while (reader.Read())
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = reader["genre_title"].ToString(),
                    Tag = reader["genre_id"].ToString()
                };
                checkBox.Checked += GenreCheckbox_Checked;
                checkBox.Unchecked += GenreCheckbox_Unchecked;

                checkBoxes.Add(checkBox);
            }
            reader.Close();

            checkBoxes = checkBoxes.OrderBy(cb => cb.Content.ToString()).ToList(); // Сортируем в алфавитном порядке

            allGenreCheckboxes.AddRange(checkBoxes);
            foreach (var checkBox in checkBoxes)
            {
                genresListBox.Items.Add(checkBox);
            }
        }

        private void GenreCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (genresListBox.Items.Count > 1)
            {
                foreach (CheckBox elem in genresListBox.Items)
                {
                    if (elem.IsChecked == true && elem != (CheckBox)sender)
                    {
                        elem.IsChecked = false;
                    }
                }
            }

            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                selectedGenreId = Convert.ToInt32(checkBox.Tag);
            }
        }

        private void GenreCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Проверяем, остался ли какой-либо чекбокс с галочкой
            var stillChecked = allGenreCheckboxes.FirstOrDefault(cb => cb.IsChecked == true);
            if (stillChecked != null)
            {
                selectedGenreId = Convert.ToInt32(stillChecked.Tag);
            }
            else
            {
                selectedGenreId = -1; // Ничего не выбрано
            }
        }

        private void LoadAlbums()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT album_id, album_title FROM ALBUMS WHERE user_id=@user_id", connection);
            command.Parameters.AddWithValue("@user_id", userId);
            MySqlDataReader reader = command.ExecuteReader();

            allAlbumCheckboxes.Clear();
            albumsListBox.Items.Clear();

            List<CheckBox> checkBoxes = new List<CheckBox>();
            while (reader.Read())
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = reader["album_title"].ToString(),
                    Tag = reader["album_id"].ToString()
                };
                checkBox.Checked += AlbumCheckbox_Checked;
                checkBox.Unchecked += AlbumCheckbox_Unchecked;

                checkBoxes.Add(checkBox);
            }
            reader.Close();

            checkBoxes = checkBoxes.OrderBy(cb => cb.Content.ToString()).ToList(); // Сортируем в алфавитном порядке

            allAlbumCheckboxes.AddRange(checkBoxes);
            foreach (var checkBox in checkBoxes)
            {
                albumsListBox.Items.Add(checkBox);
            }
        }

        private void AlbumCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (albumsListBox.Items.Count > 1)
            {
                foreach (CheckBox elem in albumsListBox.Items)
                {
                    if (elem.IsChecked == true && elem != (CheckBox)sender)
                    {
                        elem.IsChecked = false;
                    }
                }
            }

            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag != null)
            {
                selectedAlbumId = Convert.ToInt32(checkBox.Tag);
            }
        }

        private void AlbumCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Проверяем, остался ли какой-либо чекбокс с галочкой
            var stillChecked = allAlbumCheckboxes.FirstOrDefault(cb => cb.IsChecked == true);
            if (stillChecked != null)
            {
                selectedAlbumId = Convert.ToInt32(stillChecked.Tag);
            }
            else
            {
                selectedAlbumId = -1; // Ничего не выбрано
            }
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

        private void SearchArtistsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchArtistsTextBox.Text.ToLower();
            artistsListBox.Items.Clear();

            foreach (CheckBox checkBox in allArtistCheckboxes)
            {
                if (checkBox.Content.ToString().ToLower().Contains(searchText))
                {
                    artistsListBox.Items.Add(checkBox);
                }
            }
        }

        private void SearchAlbumsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchAlbumsTextBox.Text.ToLower();
            albumsListBox.Items.Clear();

            foreach (CheckBox checkBox in allAlbumCheckboxes)
            {
                if (checkBox.Content.ToString().ToLower().Contains(searchText))
                {
                    albumsListBox.Items.Add(checkBox);
                }
            }
        }

        private void SearchGenresTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchGenresTextBox.Text.ToLower();
            genresListBox.Items.Clear();

            foreach (CheckBox checkBox in allGenreCheckboxes)
            {
                if (checkBox.Content.ToString().ToLower().Contains(searchText))
                {
                    genresListBox.Items.Add(checkBox);
                }
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Файлы изображений (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите обложку трека"
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
                        trackCover.Source = null;
                        return;
                    }
                    trackCover.Source = new BitmapImage(new Uri(selectedImagePath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка при загрузке изображения");
                }
            }
        }

        private void SelectMp3_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3-файлы (*.mp3)|*.mp3",
                Title = "Выберите MP3-файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedMp3Path = openFileDialog.FileName;
                mp3PathTextBlock.Text = System.IO.Path.GetFileName(selectedMp3Path);
            }
        }

        private void TrackSingleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            trackNoAlbumCheckBox.IsEnabled = true;
            trackReleaseYearTextBox.IsEnabled = true;
            selectImageButton.IsEnabled = true;
        }

        private void TrackSingleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            trackNoAlbumCheckBox.IsEnabled = false;
            trackReleaseYearTextBox.IsEnabled = false;
            selectImageButton.IsEnabled = false;

            // Сброс значений
            trackNoAlbumCheckBox.IsChecked = false;
            trackReleaseYearTextBox.Text = string.Empty;
            selectedImagePath = null;
            imagePathTextBlock.Text = string.Empty;
            trackCover.Source = null;
        }

        private void TrackNoAlbumCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            albumsListBox.IsEnabled = false;
            trackNumberInAlbumTextBox.IsEnabled = false;

            // Сброс значений
            trackNumberInAlbumTextBox.Text = string.Empty;
            selectedAlbumId = -1;
            foreach (CheckBox cb in allAlbumCheckboxes)
            {
                cb.IsChecked = false;
            }
        }

        private void TrackNoAlbumCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            albumsListBox.IsEnabled = true;
            trackNumberInAlbumTextBox.IsEnabled = true;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string trackTitle = trackTitleTextBox.Text;
            int releaseYear;
            int trackNumber;
            bool isSingle = trackSingleCheckBox.IsChecked == true;
            bool noAlbum = trackNoAlbumCheckBox.IsChecked == true;

            if (string.IsNullOrWhiteSpace(trackTitle))
            {
                MessageBox.Show("Введите название трека!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedGenreId == -1)
            {
                MessageBox.Show("Выберите жанр трека!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedAlbumId == -1 && !noAlbum)
            {
                MessageBox.Show("Выберите альбом для трека!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedArtistIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного исполнителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(trackNumberInAlbumTextBox.Text, out trackNumber) && !isSingle)
            {
                MessageBox.Show("Введите корректный номер трека в альбоме!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(trackReleaseYearTextBox.Text, out releaseYear) && isSingle)
            {
                MessageBox.Show("Введите корректный год выпуска трека!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (isSingle && string.IsNullOrWhiteSpace(selectedImagePath))
            {
                MessageBox.Show("Выберите обложку для сингла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedMp3Path))
            {
                MessageBox.Show("Выберите MP3-файл!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (isSingle && noAlbum)
                {
                    MySqlCommand singleCommand = new MySqlCommand("INSERT INTO ALBUMS (album_title, album_release_year, path_to_album_cover, user_id) VALUES (@album_title, @album_release_year, @path_to_album_cover, @user_id)", connection);
                    singleCommand.Parameters.AddWithValue("@album_title", trackTitle);
                    singleCommand.Parameters.AddWithValue("@album_release_year", releaseYear);
                    singleCommand.Parameters.AddWithValue("@path_to_album_cover", selectedImagePath);
                    singleCommand.Parameters.AddWithValue("@user_id", userId);
                    singleCommand.ExecuteNonQuery();

                    int albumId = (int)singleCommand.LastInsertedId;
                    foreach (int artistId in selectedArtistIds)
                    {
                        MySqlCommand linkCommand = new MySqlCommand("INSERT INTO ALBUM_ARTISTS (album_id, artist_id) VALUES (@album_id, @artist_id)", connection);
                        linkCommand.Parameters.AddWithValue("@album_id", albumId);
                        linkCommand.Parameters.AddWithValue("@artist_id", artistId);
                        linkCommand.ExecuteNonQuery();
                    }

                    MySqlCommand singleInsertCommand = new MySqlCommand("INSERT INTO TRACKS (track_title, track_release_year, path_to_track_cover, album_id, number_in_album, genre_id, path_to_track_mp3_file, is_single) VALUES (@track_title, @track_release_year, @path_to_track_cover, @album_id, @number_in_album, @genre_id, @path_to_track_mp3_file, @is_single)", connection);
                    singleInsertCommand.Parameters.AddWithValue("@track_title", trackTitle);
                    singleInsertCommand.Parameters.AddWithValue("@track_release_year", releaseYear);
                    singleInsertCommand.Parameters.AddWithValue("@path_to_track_cover", selectedImagePath);
                    singleInsertCommand.Parameters.AddWithValue("@album_id", albumId);
                    singleInsertCommand.Parameters.AddWithValue("@number_in_album", 1);
                    singleInsertCommand.Parameters.AddWithValue("@genre_id", selectedGenreId);
                    singleInsertCommand.Parameters.AddWithValue("@path_to_track_mp3_file", selectedMp3Path);
                    singleInsertCommand.Parameters.AddWithValue("@is_single", isSingle);
                    singleInsertCommand.ExecuteNonQuery();

                    int singleTrackId = (int)singleInsertCommand.LastInsertedId;
                    foreach (int artistId in selectedArtistIds)
                    {
                        MySqlCommand linkCommand = new MySqlCommand("INSERT INTO TRACK_ARTISTS (track_id, artist_id) VALUES (@track_id, @artist_id)", connection);
                        linkCommand.Parameters.AddWithValue("@track_id", singleTrackId);
                        linkCommand.Parameters.AddWithValue("@artist_id", artistId);
                        linkCommand.ExecuteNonQuery();
                    }
                }

                else if (isSingle && !noAlbum)
                {
                    MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM TRACKS WHERE track_title=@track_title AND album_id=@album_id", connection);
                    checkCommand.Parameters.AddWithValue("@track_title", trackTitle);
                    checkCommand.Parameters.AddWithValue("@album_id", selectedAlbumId);

                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count > 0)
                    {
                        MessageBox.Show("Такой трек уже существует в этом альбоме!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MySqlCommand insertCommand = new MySqlCommand("INSERT INTO TRACKS (track_title, track_release_year, path_to_track_cover, album_id, number_in_album, genre_id, path_to_track_mp3_file, is_single) VALUES (@track_title, @track_release_year, @path_to_track_cover, @album_id, @number_in_album, @genre_id, @path_to_track_mp3_file, @is_single)", connection);
                    insertCommand.Parameters.AddWithValue("@track_title", trackTitle);
                    insertCommand.Parameters.AddWithValue("@track_release_year", releaseYear);
                    insertCommand.Parameters.AddWithValue("@path_to_track_cover", selectedImagePath);
                    insertCommand.Parameters.AddWithValue("@album_id", selectedAlbumId);
                    insertCommand.Parameters.AddWithValue("@number_in_album", trackNumber);
                    insertCommand.Parameters.AddWithValue("@genre_id", selectedGenreId);
                    insertCommand.Parameters.AddWithValue("@path_to_track_mp3_file", selectedMp3Path);
                    insertCommand.Parameters.AddWithValue("@is_single", isSingle);
                    insertCommand.ExecuteNonQuery();

                    int trackId = (int)insertCommand.LastInsertedId;
                    foreach (int artistId in selectedArtistIds)
                    {
                        MySqlCommand linkCommand = new MySqlCommand("INSERT INTO TRACK_ARTISTS (track_id, artist_id) VALUES (@track_id, @artist_id)", connection);
                        linkCommand.Parameters.AddWithValue("@track_id", trackId);
                        linkCommand.Parameters.AddWithValue("@artist_id", artistId);
                        linkCommand.ExecuteNonQuery();
                    }
                }

                else
                {
                    MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM TRACKS WHERE track_title=@track_title AND album_id=@album_id", connection);
                    checkCommand.Parameters.AddWithValue("@track_title", trackTitle);
                    checkCommand.Parameters.AddWithValue("@album_id", selectedAlbumId);

                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count > 0)
                    {
                        MessageBox.Show("Такой трек уже существует в этом альбоме!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MySqlCommand albumCoverCommand = new MySqlCommand("SELECT path_to_album_cover FROM ALBUMS WHERE album_id=@album_id", connection);
                    albumCoverCommand.Parameters.AddWithValue("@album_id", selectedAlbumId);
                    string albumCover = Convert.ToString(albumCoverCommand.ExecuteScalar());

                    MySqlCommand albumReleaseYearCommand = new MySqlCommand("SELECT album_release_year FROM ALBUMS WHERE album_id=@album_id", connection);
                    albumReleaseYearCommand.Parameters.AddWithValue("@album_id", selectedAlbumId);
                    releaseYear = Convert.ToInt32(albumReleaseYearCommand.ExecuteScalar());

                    MySqlCommand insertCommand = new MySqlCommand("INSERT INTO TRACKS (track_title, track_release_year, path_to_track_cover, album_id, number_in_album, genre_id, path_to_track_mp3_file, is_single) VALUES (@track_title, @track_release_year, @path_to_track_cover, @album_id, @number_in_album, @genre_id, @path_to_track_mp3_file, @is_single)", connection);
                    insertCommand.Parameters.AddWithValue("@track_title", trackTitle);
                    insertCommand.Parameters.AddWithValue("@track_release_year", releaseYear);
                    insertCommand.Parameters.AddWithValue("@path_to_track_cover", albumCover);
                    insertCommand.Parameters.AddWithValue("@album_id", selectedAlbumId);
                    insertCommand.Parameters.AddWithValue("@number_in_album", trackNumber);
                    insertCommand.Parameters.AddWithValue("@genre_id", selectedGenreId);
                    insertCommand.Parameters.AddWithValue("@path_to_track_mp3_file", selectedMp3Path);
                    insertCommand.Parameters.AddWithValue("@is_single", isSingle);
                    insertCommand.ExecuteNonQuery();

                    int trackId = (int)insertCommand.LastInsertedId;
                    foreach (int artistId in selectedArtistIds)
                    {
                        MySqlCommand linkCommand = new MySqlCommand("INSERT INTO TRACK_ARTISTS (track_id, artist_id) VALUES (@track_id, @artist_id)", connection);
                        linkCommand.Parameters.AddWithValue("@track_id", trackId);
                        linkCommand.Parameters.AddWithValue("@artist_id", artistId);
                        linkCommand.ExecuteNonQuery();
                    }
                }

                parentControl.RefreshTracks();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при добавлении трека");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void genresListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (genresListBox.SelectedItems.Count > 1)
            {
                var lastSelected = genresListBox.SelectedItems[genresListBox.SelectedItems.Count - 1];
                genresListBox.SelectedItems.Clear();
                genresListBox.SelectedItem = lastSelected;
            }
        }

        private void albumsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (albumsListBox.SelectedItems.Count > 1)
            {
                var lastSelected = albumsListBox.SelectedItems[albumsListBox.SelectedItems.Count - 1];
                albumsListBox.SelectedItems.Clear();
                albumsListBox.SelectedItem = lastSelected;
            }
        }
    }
}
