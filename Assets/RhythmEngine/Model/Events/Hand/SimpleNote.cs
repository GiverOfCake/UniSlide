using System;
using RhythmEngine.Controller;
using RhythmEngine.Model.TimingConversion;
using UnityEngine;

namespace RhythmEngine.Model.Events.Hand
{
	public class SimpleNote: Note, IScoreable
	{


		/// <summary>
		/// If set to true, hitting start of note is always scored as perfect (and comes with extra FX)
		/// </summary>
		public bool Golden;

		protected bool Scored = false;
		protected bool Hit = false;

		private static double _avgDeviation = 0;
		private static int _deviationSamples = 0;

		public override int PrimaryTextureId => Golden ? 6 : 5;



		public SimpleNote(NoteTime time, LanePosition position, bool golden) : base(time, position)
		{
			Golden = golden;
		}

		public virtual void UpdateScoring(InputState state, ScoreManager scoreManager)
		{

			//TODO: big flaw in scoring: we're not counting misses when notes go out of range without being touched!

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

			//golden notes always marvelous
			if(Golden && Hit)
				scoring.Ranking = Scoring.Rank.Marv;

			//finally, fire a scoring event.
			scoreManager.onScoring(scoring);
			Scored = true;

			if (Hit)
			{
				_avgDeviation += state.Time - Time.Seconds;
				_deviationSamples++;
				if(_deviationSamples % 50 == 0)
					Debug.Log("Average hit latency: " + (_avgDeviation / _deviationSamples) + " from " + _deviationSamples + " samples.");
			}
		}

		/// <summary>
		/// Used by the game to determine if this object should be unloaded.
		/// </summary>
		/// <returns>False if we're sure this note has been hit or is offscreen when missed; true otherwise.</returns>
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