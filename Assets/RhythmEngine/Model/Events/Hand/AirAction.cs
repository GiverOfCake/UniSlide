using System;
using RhythmEngine.Controller;
using RhythmEngine.Model.TimingConversion;

namespace RhythmEngine.Model.Events.Hand
{

    public class AirAction : Note, IScoreable
    {

	    protected bool Scored;
	    protected bool Hit;

	    public override int PrimaryTextureId => 0;

	    public override bool IsAir => true;

        public AirAction(NoteTime time, LanePosition position) : base(time, position)
        {

        }

        public void UpdateScoring(InputState state, ScoreManager scoreManager)
        {
            if (Scored)
                return;

			//TODO scoring logic

        }

        public override bool IsRelevant(double beat, double seconds)
        {
	        if (Hit)
		        return false;//already hit this note, we no longer need it on screen.

	        //else, check if we're way past missing:
	        double position = Time.PositionAt(seconds, beat);
	        if (position < -2)
	        {
		        //so we're offscreen, but we need to be sure we're not at ultra-high BPM and that there's no chance this note can count as a hit.
		        if (Scored)
			        return false;//we're certain there's no way we can hit this note.
	        }

	        //otherwise, we're still relevant
	        return true;
        }
    }
}