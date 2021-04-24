using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmEngine.Model
{
    public class Song
    {
        public string Name;
        public string Artist;

        public string Directory;
        public string AudioFile;

        //TODO things like not specifying an offset will result in below being NaN! Make sure we're NaN tolerant!
        public double Offset = double.NaN;
        public double PreviewStart = double.NaN;
        public double PreviewEnd = double.NaN;

        public List<Chart> Charts;

        public void Merge(Song other)
        {
	        if (other.Directory != Directory)
	        {
		        throw new Exception("Songs from different directories cannot be merged");
	        }

	        //more validations could be done here... (warn if name, artist etc. are different?)

	        //copy over any properties we may be missing
	        if (Name == null)
		        Name = other.Name;
	        if (Artist == null)
		        Artist = other.Artist;
	        if (AudioFile == null)
		        AudioFile = other.AudioFile;
	        if (double.IsNaN(Offset))
		        Offset = other.Offset;
	        if (double.IsNaN(PreviewStart))
		        PreviewStart = other.PreviewStart;
	        if (double.IsNaN(PreviewEnd))
		        PreviewEnd = other.PreviewEnd;

	        //combine all charts together
	        Charts.AddRange(other.Charts);
        }
    }
}