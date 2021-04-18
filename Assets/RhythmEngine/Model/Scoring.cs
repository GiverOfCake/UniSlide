using RhythmEngine.Model.Events;

namespace RhythmEngine.Model
{
    /// <summary>
    /// Represents the scoring for a single hit (or miss). Used for scoring and hit effects.
    ///
    /// For hold effects: these are managed by the hold renderers themselves to avoid performance issues.
    /// Note that these will still fire Scoring combo events.
    /// </summary>
    public class Scoring
    {
	    public double TimeDelta { get; }

	    /// <summary>
        /// Decides if the hit is early or late. Not applicable for Marvelous ranks.
        /// </summary>
        public bool Early => TimeDelta < 0;


        /// <summary>
        /// The rank earned by this hit. Null if not applicable (e.g. hold combo)
        /// </summary>
        public Rank Ranking;

        /// <summary>
        /// The note (and by extension type of note) this event came from.
        /// </summary>
        public RhythmEvent Source;

        public Scoring(double timeDelta, Rank ranking, RhythmEvent source)
        {
	        TimeDelta = timeDelta;
            Ranking = ranking;
            Source = source;
        }

        public override string ToString()
        {
            if (Ranking == Rank.Marv)
                return Ranking.Name + "!";//no early/late for Marv

            if(Early)
                return "-" + Ranking.Name; //early = leading dash
            else
                return Ranking.Name + "-"; //late = trailing dash
        }

        public class Rank
        {
            private Rank(string name, int value)
            {
                Name = name;
                Value = value;
            }
            public string Name { get; set; }
            public int Value { get; set; }


            public static readonly Rank Miss = new Rank("Miss", 0);
            public static readonly Rank Good = new Rank("Good", 50);
            public static readonly Rank Perf = new Rank("Perfect", 100);
            public static readonly Rank Marv = new Rank("Marvelous", 101);
        }

        public class ScoreType
        {
            //TODO enum for normal, golden, hold etc.
            //TODO effectsCombo should go into here

            private ScoreType(string name, bool effectsCombo)
            {
                Name = name;
                EffectsCombo = effectsCombo;
            }

            public string Name { get; set; }
            public bool EffectsCombo { get; set; }

            public static ScoreType Normal { get { return new ScoreType("Normal", true);}}
            public static ScoreType Golden { get { return new ScoreType("Golden", true);}}
            public static ScoreType HoldContinue { get { return new ScoreType("Golden", true);}}
            public static ScoreType Mine { get { return new ScoreType("Golden", true);}}

        }

    }
}