using System;

namespace Shift.Progression
{
    /// <summary>One completed run. The shape that will later be POSTed to a leaderboard.</summary>
    [Serializable]
    public class RunRecord
    {
        public string ZoneId;
        public float TotalSeconds;
        public float[] SplitSeconds;
        public string RecordedUtc;

        public RunRecord() { }

        public RunRecord(string zoneId, float totalSeconds, float[] splitSeconds, DateTime recordedUtc)
        {
            ZoneId = zoneId;
            TotalSeconds = totalSeconds;
            SplitSeconds = splitSeconds ?? new float[0];
            RecordedUtc = recordedUtc.ToString("o");
        }

        public float SplitAt(int index)
        {
            return SplitSeconds != null && index >= 0 && index < SplitSeconds.Length
                ? SplitSeconds[index]
                : RunClock.Unreached;
        }
    }

    /// <summary>
    /// File wrapper. JsonUtility cannot serialize a top-level list or dictionary, and
    /// <see cref="Version"/> is here so a schema change is a migration rather than a data loss.
    /// </summary>
    [Serializable]
    public class RunRecordFile
    {
        public int Version = 1;
        public RunRecord[] Records = new RunRecord[0];
    }
}
