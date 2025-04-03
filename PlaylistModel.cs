using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCollectionApp
{
    public class PlaylistModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PathToPlaylistCover { get; set; }

        public PlaylistModel(int id, string title, string description, string pathToPlaylistCover)
        {
            Id = id;
            Title = title;
            Description = description;
            PathToPlaylistCover = pathToPlaylistCover;
        }
    }
}
