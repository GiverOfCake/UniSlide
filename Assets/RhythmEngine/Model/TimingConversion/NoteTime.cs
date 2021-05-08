namespace RhythmEngine.Model.TimingConversion
{
	/// <summary>
	/// The fundamental class for representing events in time with music.
	/// </summary>
	public class NoteTime
	{
		public double Seconds;
		public double Beats;
		public double StartPosition;

		public TimingConverter ToPosition;
		private bool _useBeatsForPosition;

		public NoteTime(double seconds, double beats, TimingConverter toPosition, bool useBeatsForPosition)
		{
			Seconds = seconds;
			Beats = beats;
			ToPosition = toPosition;
			_useBeatsForPosition = useBeatsForPosition;

			StartPosition = ToPosition.ConvertForward(_useBeatsForPosition ? Beats : Seconds);
		}

		public float PositionAt(double seconds, double beats)
		{
			double input;
			if (_useBeatsForPosition)
				input = Beats - beats;
			else
				input = Seconds - seconds;

			return (float)ToPosition.ConvertForward(input);
		}

		public override string ToString()
		{
			return $"Beats: {Beats}, Seconds: {Seconds}";
		}
	}
}
