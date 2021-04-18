using UnityEngine;

namespace RhythmEngine.Model.Events
{
	public class SlidePoint: Note
	{
		public AnchorNote AnchorPoint;
		public bool Visible = true;

		protected Note Previous;

		public override int PrimaryTextureId => Visible ? 1 : -1;

		public SlidePoint(Note previous, NoteTime time, LanePosition position) : base(time, position)
		{
			Previous = previous;
			AnchorPoint = null;
		}
		public SlidePoint(Note previous, AnchorNote anchorPoint, NoteTime time, LanePosition position) : base(time, position)
		{
			Previous = previous;
			AnchorPoint = anchorPoint;
		}

		public bool IsBezier => AnchorPoint != null;

		//note: all render work is to happen in terms of beats, time doesn't make sense here

		public float BeatAt(float pos)
		{
			return Mathf.Lerp((float) Previous.Time.Beats, (float) Time.Beats, pos);
		}

		public float WidthAtBeat(double beat)
		{
			float pos = Mathf.InverseLerp((float) Previous.Time.Beats, (float) Time.Beats, (float)beat);
			return WidthAt(pos);
		}

		public float WidthAt(float pos)
		{
			return Mathf.Lerp(Previous.Position.Width, Position.Width, pos);
		}

		public float PositionAtBeat(double beat)
		{
			float pos = Mathf.InverseLerp((float) Previous.Time.Beats, (float) Time.Beats, (float)beat);
			return PositionAt(pos);
		}

		public float PositionAt(float pos)
		{
			if (IsBezier)
			{
				float a = Mathf.Lerp(Previous.Position.Center, AnchorPoint.Position.Center, pos);
				float b = Mathf.Lerp(AnchorPoint.Position.Center, Position.Center, pos);
				return Mathf.Lerp(a, b, pos);
			}
			else //straight line
				return Mathf.Lerp(Previous.Position.Center, Position.Center, pos);
		}

		public int MeshSectionsNeeded(int sectionsPerBeat)
		{
			if (IsBezier)
				return (int) (sectionsPerBeat * (Time.Beats - Previous.Time.Beats));
			else return 1;
		}

		/// <summary>
		/// Empty concrete implementation of Note. For use as anchor points in SlidePoints.
		/// </summary>
		public class AnchorNote : Note
		{
			public AnchorNote(NoteTime time, LanePosition position) : base(time, position)
			{
			}
		}
	}
}