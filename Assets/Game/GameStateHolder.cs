using RhythmEngine.Model;
using UnityEngine;

namespace Game
{

	/// <summary>
	/// Holds persistent game state for scene transitions.
	/// </summary>
    public class GameStateHolder : MonoBehaviour
	{
		/// <summary>
		/// The root directory for the song, containing the chart file(s), audio file, etc
		/// </summary>
		public string ChartFolder;


		public int DifficultyId;

		/// <summary>
		/// If true, user input is ignored and all events are given an ideal score.
		/// </summary>
		public bool AutoPlay;

		/// <summary>
		/// Set to 0 for normal play. Otherwise will skip to this point in the audio file in seconds at game start.
		/// </summary>
		public float StartFrom = 0f;
	}
}