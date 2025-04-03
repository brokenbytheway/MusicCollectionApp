using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace MusicCollectionApp
{
    public class MusicPlayerService
    {
        private static MusicPlayerService _instance;
        private MediaPlayer _mediaPlayer;
        private List<string> _playlist;
        private int _currentTrackIndex;
        private bool _isPaused = false;
        public event Action<string> TrackChanged;

        public static MusicPlayerService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MusicPlayerService();
                return _instance;
            }
        }

        private MusicPlayerService()
        {
            _mediaPlayer = new MediaPlayer();
            _playlist = new List<string>();
            _currentTrackIndex = -1;

            _mediaPlayer.MediaEnded += (s, e) => PlayNextTrack(); // Подписываемся на событие окончания трека
        }

        public void SetPlaylist(List<string> tracks)
        {
            _playlist = tracks;
            _currentTrackIndex = _playlist.Any() ? 0 : -1;
        }

        public void PlayTrack(string trackPath)
        {
            if (string.IsNullOrEmpty(trackPath)) return;

            _mediaPlayer.Open(new Uri(trackPath));
            _mediaPlayer.Play();
            _currentTrackIndex = _playlist.IndexOf(trackPath);
            TrackChanged?.Invoke(trackPath);
        }

        public void ResumeTrack()
        {
            if (_mediaPlayer.Source == null) return; // Проверяем, загружен ли трек

            if (_isPaused)
            {
                _mediaPlayer.Play();
                _isPaused = false;
            }

            else
            {
                _mediaPlayer.Pause();
                _isPaused = true;
            }
        }

        public void PlayNextTrack()
        {
            if (_playlist.Count == 0 || _currentTrackIndex == -1) return;

            _currentTrackIndex = (_currentTrackIndex + 1) % _playlist.Count;
            PlayTrack(_playlist[_currentTrackIndex]);
        }

        public void PlayPrevTrack()
        {
            if (_playlist.Count == 0 || _currentTrackIndex == -1) return;

            _currentTrackIndex = (_currentTrackIndex - 1 + _playlist.Count) % _playlist.Count;
            PlayTrack(_playlist[_currentTrackIndex]);
        }

        public double GetCurrentTime()
        {
            return _mediaPlayer.Position.TotalSeconds;
        }

        public double GetDuration()
        {
            return _mediaPlayer.NaturalDuration.HasTimeSpan ? _mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds : 0;
        }

        // Метод для перемещения проигрывателя на определённое время
        public void SeekToTime(double seconds)
        {
            if (_mediaPlayer.Source != null)
            {
                _mediaPlayer.Position = TimeSpan.FromSeconds(seconds);
            }
        }

        public void SetVolume(double volume)
        {
            _mediaPlayer.Volume = volume; // Устанавливаем громкость в диапазоне от 0 до 1
        }
    }
}
