using System;
using RhythmEngine.Controller;
using RhythmEngine.Model;
using TMPro;
using UnityEngine;

public class ShowScoreController : MonoBehaviour
{
    public float comboScaleOnHit = 1.25f;
    public float comboScaleDownSpeed = .25f;
    
    private float comboScale = 1f;

    private TextMeshPro _textMeshPro;
    private void Start()
    {
        FindObjectOfType<ScoreManager>().onScoring += OnScoring;
        _textMeshPro = GetComponent<TextMeshPro>();
    }

    public void OnScoring(Scoring scoring)
    { 
        if (scoring.Ranking != null)
        {
            _textMeshPro.text = scoring.ToString();
            comboScale = comboScaleOnHit;
        }
    }

    private void Update()
    {
        comboScale = Mathf.Max(1f, comboScale - comboScaleDownSpeed * Time.deltaTime);
        transform.localScale = new Vector3(comboScale, comboScale, 1);
    }
}