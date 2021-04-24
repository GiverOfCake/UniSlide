using RhythmEngine.Controller;

namespace RhythmEngine.Model.Events.Hand
{
	public class AirHold: HeldNote, IScoreable
	{
		public override float TrackUvStart => 2f / 3;
		public override float TrackUvEnd => 1f;

		public override bool IsAir => true;

		public AirHold(NoteTime time, LanePosition position, SlidePoint[] slidePoints) : base(time, position, slidePoints)
		{

		}

		public void UpdateScoring(InputState state, ScoreManager scoreManager)
		{
			//throw new System.NotImplementedException();
		}
	}
}