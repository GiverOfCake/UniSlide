using System;
using RhythmEngine.Model;

namespace Game.SongSelect
{
	/// <summary>
	/// A group of charts returned by a filter.
	/// </summary>
	public class ChartGroup
	{
		public string GroupName;

		/// <summary>
		/// The internal value this groups. Should be understandable by the source <see cref="IChartFilter"/>.
		/// </summary>
		public object GroupValue;

		public Chart[] Charts;

		public IChartFilter SourceFilter;

		/// <summary>
		/// If true,a difficulty filter has been applied.
		/// </summary>
		public bool DiffsLocked;
	}
}