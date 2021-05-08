using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using RhythmEngine.Model;
using RhythmEngine.Model.Events;
using RhythmEngine.Model.Events.Hand;
using RhythmEngine.Model.TimingConversion;
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

            return new Regex( "^" + input + "$", RegexOptions.IgnoreCase);
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

        private static void ProcessChannelNotes(Match match, int barOffset, TimingConverter barToBeat, NoteTimeFactory ntf,
	        Dictionary<int, List<PartialData>> channelData)
        {
	        int barNum = ParseMmm(match.Groups[1]) + barOffset;
	        int lane = ParseXY(match.Groups[2]);
	        int channel = ParseXY(match.Groups[3]);
	        ProcessNotes(match.Groups[4], (barOff, type, width) =>
	        {
		        double beat = barToBeat.ConvertForward(barNum + barOff);
		        var time = ntf.FromBeat(beat);
		        var pos = new LanePosition(lane * 2, width * 2);
		        if (!channelData.ContainsKey(channel))
			        channelData.Add(channel, new List<PartialData>());
		        channelData[channel].Add(new PartialData(type, pos, time));
	        });
        }


        private static readonly Regex Wave = CompileRule("WAVE string");//audio file
        private static readonly Regex WaveOffset = CompileRule("WAVEOFFSET double");//audio offset

        private static readonly Regex Request = CompileRule("REQUEST string");//special attributes


        private static readonly Regex MeasureBaseValue = CompileRule("MEASUREBS int");//int newBarBaseValue (from here in file onwards) (TODO understand this)
        private static readonly Regex BarLength = CompileRule("mmm02: int");//Hex barNumber, int newBeatsPerBar

        private static readonly Regex BpmDef = CompileRule("BPMzz: double");//B36 BpmId, double newBpm
        private static readonly Regex BpmChange = CompileRule("mmm08: int");//Hex barNumber, int BpmId
        private static readonly Regex HispeedDef = CompileRule("TILzz: string");//B36 HispeedId, string data
        private static readonly Regex HispeedChange = CompileRule("HISPEED zz");//B36 HispeedId
        private static readonly Regex HispeedOff = CompileRule("NOSPEED");
        private static readonly Regex HispeedRule = new Regex("^\\s*(\\d+)'(\\d+):\\s*([0-9.\\-]+)\\s*$");//meas'tick:speed. Assumes commas already separated.

        private static readonly Regex Tap = CompileRule("mmm1x: notes");//Hex bar, B36 startLane, notes
        private static readonly Regex Hold = CompileRule("mmm2xy: notes");//Hex bar, B36 startLane, B36 channel, notes
        private static readonly Regex Slide = CompileRule("mmm3xy: notes");//Hex bar, B36 startLane, B36 channel, notes
        private static readonly Regex AirHold = CompileRule("mmm4xy: notes");//Hex bar, B36 startLane, B36 channel, notes
        private static readonly Regex Arrow = CompileRule("mmm5x: notes");//Hex bar, B36 startLane, notes

        private static NoteTimeFactory ParseHispeed(string hispeedRules, TimingConverter barToBeat, TimingConverter secondsToBeat, int ticksPerBeat, double scrollSpeedMult)
        {
	        var sections = new List<SpeedSection>();
	        foreach (string rule in hispeedRules.Split(','))
	        {
		        var parsedRule = HispeedRule.Match(rule);
		        if (!parsedRule.Success)
			        throw new FormatException($"Incorrectly formatted hispeed rule: \"{rule}\" found in rules string \"{hispeedRules}\"");

		        double barNumber = ParseDouble(parsedRule.Groups[1]);
		        double ticksPerBar = ticksPerBeat * barToBeat.FindSlopeForward(barNumber + 0.5);//slope in this context is beats per bar
		        barNumber += ParseDouble(parsedRule.Groups[2]) / ticksPerBar; //with ticks per bar, we can take the tick and convert it to a decimal bar position
		        double beat = barToBeat.ConvertForward(barNumber);
		        double time = secondsToBeat.ConvertReverse(beat); //bar -> beat -> seconds

		        double newSpeed = ParseDouble(parsedRule.Groups[3]);

		        sections.Add(new SpeedSection(time, newSpeed));
	        }
	        sections.Sort((a, b) => a.Time.CompareTo(b.Time));
	        return new NoteTimeFactory(secondsToBeat, TimingConverter.ParseNotespeed(sections, scrollSpeedMult), false);
        }


        public Song ParseSong(string filename)
        {
	        double scrollSpeed = 50; //TODO don't hardcode this

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
            int barOffset = 0;//default
            var song = new Song();


            var barSections = new List<BarSection>();
            var bpmDefinitions = new Dictionary<int, double>();//BPM ID -> BPM value
            var hispeedDefinitions = new Dictionary<int, string>();//Hispeed ID -> definition string

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

                var match = BarLength.Match(line);
                if (match.Success)
                {
                    barSections.Add(new BarSection(ParseMmm(match.Groups[1]), ParseInt(match.Groups[2])));
                    continue;
                }

                match = BpmDef.Match(line);
                if (match.Success)
                {
                    bpmDefinitions.Add(ParseZz(match.Groups[1]), ParseDouble(match.Groups[2]));
                    continue;
                }

                match = HispeedDef.Match(line);
                if (match.Success)
                {
	                hispeedDefinitions.Add(ParseZz(match.Groups[1]), match.Groups[2].Value);
                    continue;
                }

                //also parse song info while we're at it:

                match = Wave.Match(line);
                if (match.Success)
                {
                    song.AudioFile = match.Groups[1].Value;
                    continue;
                }

                match = WaveOffset.Match(line);
                if (match.Success)
                {
                    song.Offset = ParseDouble(match.Groups[1]);
                    continue;
                }

                //reach here, nothing matched, so we didn't read this line after all
                hasBeenRead[i] = false;
            }

            //We now have enough information to construct the Bar->Beat converter
            var barToBeat = TimingConverter.ParseBars(barSections);

            var bpmChanges = new List<BeatEvent>();
            //PASS 2: construct internal rhythm model
            //TODO merge this pass with the above one by storing the changes and then processing them here (faster and less copy-pasty)
            for (int i = 0; i < data.Count; i++)
            {
                if(hasBeenRead[i])
                    continue;
                var line = data[i];
                //assume we read this line (simplifies our code a bit)
                hasBeenRead[i] = true;

                //now try all regex rules for this stage in sequence:

                var match = BpmChange.Match(line);
                if (match.Success)
                {
                    bpmChanges.Add(new BeatEvent(barToBeat.ConvertForward(ParseMmm(match.Groups[1])), bpmDefinitions[ParseInt(match.Groups[2])]));
                    continue;
                }

                //reach here, nothing matched, so we didn't read this line after all
                hasBeenRead[i] = false;

            }
			//We can now construct our BPM Graph, which means we now have everything needed to start parsing notes.
			bpmChanges.Sort((a, b) => a.beat.CompareTo(b.beat));
            var bpmGraph = TimingConverter.ParseBpm(bpmChanges, Array.Empty<BeatEvent>());
            var defaultNtf = new NoteTimeFactory(bpmGraph, TimingConverter.ParseNotespeed(new List<SpeedSection>(), scrollSpeed), false);
            var ntf = defaultNtf;

            //parse the hispeeds and generate NTFs for them
            var hispeedNtfs = new Dictionary<int, NoteTimeFactory>();
            foreach (var definition in hispeedDefinitions)
	            hispeedNtfs[definition.Key] = ParseHispeed(definition.Value, barToBeat, bpmGraph, ticksPerBeat, scrollSpeed);


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

	            var match = Tap.Match(line);
	            if (match.Success)
	            {
		            int barNum = ParseMmm(match.Groups[1]) + barOffset;
		            int lane = ParseXY(match.Groups[2]);
		            ProcessNotes(match.Groups[3], (barOff, type, width) =>
		            {
			            double beat = barToBeat.ConvertForward(barNum + barOff);
			            var time = ntf.FromBeat(beat);
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

	            match = Arrow.Match(line);
	            if (match.Success)
	            {
		            int barNum = ParseMmm(match.Groups[1]) + barOffset;
		            int lane = ParseXY(match.Groups[2]);
		            ProcessNotes(match.Groups[3], (barOff, type, width) =>
		            {
			            double beat = barToBeat.ConvertForward(barNum + barOff);
			            var time = ntf.FromBeat(beat);
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

	            match = Hold.Match(line);
	            if (match.Success)
	            {
		            ProcessChannelNotes(match, barOffset, barToBeat, ntf, holdData);
		            continue;
	            }

	            match = AirHold.Match(line);
	            if (match.Success)
	            {
		            ProcessChannelNotes(match, barOffset, barToBeat, ntf, airData);
		            continue;
	            }

	            match = Slide.Match(line);
	            if (match.Success)
	            {
		            ProcessChannelNotes(match, barOffset, barToBeat, ntf, slideData);
		            continue;
	            }

	            match = HispeedChange.Match(line);
	            if (match.Success)
	            {
		            ntf = hispeedNtfs[ParseZz(match.Groups[1])];
		            continue;
	            }

	            match = HispeedOff.Match(line);
	            if (match.Success)
	            {
		            ntf = defaultNtf;
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
