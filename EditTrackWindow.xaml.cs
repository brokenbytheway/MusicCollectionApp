using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Windows;

namespace MusicCollectionApp
{
    public partial class EditTrackWindow : Window
    {
        private TracksUserControl parentControl;
        private TrackModel track;
        private MySqlConnection connection;
        private string mySqlCon = "Server=37.128.207.248; port=3306; database=musiccollection; user=listener_user; password=password;";
        private string selectedMp3Path = null;

        public EditTrackWindow(TracksUserControl parent, TrackModel trackToEdit)
        {
            InitializeComponent();
            parentControl = parent;
            track = trackToEdit;
            connection = new MySqlConnection(mySqlCon);
            connection.Open();
        }

        private void SelectMp3_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3 files (*.mp3)|*.mp3",
                Title = "Выберите MP3-файл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedMp3Path = openFileDialog.FileName;
                mp3PathTextBlock.Text = System.IO.Path.GetFileName(selectedMp3Path);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(selectedMp3Path))
            {
                MessageBox.Show("Выберите MP3-файл!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MySqlCommand command = new MySqlCommand("UPDATE TRACKS SET path_to_track_mp3_file=@path_to_track_mp3_file WHERE track_id=@track_id", connection);
                command.Parameters.AddWithValue("@path_to_track_mp3_file", selectedMp3Path);
                command.Parameters.AddWithValue("@track_id", track.Id);
                command.ExecuteNonQuery();

                parentControl.RefreshTracks();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при обновлении трека");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
