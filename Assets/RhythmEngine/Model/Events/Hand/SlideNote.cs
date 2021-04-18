using RhythmEngine.Controller;

namespace RhythmEngine.Model.Events.Hand
{
	public class SlideNote: HeldNote, IScoreable
	{
		public override float TrackUvStart => 0f;
		public override float TrackUvEnd => .5f;

		public SlideNote(NoteTime time, LanePosition position, SlidePoint[] slidePoints) : base(time, position, slidePoints)
		{

		}

		public void UpdateScoring(InputState state, ScoreManager scoreManager)
		{
			//throw new System.NotImplementedException();
		}
	}
}