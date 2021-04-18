using System;
using System.Collections.Generic;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace RhythmEngine.Controller
{
    /// <summary>
    /// Manages all things related to player success/failure: score, combo, hit and miss animations.
    /// Also manages user input.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>
        /// Notes within this amount if time in seconds will be considered for scoring.
        /// For StepMania for example, this value is double the 'miss window' threshold (.09) -- this is to prevent button mashing from being feasible.
        /// </summary>
        public double scoringThreshold = 0.18;

        /// <summary>
        /// Notes actively being scored.
        /// These are populated by the TimeManager.
        /// </summary>
        public List<IScoreable> ActivelyScoring = new List<IScoreable>();

        /// <summary>
        /// player's input state. instance reused each frame for performance.
        /// </summary>
        private InputState _inputState = new InputState();

        private TimeManager _timeManager;

        /// <summary>
        /// Global scoring trigger for all scoring events.
        /// To be called by Notes during scoring, and can be received by particle managers, the combo UI etc.
        /// </summary>
        public Action<Scoring> onScoring = delegate { };

        /// <summary>
        /// TODO use this
        /// TODO probably best put on an InputManager, which then is listened to by ScoreManager.
        /// To be used for replay recording, 'unused key' display etc.
        /// </summary>
        public Action<InputState> onInputChange;


        /// <summary>
        /// (normal) scoring margin between 'miss' and 'good'
        /// </summary>
        public double goodThreshold = 0.09;
        /// <summary>
        /// (normal) scoring margin between 'good' and 'perfect'
        /// </summary>
        public double perfThreshold = 0.045;
        /// <summary>
        /// (normal) scoring margin between 'perfect' and 'marvelous'
        /// </summary>
        public double marvThreshold = 0.0225;

        /// <summary>
        /// Max time a hold can be 'released' for before it's no longer counted as held.
        /// </summary>
        public double holdTolerance = 0.25;

        void Start()
        {
            _timeManager = GetComponent<TimeManager>();
        }

        //we would like this to fire after TimeManager has updated ActivelyScoring.
        void LateUpdate()
        {
            //get the time for our comparisons
            double time = _timeManager.time;

            if (ActivelyScoring.Count >= 2)
            {
                //in case of multiple scoring events, make sure we evaluate them in order of closeness to 'now' first.
                ActivelyScoring.Sort((a, b) =>
                    Math.Abs(a.Time.Seconds - time).CompareTo(b.Time.Seconds - time));
            }

            if (ActivelyScoring.Count >= 20)
            {
	            Debug.LogWarning("Actively scoring count at " + ActivelyScoring.Count);
            }
            //grab input
            UpdateInput(time);

            //update scoring on all active notes (even if there's no changes! e.g. holds, mines)
            foreach (var note in ActivelyScoring)
            {
                note.UpdateScoring(_inputState, this);
            }
            //remove all no longer relevant notes
            ActivelyScoring.RemoveAll(item => item.IsRelevant(_timeManager.beat) == false);
        }

        /// <summary>
        /// Currently hardcoded array of keyboard inputs. Precision determined by number of keys (must be divisible by 32)
        /// </summary>
        private readonly KeyCode[] _inputKeys1 = {KeyCode.Z, KeyCode.X, KeyCode.N, KeyCode.M};
        private readonly KeyCode[] _inputKeys2 = {KeyCode.A, KeyCode.S, KeyCode.K, KeyCode.L};

        private void UpdateInput(double time)
        {
            //reset input state
            _inputState.ResetChanges(time);
            //obtain new input state from array of checked keys
            for (int i = 0; i < _inputKeys1.Length; i++)
            {
	            byte startingPresses = _inputState.States[i];
	            byte presses = startingPresses;

	            if (Input.GetKeyDown(_inputKeys1[i]))
		            presses++;
                else if(Input.GetKeyUp(_inputKeys1[i]))
		            presses--;
	            if (Input.GetKeyDown(_inputKeys2[i]))
		            presses++;
	            else if(Input.GetKeyUp(_inputKeys2[i]))
		            presses--;

	            if(presses != startingPresses)
					_inputState.WriteState(presses, i, _inputKeys1.Length);
            }
        }

        public Scoring GenerateScoring(double inputTime, RhythmEvent source)
        {
	        double absTimeDelta = Math.Abs(inputTime - source.Time.Seconds);
	        var givenRank = Scoring.Rank.Miss;
	        if(absTimeDelta <= marvThreshold)
		        givenRank = Scoring.Rank.Marv;
	        else if(absTimeDelta <= perfThreshold)
		        givenRank = Scoring.Rank.Perf;
	        else if(absTimeDelta <= goodThreshold)
		        givenRank = Scoring.Rank.Good;
	        return new Scoring(inputTime - source.Time.Seconds, givenRank, source);
        }
    }
}
