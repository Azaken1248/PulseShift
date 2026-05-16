using System.Globalization;
using System.IO;
using UnityEngine;

public static class BeatmapParser
{
    private const double DefaultApproachTime = 1.5d;

    public static ConvertedMapData Parse(string osuText)
    {
        if (string.IsNullOrEmpty(osuText)) return CreateEmptyMap();

        int tapCount = CountTapCircles(osuText);
        
        if (tapCount == 0)
        {
            Debug.LogWarning("BeatmapParser WARNING: Still found 0 notes. Check the DummyBeatmap string formatting in your Bootstrapper!");
            return CreateEmptyMap();
        }

        GameNote[] notes = new GameNote[tapCount];
        int writeIndex = 0;
        bool inHitObjects = false;

        using (StringReader reader = new StringReader(osuText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim(); 
                
                if (line.Length == 0) continue;

                if (line[0] == '[')
                {
                    if (line == "[HitObjects]") inHitObjects = true;
                    else if (inHitObjects) break; 
                    continue;
                }

                if (!inHitObjects) continue;

                if (TryParseTapCircle(line, out GameNote note))
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
        return new ConvertedMapData { Notes = new GameNote[0], TotalNotes = 0, ApproachTime = DefaultApproachTime };
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
                line = line.Trim(); 
                
                if (line.Length == 0) continue;

                if (line[0] == '[')
                {
                    if (line == "[HitObjects]") inHitObjects = true;
                    else if (inHitObjects) break;
                    continue;
                }

                if (!inHitObjects) continue;

                if (TryParseTapCircle(line, out _))
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
        
        if (parts.Length < 5) return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) return false;
        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeMilliseconds)) return false;
        if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int typeValue)) return false;

        if ((typeValue & 1) == 0) return false;

        note.Timestamp = timeMilliseconds * 0.001d;
        note.LaneIndex = MappingConverter.GetLaneIndex(x);
        note.Type = NoteType.Tap;
        note.SlideId = -1;
        note.SlideTargetX = x;

        return true;
    }
}