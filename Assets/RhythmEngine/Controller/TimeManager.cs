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

        private double _trueTime;
        private double _lastTrueTime = 0;

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


        public void Start()
        {
            _noteSpawner = GetComponent<NoteSpawner>();
            _scoreManager = GetComponent<ScoreManager>();
            _stateHolder = FindObjectOfType<GameStateHolder>();

            if(_stateHolder.ChartFolder != null)
				StartCoroutine(StartSong(_stateHolder.ChartFolder));
        }

        private double _worstDeviation = 0;

        public void Update()
        {
            if(source.isPlaying)
            {
	            _trueTime = (source.timeSamples * _invSampleRate) + ActiveSong.Offset + syncOffset;
                if (_trueTime == _lastTrueTime)
                {
	                //Time will often not change for a few frames (3 to 4 or so).
	                // Add deltaTime until we can get a new sample.
	                // In one test, this changes max deviation from ~5 milliseconds to ~0.0005 milliseconds.
	                // Note that in this test however that was 0.005 overshoot, and we'll still stutter on some frames without further smoothing.
	                // And also dT is around 3ms at worst, so... still quite off from accurate.
	                // For reference, marvelous scoring margin is 22.5 milliseconds in StepMania.
	                time += Time.deltaTime;


	                //Debug.LogWarning($"Spent multiple frames at time {time}, diff to classic {time - classicTime} dt {Time.deltaTime}");//if this happens too regularly, we may need to interpolate audio timing ourselves
                }
                else
                {
	                //new true time. Reset and report deviation
	                /*
	                double simulatedDeviation = (time + Time.deltaTime) - _trueTime;//compare what we would've gotten this frame to the new true time to see how far off we are
	                if (simulatedDeviation > _worstDeviation && Time.time > 5.0)//ignore first second because startup
	                {
		                _worstDeviation = simulatedDeviation;
						Debug.Log($"Worst deviation: {_worstDeviation}, at time {Time.time}");
	                }
	                //*/
	                //Debug.Log($"Time deviation: {simulatedDeviation}");
	                time = _trueTime;
                }
                _lastTrueTime = _trueTime;

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
	        ActiveSong = new SongLoader(chartFile).LoadSong();

            //messy temporary song initialisation for testing.
            yield return null;
            ActiveChart = ActiveSong.Charts[0];

            yield return AudioHelper.LoadAudio(source, ActiveSong);

            if (_stateHolder.StartFrom > 0f)
	            source.time = _stateHolder.StartFrom;

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