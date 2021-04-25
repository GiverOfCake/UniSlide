using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using RhythmEngine.Model.Events.Hand;
using RhythmEngine.Model.TimingConversion;
using UnityEngine.XR;

namespace RhythmEngine.Parser
{
    /// <summary>
    /// Parser for StepMania chart files.
    /// </summary>
    public class SMParser : ISongParser
    {
        public Song ParseSong(string filename)
        {
            var theSourceFile = new FileInfo(filename);
            var reader = theSourceFile.OpenText();

            var contentsBuffer = new StringBuilder();

            string line;
            do
            {
                line = reader.ReadLine();
                if (line != null)
                    contentsBuffer.AppendLine(line);
            } while (line != null);

            var contents = contentsBuffer.ToString();
            contents = Regex.Replace(contents, @"//.*.\n", "");
            contents = Regex.Replace(contents, @"\n|\r", "");
            contents = Regex.Replace(contents, @"\t", " ");
            contents = Regex.Replace(contents, @",#NOTES", ";#NOTES");

            var song = new Song();
            var charts = new List<Chart>();
            var bpmChanges = new List<BeatEvent>();
            var bpmStops = new List<BeatEvent>();
            TimingConverter bpmGraph = null;

            string title = "";
            string subTitle = "";
            string artist = "";
            string musicFile = "";

            double offset = double.NaN;

            double scrollSpeed = 40;//TODO don't hardcode this

            foreach (var command in contents.Split(';'))
            {
                string[] bits = command.Split(':'); //0=command, 1=data (more for #NOTES)
                if (bits.Length == 1) // blank given, enter blank in manually
                {
                    bits = new string[2];
                    bits[0] = command.Replace(":", "");
                    bits[1] = "";
                }

                if (!bits[0].StartsWith("#"))
                {
                    //System.out.println("warning: \"" + command + "\" is not a tag");
                    continue;
                }

                bits[0] = bits[0].Substring(1).ToLower(); //remove # and make lowercase (now case insensitive)
                switch (bits[0])
                {
                    case "title":
                        if (title == "") title = bits[1];
                        break;
                    case "subtitle":
                        if (subTitle == "") subTitle = bits[1];
                        break;
                    case "titletranslit":
                        if (bits[1].Trim() != "") title = bits[1];
                        break;
                    case "subtitletranslit":
                        if (bits[1].Trim() != "") subTitle = bits[1];
                        break;
                    case "artist":
                    case "credit":
                        artist = bits[1];
                        break;
                    case "music":
                        musicFile = bits[1];
                        break;

                    case "offset":
                        offset = double.Parse(bits[1], CultureInfo.InvariantCulture);
                        break;
                    case "notes":
                        if (bpmGraph == null)
                        {
                            bpmGraph = TimingConverter.ParseBpm(bpmChanges, bpmStops);
                        }

                        if (bits[1].Replace(" ", "") == "dance-single")
                        {
                            charts.Add(ParseChart(bits, bpmGraph, scrollSpeed));
                        }
                        else
                        {
                            //System.out.println("chart type " + bits[1] + ", ignoring");
                        }

                        break;
                    case "bpms":
                    {
                        string[] bpms = bits[1].Split(',');
                        foreach (var bpm in bpms)
                        {
                            if (bpm != "") bpmChanges.Add(new BeatEvent(bpm));
                        }
                    }
                        break;
                    case "stops":
                    {
                        string[] stops = bits[1].Split(',');
                        if (bits[1] != "")
                        {
                            foreach (var stop in stops)
                            {
                                if (stop != "") bpmStops.Add(new BeatEvent(stop));
                            }
                        }
                    }
                        break;
                    default:
                        //System.out.println("Unknown tag \"" + bits[0] + "\", ignoring");
                        break;
                }
            }

            song.Name = title;
            song.AudioFile = musicFile;
            song.Charts = charts;
            song.Offset = offset;

            return song;
        }

        private Chart ParseChart(string[] tagData, TimingConverter bpm, double scrollSpeed)
        {
            //0 will be the note tag, skip
            string chartType = tagData[1].Replace(" ", "");
            int keysPerMeasure = 4; //currently hardcoded for dance-single
            string description = tagData[2].Replace(" ", "");
            string difficulty = tagData[3].Replace(" ", "");
            int meter = int.Parse(tagData[4].Replace(" ", ""));
            //grooveRadar data below; we're ignoring this for now
            //String grooves[] = tagData[5].replaceAll(" ", "").split(",");

            NoteTimeFactory ntf = new NoteTimeFactory(bpm, TimingConverter.ParseNotespeed(new List<SpeedSection>(), scrollSpeed), true);
            string[] measureLines = tagData[6].Replace(" ", "").Split(',');

            var notes = new List<RhythmEvent>();
            for (int i = 0; i != measureLines.Length; i++)
            {
                parseMeasure(i, measureLines[i], keysPerMeasure, notes, ntf);
            }

            return new Chart(difficulty, bpm, notes);
        }

        private void parseMeasure(int beat, string keyData, int keysPerMeasure, List<RhythmEvent> notes, NoteTimeFactory ntf)
        {
            if (keyData.Length % keysPerMeasure != 0)
            {
                throw new ArgumentException("incorrect number of keys in measure: " + keyData);
            }

            int states = keyData.Length / keysPerMeasure;
            for (int i = 0; i != states; i++)
            {
                double offset = (beat + (1.0 / states) * i) * 4;
                parseState(offset, keyData.Substring(i * keysPerMeasure, keysPerMeasure), notes, ntf);
            }
        }

        private HeldNote[] _activeHolds = new HeldNote[32];
        private int _activeHoldCount = 0;

        private void parseState(double offset, string keys, List<RhythmEvent> notes, NoteTimeFactory ntf)
        {
            int totalActions = keys.Count(key => "124LF".Contains(key + "")) + _activeHoldCount;

            int laneWidth = 32 / keys.Length;

            for (int i = 0; i < keys.Length; i++)
            {
                switch (keys[i])
                {
                    case '0': //none
                        break;
                    case '1': //normal
                        notes.Add(new SimpleNote(ntf.FromBeat(offset), new LanePosition(i * laneWidth, laneWidth),
                            totalActions >= 2));
                        break;
                    case 'M': //mine
                        notes.Add(new Mine(ntf.FromBeat(offset), new LanePosition(i * laneWidth, laneWidth)));
                        break;
                    case 'F': //fake
                        //notes.Add(new Mine(bpm.FromBeat(offset), new LanePosition(i * laneWidth, laneWidth), true));
                        break;
                    case '2': //start hold
                    case '4': //start roll
                    {
	                    notes.Add(new SimpleNote(ntf.FromBeat(offset), new LanePosition(i * laneWidth, laneWidth), keys[i] == '4'));
                        var hold = new HoldNote(ntf.FromBeat(offset), new LanePosition(i * laneWidth, laneWidth),
                            new SlidePoint[1]);
                        notes.Add(hold);
                        _activeHoldCount++;
                        _activeHolds[i * laneWidth] = hold;
                    }
                        break;
                    case '3': // end hold/roll
                    {
	                    var activeHold = _activeHolds[i * laneWidth];
	                    activeHold.SlidePoints[0] = new SlidePoint(activeHold, ntf.FromBeat(offset), activeHold.Position);
                        _activeHolds[i * laneWidth] = null;
                        _activeHoldCount--;
                    }
                        break;
                    //TODO holds, rolls, lifts, fakes etc.
                }
            }
        }


    }
}