using System;

namespace QuizManagement.BUS
{
    public static class ExamShareLinkService
    {
        public const string LegacyScheme = "quiz";
        public const string LegacyPrefix = "quiz://exam/";

        public static string BuildLegacyLink(string roomCode)
        {
            return LegacyPrefix + (roomCode ?? "").Trim();
        }

        public static string ExtractRoomCode(string input)
        {
            string value = (input ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return "";

            if (value.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
                value = value.Substring(LegacyPrefix.Length);
            else if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
            {
                string[] segments = uri.AbsolutePath.Trim('/').Split('/');
                if (segments.Length >= 2 && segments[0].Equals("exam", StringComparison.OrdinalIgnoreCase))
                    value = Uri.UnescapeDataString(segments[1]);
            }

            return value.Trim().TrimEnd('/');
        }

        public static bool IsExamLink(string input)
        {
            string value = (input ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (value.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase)) return true;
            return Uri.TryCreate(value, UriKind.Absolute, out Uri uri)
                && uri.AbsolutePath.StartsWith("/exam/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
