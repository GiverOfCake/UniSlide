using System;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using UnityEngine;

namespace Game.Play.Events.Notes
{
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	public class NoteMeshSetup: MonoBehaviour
	{
		/// <summary>
		/// The half length (in Z axis) of the note mesh
		/// </summary>
		public float NoteMeshVerticalScale = 1f;

		/// <summary>
		/// The distance from the left/right edge the 'corners' are to be found on the UV texture
		/// </summary>
		public float UvEdgeBoundary = 0.25f;

		/// <summary>
		/// The distance from the left/right edge the 'corners' are to be placed on the mesh
		/// </summary>
		public float MeshEdgeBoundary = 0.99f;

		public void Init(int width, float uvStart, float uvEnd)
		{
			GetComponent<MeshFilter>().mesh = GenerateMesh(width, uvStart, uvEnd);
		}

		public Mesh GenerateMesh(int width, float uvStart, float uvEnd)
		{
			Mesh mesh = new Mesh();

			float hWidth = width / 2;
			var verts = new Vector3[]
			{
				new Vector3(-hWidth                   , 0f, -NoteMeshVerticalScale),
				new Vector3(-hWidth                   , 0f, +NoteMeshVerticalScale),
				new Vector3(-hWidth + MeshEdgeBoundary, 0f, -NoteMeshVerticalScale),
				new Vector3(-hWidth + MeshEdgeBoundary, 0f, +NoteMeshVerticalScale),
				new Vector3(+hWidth - MeshEdgeBoundary, 0f, -NoteMeshVerticalScale),
				new Vector3(+hWidth - MeshEdgeBoundary, 0f, +NoteMeshVerticalScale),
				new Vector3(+hWidth                   , 0f, -NoteMeshVerticalScale),
				new Vector3(+hWidth                   , 0f, +NoteMeshVerticalScale)
			};
			mesh.vertices = verts;

			var tris = new int[]
			{
				0, 1, 2,
				1, 3, 2,

				2, 3, 4,
				3, 5, 4,

				4, 5, 6,
				5, 7, 6
			};
			mesh.triangles = tris;

			var uvs = new Vector2[]
			{
				new Vector2(0f                 , uvStart),
				new Vector2(0f                 , uvEnd),
				new Vector2(UvEdgeBoundary     , uvStart),
				new Vector2(UvEdgeBoundary     , uvEnd),
				new Vector2(1f - UvEdgeBoundary, uvStart),
				new Vector2(1f - UvEdgeBoundary, uvEnd),
				new Vector2(1f                 , uvStart),
				new Vector2(1f                 , uvEnd)
			};
			mesh.uv = uvs;

			return mesh;
		}
	}
}