using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using RhythmEngine.Model.Events.Hand;
using UnityEngine;

namespace RhythmEngine.Parser
{
    public class SusParser: ISongParser
    {
        public static void Main()
        {
            int something = 0;
            Console.WriteLine(something.ToString());
        }

        /// <summary>
        /// Compiles the format used in the SUS format specification to regex.
        /// </summary>
        private static Regex CompileRule(string input)
        {
            input = input.Replace(" ", @" *");//space tolerant

            //inline types
            input = input.Replace("x", @"([0-9a-z])");//starting lane base36
            input = input.Replace("y", @"([0-9a-z])");//channel base36
            input = input.Replace("zz", @"(\w{2})");//special data base36
            input = input.Replace("mmm", @"(\d{3})");//(usually) bar number int

            //'end' data types
            input = input.Replace("double", @"([0-9.]+)");
            input = input.Replace("int", @"(\d+)");
            input = input.Replace("string", "\"([^\"]*)\"");
            input = input.Replace("notes", @"([0-9a-z ]{2,})");

            return new Regex(input, RegexOptions.IgnoreCase);
        }

        private static int ParseB36(char c)
        {
            //TODO untested!
            if (c >= '0' && c <= '9')
                return c - '0';
            else if (c >= 'a' && c <= 'z')
                return 10 + (c - 'a');
            else if (c >= 'A' && c <= 'Z')
	            return 10 + (c - 'A');
            else
                throw new ArgumentException("Illegal character for Base 36: " + c);
        }

        private static int ParseXY(Group matchGroup)
        {
            string text = matchGroup.Value;
            if (text.Length != 1)
                throw new ArgumentException("Expected single character in x or y sus value, found: " + text);
            return ParseB36(text[0]);
        }

        private static int ParseZz(Group matchGroup)
        {
            string text = matchGroup.Value;
            if (text.Length != 2)
                throw new ArgumentException("Expected 2 characters in zz sus value, found: " + text);
            return ParseB36(text[0]) * 36 + ParseB36(text[1]);
        }

        private static int ParseMmm(Group matchGroup)
        {
            string text = matchGroup.Value;
            if (text.Length != 3)
                throw new ArgumentException("Expected 3 characters in mmm sus value, found: " + text);
            return int.Parse(text);
        }
        private static int ParseInt(Group matchGroup)
        {
            return int.Parse(matchGroup.Value);
        }
        private static double ParseDouble(Group matchGroup)
        {
            return double.Parse(matchGroup.Value, CultureInfo.InvariantCulture);
        }
        private static IList<Tuple<int, int>> ParseNotes(Group matchGroup)
        {
	        string text = matchGroup.Value.Replace(" ", "");
	        var notes = new List<Tuple<int, int>>();
	        for (int i = 0; i < text.Length; i+=2)
	        {
		        notes.Add(new Tuple<int, int>(ParseB36(text[i]), ParseB36(text[i + 1])));
	        }
	        return notes;
        }

        private static void ProcessNotes(Group matchGroup, Action<double, int, int> noteProcessor)
        {
	        var notes = ParseNotes(matchGroup);
	        for (int i = 0; i < notes.Count; i++)
	        {
		        int type = notes[i].Item1;
		        if(type == 0)
			        continue;//don't process blanks

		        int width = notes[i].Item2;
		        double position = ((double) i) / notes.Count;

		        noteProcessor(position, type, width);
	        }
        }

        private static void ProcessChannelNotes(Match match, int barOffset, BpmGraph barToBeat, BpmGraph bpmGraph,
	        Dictionary<int, List<PartialData>> channelData)
        {
	        int barNum = ParseMmm(match.Groups[1]) + barOffset;
	        int lane = ParseXY(match.Groups[2]);
	        int channel = ParseXY(match.Groups[3]);
	        ProcessNotes(match.Groups[4], (barOff, type, width) =>
	        {
		        double beat = barToBeat.BeatAt(barNum + barOff);
		        var time = bpmGraph.FromBeat(beat);
		        var pos = new LanePosition(lane * 2, width * 2);
		        if (!channelData.ContainsKey(channel))
			        channelData.Add(channel, new List<PartialData>());
		        channelData[channel].Add(new PartialData(type, pos, time));
	        });
        }


        private readonly Regex _wave = CompileRule("WAVE string");//audio file
        private readonly Regex _waveOffset = CompileRule("WAVEOFFSET double");//audio offset

        private readonly Regex _request = CompileRule("REQUEST string");//special attributes


        private readonly Regex _measureBaseValue = CompileRule("MEASUREBS int");//int newBarBaseValue (from here in file onwards) (TODO understand this)
        private readonly Regex _barLength = CompileRule("mmm02: int");//Hex barNumber, int newBeatsPerBar

        private readonly Regex _bpmDef = CompileRule("BPMzz: double");//B36 BpmId, double newBpm
        private readonly Regex _bpmChange = CompileRule("mmm08: int");//Hex barNumber, int BpmId

        private readonly Regex _tap = CompileRule("mmm1x: notes");//Hex bar, B36 startLane, notes
        private readonly Regex _hold = CompileRule("mmm2xy: notes");//Hex bar, B36 startLane, B36 channel, notes
        private readonly Regex _slide = CompileRule("mmm3xy: notes");//Hex bar, B36 startLane, B36 channel, notes
        private readonly Regex _airHold = CompileRule("mmm4xy: notes");//Hex bar, B36 startLane, B36 channel, notes
        private readonly Regex _arrow = CompileRule("mmm5x: notes");//Hex bar, B36 startLane, notes


        public Song ParseSong(string filename)
        {
            var theSourceFile = new FileInfo(filename);
            var reader = theSourceFile.OpenText();

            var data = new List<string>();

            while(true)
            {
                string line = reader.ReadLine();
                if (line == null)
                    break;
                if (line.StartsWith("#"))
                    data.Add(line.Substring(1));
            }

            bool[] hasBeenRead = new bool[data.Count];

            int ticksPerBeat = 480;//default
            int beatsPerBar = 4;//default TODO this is being ignored! should be slowing down approach rate/effective BPM when increased
            int barOffset = 0;//default
            Song song = new Song();


            var barSections = new List<BarSection>();
            var bpmDefinitions = new Dictionary<int, double>();//BPM ID -> BPM value

            string difficulty = null;

            //PASS 1: construct base rhythm model
            for (int i = 0; i < data.Count; i++)
            {
                if(hasBeenRead[i])
                    continue;
                var line = data[i];
                //assume we read this line (simplifies our code a bit)
                hasBeenRead[i] = true;

                //now try all regex rules for this stage in sequence:

                var match = _barLength.Match(line);
                if (match.Success)
                {
                    barSections.Add(new BarSection(ParseMmm(match.Groups[1]), ParseInt(match.Groups[2])));
                    continue;
                }

                match = _bpmDef.Match(line);
                if (match.Success)
                {
                    bpmDefinitions.Add(ParseZz(match.Groups[1]), ParseDouble(match.Groups[2]));
                    continue;
                }

                //also parse song info while we're at it:

                match = _wave.Match(line);
                if (match.Success)
                {
                    song.AudioFile = match.Groups[1].Value;
                    continue;
                }

                match = _waveOffset.Match(line);
                if (match.Success)
                {
                    song.Offset = ParseDouble(match.Groups[1]);
                    continue;
                }

                //reach here, nothing matched, so we didn't read this line after all
                hasBeenRead[i] = false;
            }

            //We now have enough information to construct the Bar->Beat converter
            var barToBeat = BpmGraph.ParseBars(barSections);

            var bpmChanges = new List<BeatEvent>();
            //PASS 2: construct internal rhythm model
            for (int i = 0; i < data.Count; i++)
            {
                if(hasBeenRead[i])
                    continue;
                var line = data[i];
                //assume we read this line (simplifies our code a bit)
                hasBeenRead[i] = true;

                //now try all regex rules for this stage in sequence:

                var match = _bpmChange.Match(line);
                if (match.Success)
                {
                    bpmChanges.Add(new BeatEvent(barToBeat.BeatAt(ParseMmm(match.Groups[1])), bpmDefinitions[ParseInt(match.Groups[2])]));
                    continue;
                }

                //reach here, nothing matched, so we didn't read this line after all
                hasBeenRead[i] = false;

            }
			//We can now construct our BPM Graph, which means we now have everything needed to start parsing notes.
            BpmGraph bpmGraph = BpmGraph.ParseBpm(bpmChanges, Array.Empty<BeatEvent>());

            var notes = new List<RhythmEvent>();

            var holdData = new Dictionary<int, List<PartialData>>();
            var slideData = new Dictionary<int, List<PartialData>>();
            var airData = new Dictionary<int, List<PartialData>>();

            //PASS 3: read all note data
            for (int i = 0; i < data.Count; i++)
            {
	            if(hasBeenRead[i])
		            continue;
	            var line = data[i];
	            //assume we read this line (simplifies our code a bit)
	            hasBeenRead[i] = true;

	            //now try all regex rules for this stage in sequence:

	            var match = _tap.Match(line);
	            if (match.Success)
	            {
		            int barNum = ParseMmm(match.Groups[1]) + barOffset;
		            int lane = ParseXY(match.Groups[2]);
		            ProcessNotes(match.Groups[3], (barOff, type, width) =>
		            {
			            double beat = barToBeat.BeatAt(barNum + barOff);
			            var time = bpmGraph.FromBeat(beat);
			            var pos = new LanePosition(lane * 2, width * 2);
			            switch (type)
			            {

				            case 1://standard
					            notes.Add(new SimpleNote(time, pos, false));
					            break;
				            case 2://golden
					            notes.Add(new SimpleNote(time, pos, true));
					            break;
				            case 3://swipe
					            notes.Add(new SwipeNote(time, pos));
					            break;
				            case 4://mine
					            notes.Add(new Mine(time, pos));
					            break;
				            //TODO types 5 & 6
				            default:
					            Debug.LogError($"Ignoring unknown tap type {type} at time {time} position {pos}");
					            break;
			            }
		            });


		            continue;
	            }

	            match = _arrow.Match(line);
	            if (match.Success)
	            {
		            int barNum = ParseMmm(match.Groups[1]) + barOffset;
		            int lane = ParseXY(match.Groups[2]);
		            ProcessNotes(match.Groups[3], (barOff, type, width) =>
		            {
			            double beat = barToBeat.BeatAt(barNum + barOff);
			            var time = bpmGraph.FromBeat(beat);
			            var pos = new LanePosition(lane * 2, width * 2);
			            bool isUp = true;
			            int arrowShift = 0;
			            //convert the 6 cases to up/down/left/right/center information
			            switch (type)
			            {
				            case 1:
					            isUp = true;
					            arrowShift = 0;
					            break;
				            case 2:
					            isUp = false;
					            arrowShift = 0;
					            break;
				            case 3:
					            isUp = true;
					            arrowShift = -1;
					            break;
				            case 4:
					            isUp = true;
					            arrowShift = +1;
					            break;
				            case 5:
					            isUp = false;
					            arrowShift = +1;//inverted because down
					            break;
				            case 6:
					            isUp = false;
					            arrowShift = -1;//inverted because down
					            break;
				            default:
					            Debug.LogError($"Unknown Air Arrow {type} at time {time} position {pos}");
					            break;
			            }
			            notes.Add(new AirArrow(time, pos, isUp, arrowShift));
		            });

			            continue;
	            }
	            //matching holds, airholds and slides are basically exactly the same, the only thing that changes is the output array

	            match = _hold.Match(line);
	            if (match.Success)
	            {
		            ProcessChannelNotes(match, barOffset, barToBeat, bpmGraph, holdData);
		            continue;
	            }

	            match = _airHold.Match(line);
	            if (match.Success)
	            {
		            ProcessChannelNotes(match, barOffset, barToBeat, bpmGraph, airData);
		            continue;
	            }

	            match = _slide.Match(line);
	            if (match.Success)
	            {
		            ProcessChannelNotes(match, barOffset, barToBeat, bpmGraph, slideData);
		            continue;
	            }

	            //reach here, nothing matched, so we didn't read this line after all
	            hasBeenRead[i] = false;

            }

            //all note data has now been read. We'll now assemble all multi-part notes.

            //sort note data and store length so we can use this for lookups on start/end of holds (TODO)
            notes.Sort((a, b) => a.Time.Beats.CompareTo(b.Time.Beats));
            int noteDataLength = notes.Count;

            //TODO a lot of code below for (air)holds/slides is shared, may be useful to combine these into a generic function

            //holds:
            foreach (var holdChannel in holdData.Values)
            {
	            //first sort all hold data in this channel
	            holdChannel.Sort((a, b) => a.Time.Beats.CompareTo(b.Time.Beats));
	            //now walk through each in sequence with a state machine to assemble the holds

	            PartialData? startingPoint = null;

	            foreach (var dataPoint in holdChannel)
	            {
		            switch (dataPoint.Type)
		            {
			            case 1://start
				            if(startingPoint != null)
					            Debug.LogError($"Hold started on channel with already active hold at {startingPoint?.Time}");
				            startingPoint = dataPoint;
				            break;
			            case 2://end
				            if (startingPoint == null)
				            {
					            Debug.LogError($"Attempt to end hold at {dataPoint.Time} without a starting point.");
					            continue;
				            }

				            var holdNote = new HoldNote(startingPoint.Value.Time, dataPoint.Pos, new SlidePoint[1]);
				            holdNote.SlidePoints[0] = new SlidePoint(holdNote, dataPoint.Time, holdNote.Position);
				            notes.Add(holdNote);
				            startingPoint = null;
				            break;
			            case 3://relay
				            Debug.LogWarning($"Skipping hold relay point"); //TODO (will be fixed if we merge slide and hold parsing code)
				            break;
			            default:
				            Debug.LogError($"Unknown hold point type {dataPoint.Type} found at {dataPoint.Time}");
				            break;
		            }
	            }
            }

            //air holds:
            foreach (var airChannel in airData.Values)
            {
	            //first sort all hold data in this channel
	            airChannel.Sort((a, b) => a.Time.Beats.CompareTo(b.Time.Beats));
	            //now walk through each in sequence with a state machine to assemble the holds

	            PartialData? startingPoint = null;

	            foreach (var dataPoint in airChannel)
	            {
		            switch (dataPoint.Type)
		            {
			            case 1://start
				            if(startingPoint != null)
					            Debug.LogError($"Air hold started on channel with already active hold at {startingPoint?.Time}");
				            startingPoint = dataPoint;
				            break;
			            case 2://end
				            if (startingPoint == null)
				            {
					            Debug.LogError($"Attempt to end air hold at {dataPoint.Time} without a starting point.");
					            continue;
				            }

				            var airHold = new AirHold(startingPoint.Value.Time, dataPoint.Pos, new SlidePoint[1]);
				            airHold.SlidePoints[0] = new SlidePoint(airHold, dataPoint.Time, airHold.Position);
				            notes.Add(airHold);
				            notes.Add(new AirAction(dataPoint.Time, dataPoint.Pos));
				            startingPoint = null;
				            break;
			            case 3://relay
				            notes.Add(new AirAction(dataPoint.Time, dataPoint.Pos));
				            break;
			            default:
				            Debug.LogError($"Unknown hold point type {dataPoint.Type} found at {dataPoint.Time}");
				            break;
		            }
	            }
            }

            //slides:
            foreach (var slideChannel in slideData.Values)
            {
	            //first sort all hold data in this channel
	            slideChannel.Sort((a, b) => a.Time.Beats.CompareTo(b.Time.Beats));
	            //now walk through each in sequence with a state machine to assemble the holds

	            var dataPoints = new List<PartialData>();

	            foreach (var dataPoint in slideChannel)
	            {
		            switch (dataPoint.Type)
		            {
			            case 1://start
				            if(dataPoints.Count > 0)
					            Debug.LogError($"Hold started on channel with already active slide at {dataPoints[0].Time}");
				            dataPoints.Add(dataPoint);
				            break;
			            case 2://end
				            if (dataPoints.Count == 0)
				            {
					            Debug.LogError($"Attempt to end slide at {dataPoint.Time} without a starting point.");
					            continue;
				            }
				            dataPoints.Add(dataPoint);
							//we now have all points, create a SlideNote from this:
				            var slidePoints = new List<SlidePoint>();
				            var slideNote = new SlideNote(dataPoints[0].Time, dataPoints[0].Pos, null);
				            Note previous = slideNote;
				            SlidePoint.AnchorNote anchor = null;
				            for (int i = 1; i < dataPoints.Count; i++)//start from 1, the initial point is the base SlideNote
				            {
					            var pointHere = dataPoints[i];
					            if (pointHere.Type == 4)
					            {
						            //anchor point
						            anchor = new SlidePoint.AnchorNote(pointHere.Time, pointHere.Pos);
						            continue;
					            }

					            var newPoint = new SlidePoint(previous, pointHere.Time, pointHere.Pos);
					            newPoint.AnchorPoint = anchor;//writes anchor if set, null otherwise
					            anchor = null;
					            newPoint.Visible = pointHere.Type == 3;//set visible/invisible
					            slidePoints.Add(newPoint);
					            previous = newPoint;
				            }
				            slideNote.SlidePoints = slidePoints.ToArray();
				            notes.Add(slideNote);
				            dataPoints.Clear();
				            break;
			            case 3://relay
					    case 4://bezier anchor
				        case 5://invisible relay
				            if (dataPoints.Count == 0)
				            {
					            Debug.LogError($"Attempt to add points to slide at {dataPoint.Time} without a starting point.");
					            continue;
				            }
				            dataPoints.Add(dataPoint);
				            break;
			            default:
				            Debug.LogError($"Unknown slide point type {dataPoint.Type} found at {dataPoint.Time}");
				            break;
		            }
	            }
            }

			//finally, sort the notes and return the finished chart
            notes.Sort((a, b) => a.Time.Beats.CompareTo(b.Time.Beats));

			//TODO store difficulty in here
            var chart = new Chart("undefined", bpmGraph, notes);
            song.Charts = new List<Chart>();
			song.Charts.Add(chart);

			return song;
        }

        private struct PartialData
        {
	        public int Type;
	        public LanePosition Pos;
	        public NoteTime Time;

	        public PartialData(int type, LanePosition pos, NoteTime time)
	        {
		        Type = type;
		        Pos = pos;
		        Time = time;
	        }
        }
    }

}
