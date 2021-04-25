namespace RhythmEngine.Model.TimingConversion
{
	/// <summary>
	/// The fundamental class for representing events in time with music.
	/// </summary>
	public class NoteTime
	{
		public double Seconds;
		public double Beats;
		private double _hitPosition;

		public TimingConverter ToPosition;
		private bool _useBeatsForPosition;

		public NoteTime(double seconds, double beats, TimingConverter toPosition, bool useBeatsForPosition)
		{
			Seconds = seconds;
			Beats = beats;
			ToPosition = toPosition;
			_useBeatsForPosition = useBeatsForPosition;
			_hitPosition = toPosition.ConvertForward(useBeatsForPosition ? beats : seconds);
		}

		public float PositionAt(double seconds, double beats)
		{
			double input = _useBeatsForPosition ? beats : seconds;
			double position = ToPosition.ConvertForward(input);
			return (float)(_hitPosition - position);
		}

		public override string ToString()
		{
			return $"Beats: {Beats}, Seconds: {Seconds}";
		}
	}
}
