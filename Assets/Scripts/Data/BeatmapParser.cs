using System.Globalization;
using System.IO;
using UnityEngine;

public static class BeatmapParser
{
    private const double DefaultApproachTime = 1.5d;
    private const int LaneWidth = 128;

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

        int tapCount = CountTapCircles(osuText);
        GameNote[] notes = new GameNote[tapCount];
        int writeIndex = 0;
        bool inHitObjects = false;

        using (StringReader reader = new StringReader(osuText))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
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

                GameNote note;
                if (TryParseTapCircle(line, out note))
                {
                    notes[writeIndex] = note;
                    writeIndex++;
                }
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

    private static int CountTapCircles(string osuText)
    {
        int count = 0;
        bool inHitObjects = false;

        using (StringReader reader = new StringReader(osuText))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
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

                GameNote parsedNote;
                if (TryParseTapCircle(line, out parsedNote))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool TryParseTapCircle(string line, out GameNote note)
    {
        note = new GameNote();

        string[] parts = line.Split(',');
        if (parts.Length < 5)
        {
            return false;
        }

        int x;
        int timeMilliseconds;
        int typeValue;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out timeMilliseconds))
        {
            return false;
        }

        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out typeValue))
        {
            return false;
        }

        if ((typeValue & 1) == 0)
        {
            return false;
        }

        note.Timestamp = timeMilliseconds * 0.001d;
        note.LaneIndex = CalculateLaneIndex(x);
        note.Type = NoteType.Tap;
        note.SlideId = -1;
        note.SlideTargetX = x;

        return true;
    }

    private static byte CalculateLaneIndex(int x)
    {
        int laneIndex = x / LaneWidth;

        if (laneIndex < 0)
        {
            laneIndex = 0;
        }
        else if (laneIndex > 3)
        {
            laneIndex = 3;
        }

        return (byte)laneIndex;
    }
}