using System;

namespace RhythmEngine.Model
{
    /// <summary>
    /// Describes a section of the Seconds -> Beat conversion map.
    /// No two BpmSections can have overlapping time periods, but BPM sections can overlap (i.e. true reverse BPM).
    /// It is also possible for the StartBeat and EndBeat of a section to be equal, which would represent a stop.
    /// </summary>
    public class BpmSection
    {
        public double StartTime;
        public double EndTime;
        public double StartBeat;
        public double EndBeat;

        public BpmSection(double startTime, double endTime, double startBeat, double endBeat)
        {
            StartTime = startTime;
            EndTime = endTime;
            StartBeat = startBeat;
            EndBeat = endBeat;
        }

        public double Slope()
        {
            return (EndBeat - StartBeat) / (EndTime - StartTime);
        }

        public bool TimeInBounds(double time)
        {
            return !(time > EndTime || time < 0);
        }

        public bool BeatInBounds(double beat)
        {
            return !(beat > EndBeat || beat < 0);
        }

        public double BeatAt(double time)
        {
	        double slope = Slope();
	        if (double.IsNaN(slope))
		        return StartBeat;

	        return (time - StartTime) * slope + StartBeat;
        }

        public double TimeAt(double beat)
        {
            beat -= StartBeat;
            double slope = Slope();
            //Special case to avoid divide by 0 (case for stops). In this case we consider the TimeAt to be the beginning of the stop.
            if (slope == 0 || Double.IsNaN(slope))
                return StartTime;
            else
                return beat * (1.0 / Slope()) + StartTime;
        }

        public override string ToString()
        {
            return StartTime + "-" + EndTime + ":\t" + StartBeat + " " + EndBeat + " \t" + Slope();
        }
    }
}