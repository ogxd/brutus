using System;

namespace brutus
{
    public static class Extensions
    {
        public static string TruncateIfTooLong(this string str, int THRESHOLD)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > THRESHOLD)
                return str.Substring(0, THRESHOLD) + "...";

            return str;
        }

        private static Random _random = new Random();

        public static Random Random => _random;
    }
}