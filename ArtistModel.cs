using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCollectionApp
{
    public class ArtistModel
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public string PathToArtistPhoto { get; set; }
        public int TrackCount { get; set; }

        public ArtistModel(int id, string nickname, string pathToArtistPhoto, int trackCount)
        {
            Id = id;
            Nickname = nickname;
            PathToArtistPhoto = pathToArtistPhoto;
            TrackCount = trackCount;
        }
    }
}
