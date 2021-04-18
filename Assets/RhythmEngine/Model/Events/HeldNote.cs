using System.Linq;
using UnityEngine;

namespace RhythmEngine.Model.Events
{

	/// <summary>
	/// Generic class for all notes that have both a non-instantaneous scoring region.
	/// NOTE: Scoring for the start of holds is not to be handled here! In order to avoid code duplication, holds typically consist of a SimpleNote overlapping the start of a hold.
	/// </summary>
    public abstract class HeldNote: Note
    {
        /// <summary>
        /// The end time of this hold note
        /// </summary>
        public NoteTime EndTime => SlidePoints[SlidePoints.Length - 1].Time;

        public SlidePoint[] SlidePoints { get; set; }

        public override int PrimaryTextureId => 1;//generic slide start/endpoint texture.

        public virtual float TrackUvStart { get; }
        public virtual float TrackUvEnd { get; }

        public HeldNote(NoteTime time, LanePosition position, SlidePoint[] slidePoints) : base(time, position)
        {
	        SlidePoints = slidePoints;
        }

        public override bool IsRelevant(double beat)
        {
	        return EndTime.Beats >= beat;
        }

        public Mesh GenerateTrackMesh(int sectionsPerBeat)
        {
	        Mesh mesh = new Mesh();
	        int sectionCount = 1;//one section = one horizontal point. Our starting point is 1 such point.
	        foreach (var slidePoint in SlidePoints)
		        sectionCount += slidePoint.MeshSectionsNeeded(sectionsPerBeat);
	        var triangles = new int[(sectionCount - 1) * 2 * 3];

	        //vertical scaling will fix real world matching. 1 beat = 1 Z unit (Z 0 = start point).
	        //X = lane (0-32). Y = 0. Z = length in Beats.
	        var vertices = new Vector3[sectionCount * 2];
	        var uvs = new Vector2[sectionCount * 2];

	        int sectionIndex = 0;
	        foreach (var slidePoint in SlidePoints.Reverse())
	        {
		        int subSections = slidePoint.MeshSectionsNeeded(sectionsPerBeat);
		        for (int i = 0; i < subSections; i++)
		        {
			        float t = (float)i / subSections;//t = 0 ... < 1
			        t = 1 - t;// 1 ... > 0 (since we're going in reverse, and 0 = previous point)
			        float center = slidePoint.PositionAt(t);
			        float halfWidth = slidePoint.WidthAt(t) / 2;
			        float beat = slidePoint.BeatAt(t);
			        //TODO below assumes all relay points are invisible: we need to calculate up to next relay point and then reverse until next/last!
			        float progress = Mathf.InverseLerp((float) EndTime.Beats, (float) Time.Beats, beat);
			        beat -= (float)Time.Beats;//range from 0 to length
			        int v0 = sectionIndex * 2;
			        int v1 = v0 + 1;
			        vertices[v0] = new Vector3(center - halfWidth, 0f, beat);
			        vertices[v1] = new Vector3(center + halfWidth, 0f, beat);
			        uvs[v0]      = new Vector2(TrackUvStart, progress);
			        uvs[v1]      = new Vector2(TrackUvEnd, progress);
			        sectionIndex++;
		        }
	        }
	        //final vertex: the start point.
	        vertices[sectionIndex * 2]     = new Vector3(Position.Center - Position.Width / 2, 0f, 0f);
	        vertices[sectionIndex * 2 + 1] = new Vector3(Position.Center + Position.Width / 2, 0f, 0f);
	        uvs[sectionIndex * 2]          = new Vector2(TrackUvStart, 1);
	        uvs[sectionIndex * 2 + 1]      = new Vector2(TrackUvEnd, 1);
	        mesh.SetVertices(vertices);
	        mesh.SetUVs(0, uvs);
	        for (int i = 0; i < sectionCount - 1; i++)
	        {
		        //triangles must be wound clockwise! And I think we did our vertices backwards so this is reversed as well.
		        int triangleOffset = i * 6;
		        int vertexOffset = i * 2;
		        triangles[triangleOffset + 0] = vertexOffset + 1;
		        triangles[triangleOffset + 1] = vertexOffset + 2;
		        triangles[triangleOffset + 2] = vertexOffset + 0;

		        triangles[triangleOffset + 3] = vertexOffset + 3;
		        triangles[triangleOffset + 4] = vertexOffset + 2;
		        triangles[triangleOffset + 5] = vertexOffset + 1;
	        }
	        mesh.SetTriangles(triangles, 0);
	        //shouldn't need normals (unlit).. if needed probably most efficient to write them manually
	        return mesh;
        }
    }
}
