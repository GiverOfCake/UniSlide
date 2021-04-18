using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Play.Events.Notes;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using RhythmEngine.Parser;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RhythmEngine.Controller
{
    /// <summary>
    /// Manages audio playback and the synchronisation of visual and scoring with the audio.
    /// </summary>
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(NoteSpawner))]
    public class TimeManager : MonoBehaviour
    {
        public AudioSource source;

        public Song ActiveSong;

        public Chart ActiveChart;

        public double beat;
        public double time;

        private NoteSpawner _noteSpawner;
        private ScoreManager _scoreManager;
        private GameStateHolder _stateHolder;

        private int _scoreTimePointer = 0;
        private List<RhythmEvent> _notesByScoreTime;

        /// <summary>
        /// Decides the synchronisation offset. Will vary between machines and we'll need a calibration routine to figure out how to store these.
        /// </summary>
        public double syncOffset = -0.111207;

        private double _invSampleRate = 0;
        private double _lastTime = 0;

        public void Start()
        {
            _noteSpawner = GetComponent<NoteSpawner>();
            _scoreManager = GetComponent<ScoreManager>();
            _stateHolder = FindObjectOfType<GameStateHolder>();

            if(_stateHolder.ChartFolder != null)
				StartCoroutine(StartSong(_stateHolder.ChartFolder));
        }

        public void Update()
        {
            if(source.isPlaying)
            {
                //time = source.time + ActiveSong.Offset + syncOffset;
                time = (source.timeSamples * _invSampleRate) + ActiveSong.Offset + syncOffset;//more accurate version
                if (time == _lastTime)
                {
	                Debug.LogWarning($"Spent multiple frames at time {time}");//if this happens too regularly, we may need to interpolate audio timing ourselves
                }
                _lastTime = time;

                beat = ActiveChart.Rhythm.BeatAt(time);
            }

            //advance note scoring queue if necessary:
            //keep going until the pointer is no longer within the scoring consideration threshold
            while (_notesByScoreTime != null && _notesByScoreTime[_scoreTimePointer].Time.Seconds <= time + _scoreManager.scoringThreshold)
            {
                //Report the new note(s) to the ScoreManager
                if(_notesByScoreTime[_scoreTimePointer] is IScoreable)
					_scoreManager.ActivelyScoring.Add((IScoreable)_notesByScoreTime[_scoreTimePointer]);

                //advance the pointer.
                _scoreTimePointer++;
                if (_scoreTimePointer >= _notesByScoreTime.Count)
                    _notesByScoreTime = null; //Last note -> close our scoring queue.
            }

        }



        public IEnumerator StartSong(string chartFile)
        {
	        var song = new SongLoader(chartFile).LoadSong();

            //messy temporary song initialisation for testing.
            yield return null;
            ActiveChart = ActiveSong.Charts[0];

            yield return AudioHelper.LoadAudio(source, ActiveSong);
            source.Play();
            _invSampleRate = 1.0 / source.clip.frequency;


            //create a sorted copy with LINQ
            _notesByScoreTime = ActiveChart.Notes.OrderBy(o => o.Time.Seconds).ToList();
            //NOTE: C# in-place sort: objListOrder.Sort((x, y) => x.OrderDate.CompareTo(y.OrderDate));

            foreach(Note note in ActiveChart.Notes)
            {
                _noteSpawner.SpawnNote(note);
                yield return null;
            }
        }
    }
}