using System;
using RhythmEngine.Controller;
using RhythmEngine.Model;
using RhythmEngine.Model.Events.Hand;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Game.Play.Events.Score_FX
{
    public class GoldenHitFXController : MonoBehaviour
    {
        private VisualEffect _goldenHitPlayfieldFx;
        public GameObject goldenHitPrefab;


        private static readonly ExposedProperty PositionAttribute = "position";
        private static readonly ExposedProperty OnHitEvent = "OnHit";

        private void Start()
        {
            _goldenHitPlayfieldFx = GetComponent<VisualEffect>();
            FindObjectOfType<ScoreManager>().onScoring += CreateHitParticle;
        }

        private void CreateHitParticle(Scoring scoring)
        {
	        if (scoring.Ranking != null && scoring.Ranking != Scoring.Rank.Miss && scoring.Source is SimpleNote)
	        {
		        var simpleNote = (SimpleNote) scoring.Source;
		        if(!simpleNote.Golden)
			        return;//non-golden FX handled in BasicHitFXController

                _goldenHitPlayfieldFx.SendEvent(OnHitEvent);

                GameObject created = Instantiate(goldenHitPrefab, transform, true);
                created.transform.position = new Vector3(simpleNote.Position.Center, 0, 0);
                Destroy(created, 3f);//schedule deletion
                //TODO set width
            }
        }
    }
}