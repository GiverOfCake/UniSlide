using System.Collections.Generic;

namespace RhythmEngine.Model
{
    public class Song
    {
        public string Name;
        public string AudioFile;
        public double Offset;
        public List<Chart> Charts;
    }
}