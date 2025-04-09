using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace MusicCollectionApp
{
    public partial class PlaylistsUserControl : UserControl
    {
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private int userId;
        private ObservableCollection<PlaylistModel> _playlists;
        public ObservableCollection<PlaylistModel> Playlists => _playlists;
        private List<PlaylistModel> _allPlaylists; // Хранит все плейлисты для поиска
        public event Action<PlaylistModel> PlaylistSelected;

        public PlaylistsUserControl()
        {
            InitializeComponent();
            DataContext = this;
            _playlists = new ObservableCollection<PlaylistModel>();
            LoadUserId();
            LoadPlaylists();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlaylistModel playlist)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить плейлист '{playlist.Title}'?", "Удаление плейлиста", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.Open();

                        using (MySqlCommand deletePlaylistCommand = new MySqlCommand("DELETE FROM PLAYLISTS WHERE playlist_id=@playlist_id AND user_id=@user_id", connection))
                        {
                            deletePlaylistCommand.Parameters.AddWithValue("@playlist_id", playlist.Id);
                            deletePlaylistCommand.Parameters.AddWithValue("@user_id", userId);
                            deletePlaylistCommand.ExecuteNonQuery();
                        }

                        _playlists.Remove(playlist);
                        _allPlaylists.Remove(playlist);
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при удалении плейлиста");
                    }
                }
            }
        }

        private void EditPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlaylistModel playlist)
            {
                EditPlaylistWindow editPlaylistWindow = new EditPlaylistWindow(this, playlist);
                editPlaylistWindow.ShowDialog();
            }
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

        private void LoadPlaylists()
        {
            _playlists.Clear();
            _allPlaylists = new List<PlaylistModel>();

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            MySqlCommand command = new MySqlCommand("SELECT playlist_id, playlist_title, playlist_description, path_to_playlist_cover FROM PLAYLISTS WHERE user_id=@user_id ORDER BY playlist_title", connection);
            command.Parameters.AddWithValue("@user_id", userId);

            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int playlistId = reader.GetInt32("playlist_id");
                    string title = reader.GetString("playlist_title");
                    string description = reader.GetString("playlist_description");
                    string coverPath = reader.IsDBNull(reader.GetOrdinal("path_to_playlist_cover")) ? "" : reader.GetString("path_to_playlist_cover");

                    _playlists.Add(new PlaylistModel(playlistId, title, description, coverPath));
                    _allPlaylists.Add(new PlaylistModel(playlistId, title, description, coverPath));
                }
            }
            connection.Close();
        }

        public void RefreshPlaylists()
        {
            LoadPlaylists();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddPlaylistWindow addPlaylistWindow = new AddPlaylistWindow(this);
            addPlaylistWindow.ShowDialog();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            _playlists.Clear(); // Очищаем коллекцию

            var filteredPlaylists = string.IsNullOrWhiteSpace(searchText)
                ? _allPlaylists // Если строка пустая — вернуть все плейлисты
                : _allPlaylists.Where(playlist => playlist.Title.ToLower().Contains(searchText)).ToList();

            foreach (var playlist in filteredPlaylists)
            {
                _playlists.Add(playlist);
            }
        }

        private void Playlist_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is PlaylistModel playlist)
            {
                PlaylistSelected?.Invoke(playlist);
            }
        }

        private void ExportM3U_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is PlaylistModel playlist)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "M3U-файл|*.m3u",
                    FileName = playlist.Title + ".m3u"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        connection.Open();
                        MySqlCommand command = new MySqlCommand(@"SELECT path_to_track_mp3_file FROM TRACKS t JOIN PLAYLIST_TRACKS pt ON pt.track_id=t.track_id WHERE pt.playlist_id=@playlistId ORDER BY pt.number_in_playlist", connection);
                        command.Parameters.AddWithValue("@playlistId", playlist.Id);

                        List<string> trackPaths = new List<string>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                                trackPaths.Add(reader.GetString(0));
                        }

                        connection.Close();
                        File.WriteAllLines(saveFileDialog.FileName, trackPaths);

                        MessageBox.Show("Плейлист успешно экспортирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при экспорте плейлиста");
                    }
                }
            }
        }

        private void ImportM3U_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult warning = MessageBox.Show($"Внимание!\n\nПри импорте плейлиста перенесутся только те треки, которые уже есть в вашей коллекции.\n\nТреки, которых нет в вашей коллекции, не будут перенесены.\n\nВы уверены, что хотите продолжить?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (warning == MessageBoxResult.Yes)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "M3U-файл|*.m3u",
                    Title = "Выберите M3U-файл для импорта"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    var addWindow = new AddPlaylistWindow(this, fileName);
                    bool? dialogResult = addWindow.ShowDialog();

                    int newPlaylistId = -1;
                    string playlistTitle = addWindow.playlistTitleTextBox?.Text?.Trim();

                    if (dialogResult == true && !string.IsNullOrEmpty(playlistTitle))
                    {
                        try
                        {
                            connection.Open();

                            // Получаем ID только что добавленного плейлиста
                            MySqlCommand command = new MySqlCommand("SELECT playlist_id FROM PLAYLISTS WHERE user_id=@user_id AND playlist_title=@title ORDER BY playlist_id DESC LIMIT 1", connection);
                            command.Parameters.AddWithValue("@user_id", userId);
                            command.Parameters.AddWithValue("@title", playlistTitle);
                            var result = command.ExecuteScalar();

                            if (result != null)
                            {
                                newPlaylistId = Convert.ToInt32(result);
                            }
                            else
                            {
                                MessageBox.Show("Не удалось найти добавленный плейлист.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Читаем и добавляем треки
                            var lines = File.ReadAllLines(openFileDialog.FileName);
                            int order = 1;

                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                                    continue;

                                MySqlCommand findTrackCommand = new MySqlCommand("SELECT track_id FROM TRACKS WHERE path_to_track_mp3_file=@path", connection);
                                findTrackCommand.Parameters.AddWithValue("@path", line);
                                var trackResult = findTrackCommand.ExecuteScalar();

                                if (trackResult != null)
                                {
                                    int trackId = Convert.ToInt32(trackResult);

                                    MySqlCommand insertCommand = new MySqlCommand("INSERT INTO PLAYLIST_TRACKS (playlist_id, track_id, number_in_playlist, last_modified_time) VALUES (@playlist_id, @track_id, @number, @modified)", connection);
                                    {
                                        insertCommand.Parameters.AddWithValue("@playlist_id", newPlaylistId);
                                        insertCommand.Parameters.AddWithValue("@track_id", trackId);
                                        insertCommand.Parameters.AddWithValue("@number", order++);
                                        insertCommand.Parameters.AddWithValue("@modified", DateTime.Now);
                                        insertCommand.ExecuteNonQuery();
                                    }
                                }
                            }

                            MessageBox.Show("Плейлист успешно импортирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshPlaylists();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Ошибка при импорте плейлиста");
                        }
                    }
                }
            }
        }

        private void CreateReport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is PlaylistModel playlist)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Документ Word|*.docx",
                    FileName = $"{playlist.Title}_отчет.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        // Создаем документ Word
                        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(saveFileDialog.FileName, WordprocessingDocumentType.Document))
                        {
                            // Добавляем основную часть документа
                            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                            mainPart.Document = new Document();
                            Body body = mainPart.Document.AppendChild(new Body());

                            // Добавляем заголовок
                            Paragraph titlePar = body.AppendChild(new Paragraph());
                            Run titleRun = titlePar.AppendChild(new Run());
                            titleRun.AppendChild(new Text($"Отчет по плейлисту {playlist.Title}"));
                            titleRun.RunProperties = new RunProperties(new Bold());
                            titlePar.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new SpacingBetweenLines() { After = "200" });

                            // Добавляем информацию о плейлисте
                            AddPlaylistInfo(body, playlist);

                            // Добавляем таблицу с треками
                            AddTracksTable(body, playlist);

                            // Сохраняем документ
                            mainPart.Document.Save();
                        }

                        MessageBox.Show("Отчет успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при создании отчета");
                    }
                }
            }
        }

        private void AddPlaylistInfo(Body body, PlaylistModel playlist)
        {
            // Параграф с информацией о плейлисте
            Paragraph infoPar = body.AppendChild(new Paragraph());
            infoPar.AppendChild(new Run(new Text($"Название: {playlist.Title}")));
            infoPar.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "100" });

            // Описание плейлиста
            Paragraph descPar = body.AppendChild(new Paragraph());
            descPar.AppendChild(new Run(new Text($"Описание: {playlist.Description}")));
            descPar.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "100" });

            // Путь к обложке
            Paragraph coverPar = body.AppendChild(new Paragraph());
            coverPar.AppendChild(new Run(new Text($"Путь к обложке: {playlist.PathToPlaylistCover}")));
            coverPar.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "200" });
        }

        private void AddTracksTable(Body body, PlaylistModel playlist)
        {
            // Получаем данные о треках из базы данных
            List<TrackModel> tracks = GetPlaylistTracks(playlist.Id);

            // Проверяем, есть ли треки в плейлисте
            if (tracks.Count == 0)
            {
                Paragraph noTracksPar = body.AppendChild(new Paragraph());
                noTracksPar.AppendChild(new Run(new Text("В плейлисте нет треков.")));
                noTracksPar.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "200" });
                return;
            }

            // Заголовок таблицы
            Paragraph tableTitle = body.AppendChild(new Paragraph());
            tableTitle.AppendChild(new Run(new Text("Треки в плейлисте:")));
            tableTitle.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { After = "100" });

            // Создаем таблицу
            Table table = new Table();

            // Стили таблицы
            TableProperties tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.AppendChild(tableProperties);

            // Заголовки столбцов
            TableRow headerRow = new TableRow();
            string[] headers = { "#", "Название", "Исполнители", "Альбом", "Год", "Жанр", "Путь к MP3-файлу" };

            foreach (var header in headers)
            {
                TableCell cell = new TableCell();
                cell.AppendChild(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Auto }));
                Paragraph par = new Paragraph();
                Run run = par.AppendChild(new Run());
                run.AppendChild(new Text(header));
                run.RunProperties = new RunProperties(new Bold());
                cell.AppendChild(par);
                headerRow.AppendChild(cell);
            }
            table.AppendChild(headerRow);

            // Добавляем строки с треками
            for (int i = 0; i < tracks.Count; i++)
            {
                TableRow row = new TableRow();
                var track = tracks[i];

                // Номер в плейлисте
                AddTableCell(row, (i + 1).ToString());

                // Название трека
                AddTableCell(row, track.Title);

                // Исполнители
                AddTableCell(row, track.ArtistsString);

                // Альбом
                string albumTitle = GetAlbumTitle(track.AlbumId);
                AddTableCell(row, albumTitle);

                // Год выпуска
                AddTableCell(row, track.ReleaseYear.ToString());

                // Жанр
                AddTableCell(row, track.Genre);

                // Путь к MP3-файлу
                AddTableCell(row, track.PathToMP3File);

                table.AppendChild(row);
            }
            body.AppendChild(table);
        }

        private void AddTableCell(TableRow row, string text)
        {
            TableCell cell = new TableCell();
            cell.AppendChild(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Auto }));
            Paragraph par = new Paragraph();
            par.AppendChild(new Run(new Text(text)));
            cell.AppendChild(par);
            row.AppendChild(cell);
        }

        private List<TrackModel> GetPlaylistTracks(int playlistId)
        {
            List<TrackModel> tracks = new List<TrackModel>();

            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                string query = @"SELECT t.track_id, t.track_title, t.album_id, t.number_in_album, g.genre_title, t.track_release_year, t.path_to_track_cover, t.path_to_track_mp3_file, t.is_single, GROUP_CONCAT(a.artist_nickname SEPARATOR ', ') as artists FROM TRACKS t
                                JOIN GENRES g ON t.genre_id=g.genre_id
                                JOIN PLAYLIST_TRACKS pt ON pt.track_id=t.track_id
                                LEFT JOIN TRACK_ARTISTS ta ON ta.track_id=t.track_id
                                LEFT JOIN ARTISTS a ON a.artist_id=ta.artist_id
                                WHERE pt.playlist_id=@playlistId
                                GROUP BY t.track_id
                                ORDER BY pt.number_in_playlist";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@playlistId", playlistId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int trackId = reader.GetInt32("track_id");
                        string title = reader.GetString("track_title");
                        int albumId = reader.GetInt32("album_id");
                        int numberInAlbum = reader.GetInt32("number_in_album");
                        string genre = reader.GetString("genre_title");
                        int releaseYear = reader.GetInt32("track_release_year");
                        string coverPath = reader.IsDBNull(reader.GetOrdinal("path_to_track_cover")) ? "" : reader.GetString("path_to_track_cover");
                        string mp3Path = reader.IsDBNull(reader.GetOrdinal("path_to_track_mp3_file")) ? "" : reader.GetString("path_to_track_mp3_file");
                        int isSingle = reader.GetInt32("is_single");

                        List<string> artists = LoadTrackArtists(trackId);

                        tracks.Add(new TrackModel(trackId, title, albumId, numberInAlbum, genre, releaseYear, coverPath, mp3Path, isSingle, artists));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при получении треков из плейлиста");
            }

            connection.Close();
            return tracks;
        }

        private List<string> LoadTrackArtists(int trackId)
        {
            List<string> artists = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(mySqlCon))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand("SELECT ARTISTS.artist_nickname FROM ARTISTS JOIN TRACK_ARTISTS ON ARTISTS.artist_id=TRACK_ARTISTS.artist_id WHERE TRACK_ARTISTS.track_id=@track_id", connection);
                command.Parameters.AddWithValue("@track_id", trackId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        artists.Add(reader.GetString("artist_nickname"));
                    }
                }
            }
            return artists;
        }

        private string GetAlbumTitle(int albumId)
        {
            string album = "";

            using (MySqlConnection connection = new MySqlConnection(mySqlCon))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand("SELECT album_title FROM ALBUMS WHERE album_id=@album_id", connection);
                command.Parameters.AddWithValue("@album_id", albumId);
                object result = command.ExecuteScalar();

                if (result != null)
                {
                    album = result.ToString();
                }
            }
            return album;
        }
    }
}
