using System;
using RhythmEngine.Controller;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using RhythmEngine.Model.Events.Hand;
using UnityEngine;

namespace Game.Play.Events.Notes
{
    ///<summary>
    /// In charge of the generic RhythmEngine.Model -> Gameplay bridge.
    ///</summary>
    public class NoteController : MonoBehaviour
    {
        public Note Note;

        private TimeManager _timeManager;

        public float NoteHeight = 0.02f;
        public float SliderHeight = 0.01f;
        public float AirHeight = 5f;

        public GameObject NoteMeshPrefab;
        public GameObject TrackMeshPrefab;
        public GameObject ArrowMeshPrefab;

        private static readonly int NoteTextureCount = 8;

        private TrackMeshController _trackController;
        private float _height = -1;

        private void InitNoteMesh(int noteTextureId, LanePosition position, float verticalOffset)
        {
	        float uvStart = (float)noteTextureId       / NoteTextureCount;
	        float uvEnd   = (float)(noteTextureId + 1) / NoteTextureCount;
	        var noteMesh = Instantiate(NoteMeshPrefab);//new generic note mesh
	        noteMesh.transform.parent = transform;//parent to us
	        noteMesh.transform.localPosition = new Vector3(position.Center, 0, verticalOffset);//offset from us
	        noteMesh.GetComponent<NoteMeshSetup>().Init(position.Width, uvStart, uvEnd);//generate mesh
        }

        private void InitTrackMesh(HeldNote heldNote)
        {
	        var noteMesh = Instantiate(TrackMeshPrefab);//new generic track mesh
	        noteMesh.transform.parent = transform;//parent to us
	        noteMesh.transform.localPosition = new Vector3(0, 0, 0);//mesh will hold further offsets
	        _trackController = noteMesh.GetComponent<TrackMeshController>();//store this component for later updates
		    _trackController.Init(heldNote);//generate mesh
        }

        private void InitArrowMesh(AirArrow airArrow)
        {
	        var arrowMesh = Instantiate(ArrowMeshPrefab);//new generic arrow mesh
	        arrowMesh.transform.parent = transform;//parent to us
	        arrowMesh.transform.localPosition = new Vector3(airArrow.Position.Center, 0, 0);//offset from us
	        arrowMesh.GetComponent<AirArrowSetup>().Init(airArrow);//generate mesh
        }

        public void Init(Note note, ScoreManager scoreManager, TimeManager timeManager)
        {
            //store for later
            Note = note;
            _timeManager = timeManager;
            _height = NoteHeight;//otherwise overridden

            //init primary texture
			if(note.PrimaryTextureId >= 0)
				InitNoteMesh(note.PrimaryTextureId, note.Position, 0);

			//do we need a track?
			if (note is HeldNote)
			{
				var heldNote = (HeldNote) note;

				//generate track mesh (and store for later updates)
				InitTrackMesh(heldNote);

				//generate meshes for all visible anchor points:
				foreach (var slidePoint in heldNote.SlidePoints)
				{
					if (slidePoint.Visible)
					{
						float vOff = (float)(slidePoint.Time.StartPosition - note.Time.StartPosition);
						InitNoteMesh(slidePoint.PrimaryTextureId, slidePoint.Position, vOff);
					}
				}

				_height = SliderHeight;
			}

			if (note.IsAir)
				_height += AirHeight;

			//air arrows have special rendering
			if (note is AirArrow)
				InitArrowMesh((AirArrow)note);

			UpdateScroll();
        }

        public void Update()
        {
            UpdateScroll();
            if (!Note.IsRelevant(_timeManager.beat, _timeManager.time))
            {
	            //No longer relevant: destroy all children, then ourselves.
                for (int i = 0; i < transform.childCount; i++)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
                Destroy(gameObject);
            }
        }

        private void UpdateScroll()
        {
            float newZ = Note.Time.PositionAt(_timeManager.time, _timeManager.beat);
            transform.position = new Vector3(0, _height, newZ);
        }
    }
}