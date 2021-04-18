using RhythmEngine.Controller;

namespace RhythmEngine.Model.Events
{
	public interface IScoreable
	{
		NoteTime Time { get; }

		/// <summary>
		/// Check for scoring updates on this event.
		/// </summary>
		/// <param name="state">The current input state</param>
		/// <param name="scoreManager">The ScoreManager to report scoring data back to</param>
		void UpdateScoring(InputState state, ScoreManager scoreManager);

		/// <summary>
		/// Used by the game to determine if this object should be unloaded.
		/// </summary>
		/// <returns>False if we're sure this note has been hit or is offscreen when missed; true otherwise.</returns>
		bool IsRelevant(double beat);
	}
}