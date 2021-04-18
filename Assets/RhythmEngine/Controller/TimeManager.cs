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

        void Start()
        {
            _noteSpawner = GetComponent<NoteSpawner>();
            _scoreManager = GetComponent<ScoreManager>();
            _stateHolder = FindObjectOfType<GameStateHolder>();

            if(_stateHolder.ChartFile != null)
				StartCoroutine(StartSong(_stateHolder.ChartFile));
        }

        void Update()
        {
            if(source.isPlaying)
            {
                time = source.time + ActiveSong.Offset + syncOffset;
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

        private AudioType AudioTypeFromFilename(string filename)
        {
	        filename = filename.ToLower();
	        if (filename.EndsWith(".mp3") || filename.EndsWith(".mp2"))
		        return AudioType.MPEG;
	        if (filename.EndsWith(".ogg"))
		        return AudioType.OGGVORBIS;
	        if (filename.EndsWith(".wav"))
		        return AudioType.WAV;
	        Debug.LogError("Unknown/unsupported audio format from file: " + filename);
	        return AudioType.UNKNOWN;
        }

        public IEnumerator StartSong(string chartFile)
        {
	        //TODO handle case of no slashes
	        string directory = chartFile.Substring(0, chartFile.LastIndexOf('\\'));

	        ISongParser parser = null;
	        if (chartFile.EndsWith(".sm"))
		        parser = new SMParser();
	        else if (chartFile.EndsWith(".sus"))
		        parser = new SusParser();
	        else
	        {
		        Debug.LogError("Unknown chart file format in file: " + chartFile);
		        yield return null;
	        }
            ActiveSong = parser.ParseSong(chartFile);
            //ActiveSong = new SMParser().ParseSong(path + "\\BBKKBKK.sm");
            //ActiveSong = new SMParser().ParseSong(path + "\\Oshama Scramble.sm");
            //ActiveSong = new SMParser().ParseSong(path + "\\Splatter Party.sm");
            //ActiveSong = new SMParser().ParseSong(path + "\\Outer Science.sm");


            //messy temporary song initialisation for testing.
            yield return null;

            ActiveChart = ActiveSong.Charts[0];

            var audioType = AudioTypeFromFilename(ActiveSong.AudioFile);
            using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + directory + "\\" + ActiveSong.AudioFile, audioType))
            {
	            yield return www.SendWebRequest();

	            if (www.isNetworkError)
	            {
		            Debug.Log(www.error);
	            }
	            else
	            {
		            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
		            source.clip = audioClip;
		            while (source.clip.loadState != AudioDataLoadState.Loaded)
			            yield return new WaitForSeconds(0.1f);
		            source.Play();
	            }
            }

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