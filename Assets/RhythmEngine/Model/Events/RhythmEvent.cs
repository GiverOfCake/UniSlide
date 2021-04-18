namespace RhythmEngine.Model.Events
{
    public abstract class RhythmEvent
    {
        /// <summary>
        /// The (start) time of this event.
        /// </summary>
        public NoteTime Time { get; set; }


        /// <summary>
        /// Used by the game to determine if this object should be unloaded.
        /// </summary>
        /// <returns>False if we're sure this note has been hit or is offscreen when missed; true otherwise.</returns>
        public virtual bool IsRelevant(double beat)
        {
	        return Time.Beats >= beat;
        }
    }
}