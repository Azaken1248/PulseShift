public static class MappingConverter
{
    private const float OsuWidth = 512f;
    private const byte MaxLaneIndex = 3;

    public static byte GetLaneIndex(float osuX)
    {
        if (osuX <= 0f)
        {
            return 0;
        }

        if (osuX >= OsuWidth)
        {
            return MaxLaneIndex;
        }

        int laneIndex = (int)((osuX / OsuWidth) * 4f);
        if (laneIndex < 0)
        {
            laneIndex = 0;
        }
        else if (laneIndex > MaxLaneIndex)
        {
            laneIndex = MaxLaneIndex;
        }

        return (byte)laneIndex;
    }
}