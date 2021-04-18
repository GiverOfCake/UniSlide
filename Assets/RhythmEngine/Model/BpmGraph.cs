using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.Assertions;

namespace RhythmEngine.Model
{
    public class BpmGraph
    {
        //TODO: this class doesn't yet handle true negative BPM and naively assumes each beat happens at only 1 possible time.

        public List<BpmSection> BpmSections;

        private BpmSection _beatCache = null;
        private BpmSection _timeCache = null;

        public BpmGraph(List<BpmSection> sections)
        {
            BpmSections = sections;
        }

        public static BpmGraph ParseBars(List<BarSection> barSections)
        {
	        //In this context, 'time' = bar (beat is still beat)
	        barSections.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
	        var barToBeatSections = new List<BpmSection>();
	        double maxBar = 100_000;
	        var lastBarSection = new BpmSection(0, maxBar, 0, maxBar * 4);//default bar section for default Beats Per Bar of 4
	        foreach (var bar in barSections)
	        {
		        double startBeat = lastBarSection.BeatAt(bar.StartBar);
		        double endBeat = (maxBar - bar.StartBar) * bar.NewBeatsPerBar + startBeat;
		        var section = new BpmSection(bar.StartBar, maxBar, startBeat, endBeat);

		        Assert.AreApproximatelyEqual(bar.NewBeatsPerBar, (float)section.Slope());

		        barToBeatSections.Add(section);
		        lastBarSection = section;
	        }

	        return new BpmGraph(barToBeatSections);
        }

        public static BpmGraph ParseBpm(IList<BeatEvent> changes, IList<BeatEvent> stops)
        {
            var beatGraph = new List<BpmSection>(changes.Count);

            double curBps;
            double curBeat;

            double nextBeat = changes[0].beat;
            double nextBps = 60.0 / changes[0].change;//actually inverse BPS

            double time = 0.0;
            foreach (var change in changes.Skip(1))
            {
                curBeat = nextBeat;
                curBps = nextBps;

                nextBeat = change.beat;
                nextBps = 60.0 / change.change;

                double endTime = time + (nextBeat - curBeat) * curBps;
                beatGraph.Add(new BpmSection(time, endTime, curBeat, nextBeat));
                time = endTime;
            }

            curBeat = nextBeat;
            curBps = nextBps;
            nextBeat = Math.Pow(10, 5);//last segment: make it practically infinitely long
            double lastEndTime = time + (nextBeat - curBeat) * curBps;

            beatGraph.Add(new BpmSection(time, lastEndTime, curBeat, nextBeat));//insert last segment

            var finalGraph = new List<BpmSection>(changes.Count + stops.Count);
            int beatPos = 1;//array index (first one already used)
            BpmSection currentSegment = beatGraph[0];
            double timeOffset = 0.0;
            foreach(var stop in stops)
            {
                double beat = stop.beat;
                double delay = stop.change;
                //insert all segments we've passed
                while(beat > currentSegment.EndBeat)
                {
                    finalGraph.Add(currentSegment);
                    currentSegment = beatGraph[beatPos++];
                    currentSegment.StartTime += timeOffset;
                    currentSegment.EndTime += timeOffset;
                }
                //currentSegment now intersects the delay:
                //cut it in half, insert first half and stop, set currentSegment to second half.
                double midTime = currentSegment.TimeAt(beat);

                BpmSection firstHalf = new BpmSection(currentSegment.StartTime, midTime, currentSegment.StartBeat, beat);
                BpmSection stopSection = new BpmSection(midTime, midTime + delay, beat, beat);
                BpmSection secondHalf = new BpmSection(midTime + delay, currentSegment.EndTime + delay, beat, currentSegment.EndBeat);

                finalGraph.Add(firstHalf);
                finalGraph.Add(stopSection);
                currentSegment = secondHalf;

                timeOffset += delay;
            }
            finalGraph.Add(currentSegment);//add the last piece
            //add any remaining segments
            while(beatPos != changes.Count)
            {
                BpmSection seg = beatGraph[beatPos++];
                seg.StartTime += timeOffset;
                seg.EndTime += timeOffset;
                finalGraph.Add(seg);
            }
            return new BpmGraph(finalGraph);
        }

        public double BeatAt(double time)
        {
            //check if last entry is valid for efficiency
            if(_beatCache != null && _beatCache.EndTime >= time && _beatCache.StartTime <= time)
                return _beatCache.BeatAt(time);//reuse cache

            //(trivial) edge case
            if(BpmSections.Count == 1)
                return BpmSections[0].BeatAt(time);

            //binary search
            int min = 0;
            int max = BpmSections.Count - 1;


            while(true)//will break when found
            {
                int midpt = (min + max) / 2;
                if(max - min == 1)//right next to each other
                {
                    if(BpmSections[min].TimeInBounds(time))
                    {
                        _beatCache = BpmSections[min];
                        return BpmSections[min].BeatAt(time);
                    }
                    else
                    {
                        _beatCache = BpmSections[max];
                        return BpmSections[max].BeatAt(time);
                    }
                }
                BpmSection mid = BpmSections[midpt];
                if(mid.EndTime > time)
                {
                    max = midpt;
                }
                else if(mid.StartTime < time)
                {
                    min = midpt;
                }
                else
                {
                    _beatCache = mid;
                    return mid.BeatAt(time);
                }
            }
        }

        public double TimeAt(double beat)
        {
            //check if last entry is valid for efficiency
            if(_timeCache != null && _timeCache.EndBeat >= beat && _timeCache.StartBeat <= beat)
                return _timeCache.TimeAt(beat);//reuse cache

            //(trivial) edge case
            if(BpmSections.Count == 1)
                return BpmSections[0].TimeAt(beat);

            //binary search
            int min = 0;
            int max = BpmSections.Count - 1;

            while(true)//will break when found
            {
                int midpt = (min + max) / 2;
                if(max - min == 1)//right next to each other
                {
                    if(BpmSections[min].BeatInBounds(beat))
                    {
                        _timeCache = BpmSections[min];
                        return BpmSections[min].TimeAt(beat);
                    }
                    else
                    {
                        _timeCache = BpmSections[max];
                        return BpmSections[max].TimeAt(beat);
                    }
                }
                BpmSection mid = BpmSections[midpt];

                if(mid.EndBeat > beat)
                {
                    max = midpt;
                }
                else if(mid.StartBeat < beat)
                {
                    min = midpt;
                }
                else
                {
                    _timeCache = mid;
                    return mid.TimeAt(beat);
                }
            }
        }

        public NoteTime FromTime(double time, double approachRateMultiplier = 40.0)
        {
            return new NoteTime(time, BeatAt(time), approachRateMultiplier);
        }
        public NoteTime FromBeat(double beat, double approachRateMultiplier = 40.0)
        {
            return new NoteTime(TimeAt(beat), beat, approachRateMultiplier);
        }
    }

    public struct BeatEvent
    {
        public readonly double beat;
        public readonly double change;

        public BeatEvent(double beat, double change)
        {
	        this.beat = beat;
	        this.change = change;
        }
        public BeatEvent(string command)
        {
            string[] bits = command.Split('=');
            beat = double.Parse(bits[0], CultureInfo.InvariantCulture);
            change = double.Parse(bits[1], CultureInfo.InvariantCulture);
        }
    }

    public struct BarSection
    {
	    public int StartBar;
	    public int NewBeatsPerBar;

	    public BarSection(int startBar, int newBeatsPerBar)
	    {
		    StartBar = startBar;
		    NewBeatsPerBar = newBeatsPerBar;
	    }
    }
}