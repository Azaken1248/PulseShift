using System.IO;
using UnityEngine;

public static class BeatmapParser
{
    private const double DefaultApproachTime = 1.5d;
    private const int HoldTickIntervalMilliseconds = 100;

    private struct ParsedHitObject
    {
        public int X;
        public int StartTimeMilliseconds;
        public int EndTimeMilliseconds;
        public bool IsTap;
        public bool IsHold;
    }

    public static ConvertedMapData Parse(TextAsset osuFile)
    {
        if (osuFile == null)
        {
            return CreateEmptyMap();
        }

        return Parse(osuFile.text);
    }

    public static ConvertedMapData Parse(string osuText)
    {
        if (string.IsNullOrEmpty(osuText))
        {
            return CreateEmptyMap();
        }

        int totalNotes = CountConvertedNotes(osuText);
        if (totalNotes <= 0)
        {
            return CreateEmptyMap();
        }

        GameNote[] notes = new GameNote[totalNotes];
        int writeIndex = 0;
        int slideId = 0;
        bool inHitObjects = false;

        using (StringReader reader = new StringReader(osuText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    if (line == "[HitObjects]")
                    {
                        inHitObjects = true;
                    }
                    else if (inHitObjects)
                    {
                        break;
                    }

                    continue;
                }

                if (!inHitObjects)
                {
                    continue;
                }

                ParsedHitObject parsedHitObject;
                if (!TryParseHitObject(line, out parsedHitObject))
                {
                    continue;
                }

                if (parsedHitObject.IsTap)
                {
                    notes[writeIndex] = CreateNote(parsedHitObject.X, parsedHitObject.StartTimeMilliseconds, NoteType.Tap, -1, 0d);
                    writeIndex++;
                    continue;
                }

                if (!parsedHitObject.IsHold)
                {
                    continue;
                }

                double durationSeconds = (parsedHitObject.EndTimeMilliseconds - parsedHitObject.StartTimeMilliseconds) * 0.001d;
                if (durationSeconds < 0d)
                {
                    durationSeconds = 0d;
                }

                notes[writeIndex] = CreateNote(parsedHitObject.X, parsedHitObject.StartTimeMilliseconds, NoteType.SlideStart, slideId, durationSeconds);
                writeIndex++;

                int tickTimeMilliseconds = parsedHitObject.StartTimeMilliseconds + HoldTickIntervalMilliseconds;
                while (tickTimeMilliseconds < parsedHitObject.EndTimeMilliseconds)
                {
                    notes[writeIndex] = CreateNote(parsedHitObject.X, tickTimeMilliseconds, NoteType.SlideTick, slideId, durationSeconds);
                    writeIndex++;
                    tickTimeMilliseconds += HoldTickIntervalMilliseconds;
                }

                notes[writeIndex] = CreateNote(parsedHitObject.X, parsedHitObject.EndTimeMilliseconds, NoteType.SlideEnd, slideId, durationSeconds);
                writeIndex++;

                slideId++;
            }
        }

        return new ConvertedMapData
        {
            Notes = notes,
            TotalNotes = writeIndex,
            ApproachTime = DefaultApproachTime
        };
    }

    private static ConvertedMapData CreateEmptyMap()
    {
        return new ConvertedMapData
        {
            Notes = new GameNote[0],
            TotalNotes = 0,
            ApproachTime = DefaultApproachTime
        };
    }

    private static int CountConvertedNotes(string osuText)
    {
        int count = 0;
        bool inHitObjects = false;

        using (StringReader reader = new StringReader(osuText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    if (line == "[HitObjects]")
                    {
                        inHitObjects = true;
                    }
                    else if (inHitObjects)
                    {
                        break;
                    }

                    continue;
                }

                if (!inHitObjects)
                {
                    continue;
                }

                ParsedHitObject parsedHitObject;
                if (!TryParseHitObject(line, out parsedHitObject))
                {
                    continue;
                }

                if (parsedHitObject.IsTap)
                {
                    count++;
                    continue;
                }

                if (!parsedHitObject.IsHold)
                {
                    continue;
                }

                count += 2;

                int tickTimeMilliseconds = parsedHitObject.StartTimeMilliseconds + HoldTickIntervalMilliseconds;
                while (tickTimeMilliseconds < parsedHitObject.EndTimeMilliseconds)
                {
                    count++;
                    tickTimeMilliseconds += HoldTickIntervalMilliseconds;
                }
            }
        }

        return count;
    }

    private static bool TryParseHitObject(string line, out ParsedHitObject parsedHitObject)
    {
        parsedHitObject = new ParsedHitObject();

        string[] parts = line.Split(',');
        if (parts.Length < 5)
        {
            return false;
        }

        int x;
        int startTimeMilliseconds;
        int typeValue;

        if (!int.TryParse(parts[0], out x))
        {
            return false;
        }

        if (!int.TryParse(parts[2], out startTimeMilliseconds))
        {
            return false;
        }

        if (!int.TryParse(parts[3], out typeValue))
        {
            return false;
        }

        parsedHitObject.X = x;
        parsedHitObject.StartTimeMilliseconds = startTimeMilliseconds;
        parsedHitObject.IsHold = (typeValue & 128) != 0;
        parsedHitObject.IsTap = !parsedHitObject.IsHold && (typeValue & 1) != 0;

        if (!parsedHitObject.IsTap && !parsedHitObject.IsHold)
        {
            return false;
        }

        if (parsedHitObject.IsHold)
        {
            if (parts.Length < 6)
            {
                return false;
            }

            int colonIndex = parts[5].IndexOf(':');
            string endTimeText = colonIndex >= 0 ? parts[5].Substring(0, colonIndex) : parts[5];

            int endTimeMilliseconds;
            if (!int.TryParse(endTimeText, out endTimeMilliseconds))
            {
                return false;
            }

            parsedHitObject.EndTimeMilliseconds = endTimeMilliseconds;
        }
        else
        {
            parsedHitObject.EndTimeMilliseconds = startTimeMilliseconds;
        }

        return true;
    }

    private static GameNote CreateNote(int x, int timeMilliseconds, NoteType noteType, int slideId, double durationSeconds)
    {
        GameNote note = new GameNote();
        note.Timestamp = timeMilliseconds * 0.001d;
        note.LaneIndex = MappingConverter.GetLaneIndex(x);
        note.Type = noteType;
        note.SlideId = slideId;
        note.SlideTargetX = x;
        note.Duration = durationSeconds;
        return note;
    }
}