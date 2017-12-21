using System.Collections.Generic;

namespace StreamRipperMusicLibrary.Models
{
    public class SongModel
    {
        public string Artist { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public List<string> Tags { get; set; }
        public string Url { get; set; }
        public string StreamUrl { get; set; }
        public string FileName { get; set; }
    }
}