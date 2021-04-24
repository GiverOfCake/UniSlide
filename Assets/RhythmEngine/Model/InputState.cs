using System.IO.IsolatedStorage;

namespace RhythmEngine.Model
{
    /// <summary>
    /// Describes the current state of user input. New taps, active holds, active slides etc.
    /// </summary>
    public class InputState
    {

	    //TODO we will likely benefit with storing time since down events as well -- we could figure out which notes 'add' to the start of a touch
		// (e.g. 3 notes pressed 'at once' may not arrive 'at once' on our side) such that we can avoid multiple notes being hit from the same tap!
		// This information could also be used to better understand swipe notes

	    /// <summary>
	    /// Holds information about the current state of input.
	    /// </summary>
	    public byte[] States { get; } = new byte[32];

	    /// <summary>
        /// Holds information about new 'down' inputs.
        /// </summary>
        private bool[] changes = new bool[32];

        /// <summary>
        /// Holds precise positional input.
        /// In hand mode, this is the 'air' height. In feet mode, this is more precise foot position tracking.
        /// </summary>
        private float[] movement = new float[2];


        /// <summary>
        /// If true, ignore 'real' input and pretend input is perfect.
        /// </summary>
        public bool AutoPlay;

        /// <summary>
        /// The time this input was recorded at.
        /// </summary>
        public double Time;

        /// <summary>
        /// Consider this state the 'old' state.
        /// Use this at the start of the frame on the old InputState to avoid having to construct a new one each frame.
        /// <param name="time">The time this input was recorded at.</param>
        /// </summary>
        public void ResetChanges(double time)
        {
            for (int i = 0; i < 32; i++)
            {
                changes[i] = false;
            }

            Time = time;
        }

        /// <summary>
        /// Writes new input state information.
        /// </summary>
        /// <param name="newState">The new state to write</param>
        /// <param name="pos">The position to write at</param>
        /// <param name="precision">The input precision; defaults to 32 (full precision).</param>
        public void WriteState(byte newState, int pos, int precision = 32)
        {
            if (precision == 32)
            {
                changes[pos] |= States[pos] < newState;
                States[pos] = newState;
            }
            else
            {
                int width = 32 / precision;
                for (int i = 0; i < width; i++)
                {
                    WriteState(newState, (pos * width) + i);
                }
            }
        }


        /// <summary>
        /// Check if any input is 'down' (currently pressed/tapped) in the given range.
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>True if any input is 'down' in the given range.</returns>
        public bool ActiveInRange(LanePosition position)
        {
            for (int i = 0; i < position.Width; i++)
            {
                if (States[i + position.Lane] > 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if any input is has changed from up to down in the given range.
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>True if any input has changed value since the last frame in the given range.</returns>
        public bool DownInRange(LanePosition position)
        {
            for (int i = 0; i < position.Width; i++)
            {
                if (changes[i + position.Lane])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Marks a region as 'absorbed', aka un-marks it as changed. This is used by Notes during scoring to mark they have acknowledged this region of input.
        /// This prevents, e.g, a tap in a fast stream from being scored for 2 or more notes instead of only the closest one (as the closest one would 'absorb' the change first)
        /// </summary>
        /// <param name="position">The position to absorb</param>
        public void AbsorbChangesInRange(LanePosition position)
        {
            for (int i = 0; i < position.Width; i++)
                changes[i + position.Lane] = false;
        }
    }
}
