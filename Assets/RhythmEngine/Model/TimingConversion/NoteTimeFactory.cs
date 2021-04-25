using RhythmEngine.Model;

namespace RhythmEngine.Model.TimingConversion
{
	public class NoteTimeFactory
	{
		private TimingConverter _secToBeat;
		private TimingConverter _toPosition;
		private bool _useBeatsForPosition;

		public NoteTimeFactory(TimingConverter secToBeat, TimingConverter toPosition, bool useBeatsForPosition)
		{
			_secToBeat = secToBeat;
			_toPosition = toPosition;
			_useBeatsForPosition = useBeatsForPosition;
		}

		public NoteTime FromTime(double time)
		{
			return new NoteTime(time, _secToBeat.ConvertForward(time), _toPosition, _useBeatsForPosition);
		}
		public NoteTime FromBeat(double beat)
		{
			return new NoteTime(_secToBeat.ConvertReverse(beat), beat, _toPosition, _useBeatsForPosition);
		}

	}
}