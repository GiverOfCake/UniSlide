using RhythmEngine.Controller;

namespace RhythmEngine.Model.Events.Hand
{
	public class HoldNote: HeldNote, IScoreable
	{
		public override float TrackUvStart => 1f / 3;
		public override float TrackUvEnd => 2f / 3;

		public HoldNote(NoteTime time, LanePosition position, SlidePoint[] slidePoints) : base(time, position, slidePoints)
		{

		}

		public void UpdateScoring(InputState state, ScoreManager scoreManager)
		{
			//throw new System.NotImplementedException();
		}
	}
}