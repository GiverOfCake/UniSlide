using System.IO;
using RhythmEngine.Model;

namespace RhythmEngine.Parser
{
	public class SongLoader
	{
		public bool Done => _done;

		private string _directory;
		private bool _done;

		public SongLoader(string directory)
		{
			_directory = directory;
			_done = false;
		}

		private ISongParser ParserForExtension(string filename)
		{
			if (filename.EndsWith(".sm"))
				return new SMParser();
			else if (filename.EndsWith(".sus"))
				return new SusParser();
			else
				return null;
		}

		public Song LoadSong()
		{
			Song finalSong = null;
			var files = Directory.EnumerateFiles(_directory, "*");
			foreach (string file in files)
			{
				var parser = ParserForExtension(file);
				if (parser != null)
				{
					var loadedSong = parser.ParseSong(file);
					loadedSong.Directory = _directory;

					if (finalSong == null)
						finalSong = loadedSong;
					else
						finalSong.Merge(loadedSong);
				}
			}

			_done = true;
			return finalSong;
		}

	}
}