using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.Assertions;

namespace RhythmEngine.Model.TimingConversion
{
    public class TimingConverter
    {
        public List<TimingSection> BpmSections;

        private TimingSection _forwardCache = null;
        private TimingSection _reverseCache = null;

        public TimingConverter(List<TimingSection> sections)
        {
            BpmSections = sections;
        }

        public static TimingConverter ParseBars(List<BarSection> barSections)
        {
	        //In this context, from = bar, to = beat
	        barSections.Sort((a, b) => a.StartBar.CompareTo(b.StartBar));
	        var barToBeatSections = new List<TimingSection>();
	        double maxBar = 100_000;
	        var lastBarSection = new TimingSection(0, maxBar, 0, maxBar * 4);//default bar section for default Beats Per Bar of 4
	        barToBeatSections.Add(lastBarSection);
	        foreach (var bar in barSections)
	        {
		        double startBeat = lastBarSection.BeatAt(bar.StartBar);
		        double endBeat = (maxBar - bar.StartBar) * bar.NewBeatsPerBar + startBeat;
		        var section = new TimingSection(bar.StartBar, maxBar, startBeat, endBeat);
		        //modify the last section to end here
		        lastBarSection.EndBeat = startBeat;
		        lastBarSection.EndTime = bar.StartBar;

		        Assert.AreApproximatelyEqual(bar.NewBeatsPerBar, (float)section.Slope());

		        barToBeatSections.Add(section);
		        lastBarSection = section;
	        }

	        return new TimingConverter(barToBeatSections);
        }

        public static TimingConverter ParseNotespeed(List<SpeedSection> speedSections, double scrollSpeedMult)
        {
	        speedSections.Sort((a, b) => a.Time.CompareTo(b.Time));
	        double maxTime = 100_000;
	        var lastSection = new TimingSection(0, maxTime, 0, maxTime * scrollSpeedMult);
	        var timeToPosSections = new List<TimingSection>();
	        timeToPosSections.Add(lastSection);
	        foreach (var speed in speedSections)
	        {
		        double startPosition = lastSection.BeatAt(speed.Time);
		        double endPosition = (maxTime - speed.Time) * (speed.SpeedMultiplier * scrollSpeedMult) + startPosition;
		        var section = new TimingSection(speed.Time, maxTime, startPosition, endPosition);
		        //modify the last section to end here
		        lastSection.EndBeat = startPosition;
		        lastSection.EndTime = speed.Time;

		        Assert.AreApproximatelyEqual((float)(speed.SpeedMultiplier * scrollSpeedMult), (float)section.Slope());

		        timeToPosSections.Add(section);
		        lastSection = section;
	        }

	        return new TimingConverter(timeToPosSections);
        }

        public static TimingConverter ParseBpm(IList<BeatEvent> changes, IList<BeatEvent> stops)
        {
            var beatGraph = new List<TimingSection>(changes.Count);

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
                beatGraph.Add(new TimingSection(time, endTime, curBeat, nextBeat));
                time = endTime;
            }

            curBeat = nextBeat;
            curBps = nextBps;
            nextBeat = Math.Pow(10, 5);//last segment: make it practically infinitely long
            double lastEndTime = time + (nextBeat - curBeat) * curBps;

            beatGraph.Add(new TimingSection(time, lastEndTime, curBeat, nextBeat));//insert last segment

            var finalGraph = new List<TimingSection>(changes.Count + stops.Count);
            int beatPos = 1;//array index (first one already used)
            TimingSection currentSegment = beatGraph[0];
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

                TimingSection firstHalf = new TimingSection(currentSegment.StartTime, midTime, currentSegment.StartBeat, beat);
                TimingSection stopSection = new TimingSection(midTime, midTime + delay, beat, beat);
                TimingSection secondHalf = new TimingSection(midTime + delay, currentSegment.EndTime + delay, beat, currentSegment.EndBeat);

                finalGraph.Add(firstHalf);
                finalGraph.Add(stopSection);
                currentSegment = secondHalf;

                timeOffset += delay;
            }
            finalGraph.Add(currentSegment);//add the last piece
            //add any remaining segments
            while(beatPos != changes.Count)
            {
                TimingSection seg = beatGraph[beatPos++];
                seg.StartTime += timeOffset;
                seg.EndTime += timeOffset;
                finalGraph.Add(seg);
            }
            return new TimingConverter(finalGraph);
        }

        public double ConvertForward(double time)
        {
            //check if last entry is valid for efficiency
            if(_forwardCache != null && _forwardCache.EndTime >= time && _forwardCache.StartTime <= time)
                return _forwardCache.BeatAt(time);//reuse cache

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
                        _forwardCache = BpmSections[min];
                        return BpmSections[min].BeatAt(time);
                    }
                    else
                    {
                        _forwardCache = BpmSections[max];
                        return BpmSections[max].BeatAt(time);
                    }
                }
                TimingSection mid = BpmSections[midpt];
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
                    _forwardCache = mid;
                    return mid.BeatAt(time);
                }
            }
        }

        public double ConvertReverse(double time)
        {
            //check if last entry is valid for efficiency
            if(_reverseCache != null && _reverseCache.EndBeat >= time && _reverseCache.StartBeat <= time)
                return _reverseCache.TimeAt(time);//reuse cache

            //(trivial) edge case
            if(BpmSections.Count == 1)
                return BpmSections[0].TimeAt(time);

            //binary search
            int min = 0;
            int max = BpmSections.Count - 1;

            while(true)//will break when found
            {
                int midpt = (min + max) / 2;
                if(max - min == 1)//right next to each other
                {
                    if(BpmSections[min].BeatInBounds(time))
                    {
                        _reverseCache = BpmSections[min];
                        return BpmSections[min].TimeAt(time);
                    }
                    else
                    {
                        _reverseCache = BpmSections[max];
                        return BpmSections[max].TimeAt(time);
                    }
                }
                TimingSection mid = BpmSections[midpt];

                if(mid.EndBeat > time)
                {
                    max = midpt;
                }
                else if(mid.StartBeat < time)
                {
                    min = midpt;
                }
                else
                {
                    _reverseCache = mid;
                    return mid.TimeAt(time);
                }
            }
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

    public struct SpeedSection
    {
	    public double Time;//converted from bar number/tick
	    public double SpeedMultiplier;
    }
}