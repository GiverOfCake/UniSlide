using RhythmEngine.Model;
using TMPro;
using UnityEngine;

namespace Game.UI.Controller
{
	public class SongItemController : MonoBehaviour
	{
		public TextMeshPro NameText;
		public TextMeshPro ArtistText;

		public Song Song;
		/// <summary>
		/// If specified, this specific diff is being displayed (e.g. sorting by difficulty), not just the whole song.
		/// </summary>
		public Chart ForcedDiff;
	}
}