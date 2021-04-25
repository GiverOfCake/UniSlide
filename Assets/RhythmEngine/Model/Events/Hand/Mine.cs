using System;
using RhythmEngine.Controller;
using RhythmEngine.Model.TimingConversion;

namespace RhythmEngine.Model.Events.Hand
{
    /// <summary>
    /// A mine.
    /// A 'golden' mine is a fake, and is not scored regardless of hit/miss.
    /// </summary>
    public class Mine : Note, IScoreable
    {

	    protected bool Scored;
	    protected bool Hit;

	    public override int PrimaryTextureId => 3;

        public Mine(NoteTime time, LanePosition position) : base(time, position)
        {

        }

        public void UpdateScoring(InputState state, ScoreManager scoreManager)
        {
            if (Scored)
                return;

            //First, check we're in the input range
            if (!state.ActiveInRange(Position))
                return;

            //We have a relevant 'down' on our mine. Note we're not checking for changes as mines can be hit staying down.
            //Let's see if they hit the mine

            double timeDelta = Math.Abs(state.Time - Time.Seconds);
            bool isLate = Time.Seconds > state.Time;

            if (timeDelta <= scoreManager.goodThreshold) //we'll consider this a 'hit'
            {
                scoreManager.onScoring(new Scoring(state.Time - Time.Seconds, Scoring.Rank.Miss, this));
                Scored = true;
            }
            else if (isLate && timeDelta > scoreManager.scoringThreshold)
            {
                scoreManager.onScoring(new Scoring(0, Scoring.Rank.Marv, this));
                Scored = true;
                Hit = true;
            }
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