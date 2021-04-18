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
		public void Init(int width, float uvStart, float uvEnd)
		{
			GetComponent<MeshFilter>().mesh = GenerateMesh(width, uvStart, uvEnd);
		}

		public static Mesh GenerateMesh(int width, float uvStart, float uvEnd)
		{
			Mesh mesh = new Mesh();

			float hWidth = width / 2;
			var verts = new Vector3[]
			{
				new Vector3(-hWidth       , 0f, -1f),
				new Vector3(-hWidth       , 0f, +1f),
				new Vector3(-hWidth + .99f, 0f, -1f),
				new Vector3(-hWidth + .99f, 0f, +1f),
				new Vector3(+hWidth - .99f, 0f, -1f),
				new Vector3(+hWidth - .99f, 0f, +1f),
				new Vector3(+hWidth       , 0f, -1f),
				new Vector3(+hWidth       , 0f, +1f)
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
				new Vector2(0f  , uvStart),
				new Vector2(0f  , uvEnd),
				new Vector2(.25f, uvStart),
				new Vector2(.25f, uvEnd),
				new Vector2(.75f, uvStart),
				new Vector2(.75f, uvEnd),
				new Vector2(1f  , uvStart),
				new Vector2(1f  , uvEnd)
			};
			mesh.uv = uvs;

			return mesh;
		}
	}
}