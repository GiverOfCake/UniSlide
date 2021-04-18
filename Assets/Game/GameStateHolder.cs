using RhythmEngine.Model;
using UnityEngine;

namespace Game
{

	/// <summary>
	/// Holds persistent game state for scene transitions.
	/// </summary>
    public class GameStateHolder : MonoBehaviour
	{
		public string ChartFolder;
		public int DifficultyId;

		public bool AutoPlay;

		//Other modifiers like practice mode, start from etc.
	}
}