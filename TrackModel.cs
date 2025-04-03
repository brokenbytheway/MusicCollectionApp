using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace MusicCollectionApp
{
    public class TrackModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        //public string Album { get; set; }
        public int AlbumId { get; set; }
        public int NumberInAlbum { get; set; }
        public string Genre { get; set; }
        public int ReleaseYear { get; set; }
        public string PathToTrackCover { get; set; }
        public string PathToMP3File { get; set; }
        public int IsSingle { get; set; }
        public List<string> Artists { get; set; }
        public string ArtistsString => Artists != null && Artists.Count > 0 ? string.Join(", ", Artists) : "Неизвестный исполнитель";

        public TrackModel(int id, string title, int albumId, int numberInAlbum, string genre, int releaseYear, string pathToTrackCover, string pathToMP3File, int isSingle, List<string> artists)
        {
            Id = id;
            Title = title;
            AlbumId = albumId;
            NumberInAlbum = numberInAlbum;
            Genre = genre;
            ReleaseYear = releaseYear;
            PathToTrackCover = pathToTrackCover;
            PathToMP3File = pathToMP3File;
            IsSingle = isSingle;
            Artists = artists;
        }
    }
}
