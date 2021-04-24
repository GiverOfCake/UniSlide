using System;
using RhythmEngine.Controller;
using RhythmEngine.Model.Events;
using UnityEngine;

namespace Game.Play.Events.Notes
{
    //TODO remove, this class is basically pointless since we now have generic notes
    public class NoteSpawner : MonoBehaviour
    {
        public GameObject GenericNotePrefab;

        private ScoreManager _scoreManager;
        private TimeManager _timeManager;

        private void Start()
        {
            _scoreManager = GetComponent<ScoreManager>();
            _timeManager = GetComponent<TimeManager>();
        }

        public void SpawnNote(Note note)
        {
            Instantiate(GenericNotePrefab).GetComponent<NoteController>().Init(note, _scoreManager, _timeManager);
        }
    }
}