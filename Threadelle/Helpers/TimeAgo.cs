namespace Threadelle.Helpers
{
    public static class TimeAgo
    {
        public static string From(DateTime utc)
        {
            var span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)}w ago";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)}mo ago";
            return utc.ToLocalTime().ToString("dd MMM yyyy");
        }
    }
}
