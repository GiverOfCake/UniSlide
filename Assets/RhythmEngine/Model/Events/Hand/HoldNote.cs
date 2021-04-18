using RhythmEngine.Controller;

namespace RhythmEngine.Model.Events.Hand
{
	public class HoldNote: HeldNote, IScoreable
	{
		public override float TrackUvStart => 0.5f;
		public override float TrackUvEnd => 1f;

		public HoldNote(NoteTime time, LanePosition position, SlidePoint[] slidePoints) : base(time, position, slidePoints)
		{

		}

		public void UpdateScoring(InputState state, ScoreManager scoreManager)
		{
			//throw new System.NotImplementedException();
		}
	}
}