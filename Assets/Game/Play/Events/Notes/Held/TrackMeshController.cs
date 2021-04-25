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
		/// <summary>
		/// If the scroll speed of the start of the track mesh is different than the scroll speed of the end, we cannot have a constant scale for this TrackMesh.
		/// If true, we will rescale every update()
		/// TODO implement this
		/// </summary>
		private bool _dynamicScale = false;

		public void Init(HeldNote heldNote)
		{
			_meshFilter = GetComponent<MeshFilter>();
			_meshFilter.mesh = heldNote.GenerateTrackMesh(10);
			double length = heldNote.EndTime.PositionAt(0, 0) - heldNote.Time.PositionAt(0, 0);

			if (heldNote.Time.ToPosition != heldNote.EndTime.ToPosition)
			{
				Debug.LogWarning($"Dynamic scale needed on held note {heldNote} but not yet supported; applying initial scale only.");
				//the issue is that all relay points will need to be scaled as well, which we can't do from here... TODO
				_dynamicScale = true;
			}

			transform.localScale = new Vector3(1, 1, (float) length);//scale mesh to approach rate
			transform.localPosition = new Vector3(0, -0.005f, 0);//slightly below track points
		}
	}
}