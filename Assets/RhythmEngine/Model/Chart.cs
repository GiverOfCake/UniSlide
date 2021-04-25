using System.Collections.Generic;
using RhythmEngine.Model.Events;
using RhythmEngine.Model.TimingConversion;

namespace RhythmEngine.Model
{
    /// <summary>
    /// Stores a playable chart's data. In additon to Note Events, this also includes the BPM events. See the property for an explanation.
    ///
    /// </summary>
    public class Chart
    {
        /// <summary>
        /// Numeric difficulty indicator.
        /// </summary>
        public string Difficulty;

        /// <summary>
        /// One might argue Rhythm belongs to the <see cref="Song"/> class.
        /// StepMania initially made this same mistake, however the distinction is made as BPM events are used to create BPM gimmicks.
        /// It is therefore useful for easier charts to exclude these gimmicks, and have harder charts contain BPM gimmicks unique to that difficulty.
        /// </summary>
        public TimingConverter Rhythm;

        /// <summary>
        /// A list of all events that will occur on the playfield, sorted in order of start time.
        /// </summary>
        public List<RhythmEvent> Notes;

        public Chart(string difficulty, TimingConverter rhythm, List<RhythmEvent> notes)
        {
            Difficulty = difficulty;
            Rhythm = rhythm;
            Notes = notes;
        }
    }
}