using System;
using RhythmEngine.Controller;
using RhythmEngine.Model;
using RhythmEngine.Model.Events.Hand;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Game.Play.Events.Score_FX
{
    public class BasicHitFXController : MonoBehaviour
    {
        private VisualEffect _basicHitFx;
        private VFXEventAttribute _eventAttribute;


        private static readonly ExposedProperty PositionAttribute = "position";
        private static readonly ExposedProperty OnHitEvent = "OnHit";

        private void Start()
        {
            _basicHitFx = GetComponent<VisualEffect>();
            _eventAttribute = _basicHitFx.CreateVFXEventAttribute();
            FindObjectOfType<ScoreManager>().onScoring += CreateHitParticle;
        }

        private void CreateHitParticle(Scoring scoring)
        {
            if (scoring.Ranking != null && scoring.Ranking != Scoring.Rank.Miss && scoring.Source is SimpleNote)
            {
	            var simpleNote = (SimpleNote) scoring.Source;
	            if(simpleNote.Golden)
		            return;//golden FX handled in GoldenHitFXController

                _eventAttribute.SetVector3(PositionAttribute, new Vector3(simpleNote.Position.Center, .1f, 0f));
                _basicHitFx.SendEvent(OnHitEvent, _eventAttribute);
            }
        }
    }
}