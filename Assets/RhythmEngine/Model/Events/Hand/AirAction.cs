using System;
using RhythmEngine.Controller;

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

        public override bool IsRelevant(double beat)
        {
	        if (Hit)
		        return false;//already hit this note, we no longer need it on screen.

	        //else, check if we're way past missing:
	        double relativeBeat = ((Time.Beats - beat) * Time.ApproachRateMultiplier);
	        //TODO take into account time as well for high BPM/approach rate late hits (offscreen but still valid)!
	        if (relativeBeat < -2)
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