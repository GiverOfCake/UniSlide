namespace RhythmEngine.Model
{
	/// <summary>
	/// The fundamental class for representing events in time with music.
	/// </summary>
	public class NoteTime
	{
		public double Seconds;
		public double Beats;
		public double ApproachRateMultiplier;

		public NoteTime(double seconds, double beats, double approachRateMultiplier)
		{
			Seconds = seconds;
			Beats = beats;
			ApproachRateMultiplier = approachRateMultiplier;
		}

		public override string ToString()
		{
			return $"Beats: {Beats}, Seconds: {Seconds}";
		}
	}
}
