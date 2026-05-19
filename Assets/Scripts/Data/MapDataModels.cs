using System;

public enum NoteType : byte
{
    Tap = 0,
    SlideStart = 1,
    SlideTick = 2,
    SlideEnd = 3,
    Spinner = 4
}

[Serializable]
public struct GameNote
{
    public double Timestamp;
    public byte LaneIndex;
    public NoteType Type;
    public int SlideId;
    public float SlideTargetX;
    public double Duration;
}

public class ConvertedMapData
{
    public GameNote[] Notes;
    public int TotalNotes;

    public double ApproachTime;
}
