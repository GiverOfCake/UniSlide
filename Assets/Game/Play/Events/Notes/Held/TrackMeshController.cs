using System;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using UnityEngine;

namespace Game.Play.Events.Notes
{
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	public class TrackMeshController: MonoBehaviour
	{
		private MeshFilter _meshFilter;

		public void Init(HeldNote heldNote)
		{
			_meshFilter = GetComponent<MeshFilter>();
			_meshFilter.mesh = heldNote.GenerateTrackMesh(10);
			transform.localScale = new Vector3(1, 1, (float) heldNote.Time.ApproachRateMultiplier);//scale mesh to approach rate (temp. solution)
			transform.localPosition = new Vector3(0, -0.005f, 0);//slightly below track points
		}
	}
}