using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCollectionApp
{
    public class GenreModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int TrackCount { get; set; }

        public GenreModel(int id, string title, int trackCount)
        {
            Id = id;
            Title = title;
            TrackCount = trackCount;
        }
    }
}
