using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCollectionApp
{
    public class AlbumModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ReleaseYear { get; set; }
        public string PathToAlbumCover { get; set; }
        public List<string> Artists { get; set; }
        public string ArtistsString => Artists != null && Artists.Count > 0 ? string.Join(", ", Artists) : "Неизвестный исполнитель";

        public AlbumModel(int id, string title, int releaseYear, string pathToAlbumCover, List<string> artists)
        {
            Id = id;
            Title = title;
            ReleaseYear = releaseYear;
            PathToAlbumCover = pathToAlbumCover;
            Artists = artists;
        }
    }
}
