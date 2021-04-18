using RhythmEngine.Model;

namespace RhythmEngine.Parser
{
    public interface ISongParser
    {
        Song ParseSong(string filename);
    }
}