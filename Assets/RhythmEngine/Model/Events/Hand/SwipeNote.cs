using System;
using RhythmEngine.Controller;
using UnityEngine;

namespace RhythmEngine.Model.Events.Hand
{
	public class SwipeNote: Note, IScoreable
	{

		protected bool Scored = false;
		protected bool Hit = false;

		public override int PrimaryTextureId => 2;


		public SwipeNote(NoteTime time, LanePosition position) : base(time, position)
		{

		}

		public virtual void UpdateScoring(InputState state, ScoreManager scoreManager)
		{
			//TODO currently a copy of SimpleNote: fix this!

			//obviously don't score if we've already scored this note
			if (Scored)
				return;


			double timeDelta = Math.Abs(state.Time - Time.Seconds);
			bool isEarly = Time.Seconds > state.Time;

			//late note out of scoring threshold: doesn't matter if there was input, this is a miss
			if(timeDelta > scoreManager.scoringThreshold && !isEarly)
			{
				scoreManager.onScoring(new Scoring(timeDelta, Scoring.Rank.Miss, this));
				Scored = true;
				return;
			}

			//First, check we're in the input range
			if (!state.DownInRange(Position))
				return;
			//We have a relevant 'down' event on our note. Let's see how well they did.

			//just for safety, disregard attempts that are out of the score threshold
			// (but only early hits; 'late' out of the scoring threshold means there was no attempt made to hit this note, and it can be counted as a miss.
			if (timeDelta > scoreManager.scoringThreshold && isEarly)
			{
				return;
			}

			//consider this change 'absorbed'
			state.AbsorbChangesInRange(Position);

			//now score this change based on the time delta.
			var scoring = scoreManager.GenerateScoring(state.Time, this);
			Hit = scoring.Ranking != Scoring.Rank.Miss;


			//finally, fire a scoring event.
			scoreManager.onScoring(scoring);
			Scored = true;
		}

		/// <summary>
		/// Used by the game to determine if this object should be unloaded.
		/// </summary>
		/// <returns>False if we're sure this note has been hit or is offscreen when missed; true otherwise.</returns>
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