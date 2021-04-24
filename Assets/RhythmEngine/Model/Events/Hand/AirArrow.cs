using System;
using RhythmEngine.Controller;

namespace RhythmEngine.Model.Events.Hand
{

	/// <summary>
	/// Arrows rendered on the ground layer which indicate
	/// </summary>
    public class AirArrow : Note, IScoreable
    {

	    protected bool Scored;
	    protected bool Hit;

	    public override int PrimaryTextureId => -1;

	    /// <summary>
	    /// Sounds counterintuitive, but AirArrow is not 'air' as it's drawn on the ground.
	    /// </summary>
	    public override bool IsAir => false;

	    /// <summary>
	    /// True for 'up' air arrows, false for 'down' air arrows
	    /// </summary>
	    public bool IsUp;

	    /// <summary>
	    /// (purely visual) shift of the arrow. 0 for vertical, -1 for left, +1 for right.
	    /// </summary>
	    public int ArrowShift;

        public AirArrow(NoteTime time, LanePosition position, bool isUp, int arrowShift) : base(time, position)
        {
	        IsUp = isUp;
	        ArrowShift = arrowShift;
        }

        public void UpdateScoring(InputState state, ScoreManager scoreManager)
        {
            if (Scored)
                return;

			//TODO scoring logic

        }
    }
}