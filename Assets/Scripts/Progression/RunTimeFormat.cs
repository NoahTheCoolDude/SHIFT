using System.Globalization;
using UnityEngine;

namespace Shift.Progression
{
    /// <summary>Speedrun-convention time formatting. Hundredths, because that is what runners compare.</summary>
    public static class RunTimeFormat
    {
        public const string Unreached = "—";

        public static string Format(float seconds)
        {
            if (seconds < 0f) return Unreached;

            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - minutes * 60f;

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00.00}", minutes, remainder);
        }

        /// <summary>Signed difference against a reference time, e.g. "-1.23" when ahead.</summary>
        public static string Delta(float seconds, float reference)
        {
            if (seconds < 0f || reference < 0f) return string.Empty;

            float delta = seconds - reference;
            string sign = delta < 0f ? "-" : "+";

            return sign + Mathf.Abs(delta).ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
