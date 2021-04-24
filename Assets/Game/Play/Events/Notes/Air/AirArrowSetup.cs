using System;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using RhythmEngine.Model.Events.Hand;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Game.Play.Events.Notes
{
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	public class AirArrowSetup: MonoBehaviour
	{
		/// <summary>
		/// The height (on the Y axis) the arrow mesh should reach
		/// </summary>
		public float ArrowHeight = 5f;

		/// <summary>
		/// The amount to multiply by when applying left or right arrow shift on the top edge of the mesh.
		/// </summary>
		public float ShiftMultiplier = 3f;

		/// <summary>
		/// The offset (on the Z axis) needed to make the arrow mesh line up with the 'back' edge of the note it may be attached to.
		/// </summary>
		public float MeshZOffset = 1f;

		/// <summary>
		/// Scroll speed used for animation rising/falling animation. Value is inverted for down arrows.
		/// </summary>
		public float ShaderScrollSpeed = 0.75f;

		/// <summary>
		/// Amount to shift the half-width by. Used for making arrows align nicely with notes
		/// </summary>
		public float WidthModifier = 0.5f;

		private readonly int ShaderSpeedPropertyID = Shader.PropertyToID("ScrollSpeed");

		public void Init(AirArrow airArrow)
		{
			float uvStart, uvEnd;
			if (airArrow.IsUp)
			{
				uvStart = 0f;
				uvEnd = 0.5f;
			}
			else
			{
				uvStart = 0.5f;
				uvEnd = 1f;
			}

			GetComponent<MeshFilter>().mesh = GenerateMesh(airArrow.Position.Width, uvStart, uvEnd, airArrow.ArrowShift);

			var material = GetComponent<MeshRenderer>().material;

			float scrollSpeed = ShaderScrollSpeed;
			if (!airArrow.IsUp)
				scrollSpeed *= -1;//invert for down arrows

			material.SetFloat(ShaderSpeedPropertyID, scrollSpeed);
		}

		public Mesh GenerateMesh(int width, float uvEnd, float uvStart, int arrowShift)
		{
			Mesh mesh = new Mesh();

			float hWidth = width / 2 - WidthModifier;// minus 1 to compensate for note edge size
			float shift = arrowShift * ShiftMultiplier;
			var verts = new Vector3[]
			{
				new Vector3(-hWidth        , 0f,          MeshZOffset),
				new Vector3(+hWidth        , 0f,          MeshZOffset),
				new Vector3(-hWidth + shift, ArrowHeight, MeshZOffset),
				new Vector3(+hWidth + shift, ArrowHeight, MeshZOffset)
			};
			mesh.vertices = verts;

			var tris = new int[]
			{
				2, 1, 0,
				2, 3, 1
			};
			mesh.triangles = tris;

			var uvs = new Vector2[]
			{
				new Vector2(uvStart, 0f),
				new Vector2(uvEnd,   0f),
				new Vector2(uvStart, 1f),
				new Vector2(uvEnd,   1f)
			};
			mesh.uv = uvs;

			return mesh;
		}
	}
}