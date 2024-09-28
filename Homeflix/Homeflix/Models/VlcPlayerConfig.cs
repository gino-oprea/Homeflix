using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homeflix.Models
{
    public class VlcPlayerConfig
    {
        public string VlcPlayerLocation { get; set; } = "";
        public List<Movie> Movies { get; set; }
    }

    public class Movie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LastEpisodePlayed LastEpisodePlayed { get; set; }
        public List<Season> Seasons { get; set; }
    }

    public class LastEpisodePlayed
    {
        public string Episode { get; set; }
        public int Time { get; set; }
    }

    public class Season
    {
        public string Name { get; set; }
        public List<Episode> Episodes { get; set; }
    }

    public class Episode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
