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

        public double Offset;
        public double PreviewStart;
        public double PreviewEnd;

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
	        if (Offset == null)
		        Offset = other.Offset;
	        if (PreviewStart == null)
		        PreviewStart = other.PreviewStart;
	        if (PreviewEnd == null)
		        PreviewEnd = other.PreviewEnd;

	        //combine all charts together
	        Charts.AddRange(other.Charts);
        }
    }
}