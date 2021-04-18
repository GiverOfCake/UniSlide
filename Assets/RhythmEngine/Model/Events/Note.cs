using System;
using System.Resources;
using RhythmEngine.Controller;
using UnityEngine;

namespace RhythmEngine.Model.Events
{
	/// <summary>
	/// Notes are playable/judged Events.
	/// </summary>
	public abstract class Note: RhythmEvent
	{

		/// <summary>
		/// The leftmost lane (0-31) this note starts at.
		/// </summary>
		public LanePosition Position { get; set; }

		/// <summary>
		/// The texture for the 'starting point' of this Note. Exists to simplify generic rendering.
		/// Return -1 to indicate no primary mesh texture exists (e.g. in the case of air notes)
		/// </summary>
		public virtual int PrimaryTextureId { get; }

		/// <summary>
		/// Used for render positioning
		/// </summary>
		public virtual bool IsAir => false;


		public Note(NoteTime time, LanePosition position)
		{
			Time = time;
			Position = position;
		}


		//public Mesh GenerateNoteMesh


	}


}
