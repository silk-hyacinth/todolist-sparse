using System.Globalization;

namespace TodoApp;

// Parsing and display for the optional due date + optional time of day.
public static class Dates
{
    private static readonly string[] TimeFormats =
        { "H:mm", "HH:mm", "h:mm tt", "h:mmtt", "h tt", "htt", "HHmm" };

    // Returns false only when the text is present but unparseable.
    // Empty text is valid and means "no time of day".
    public static bool TryParseTime(string text, out TimeSpan? time)
    {
        time = null;
        text = text.Trim();
        if (text.Length == 0) return true;

        if (DateTime.TryParseExact(text, TimeFormats, CultureInfo.CurrentCulture,
                DateTimeStyles.None, out var dt)
            || DateTime.TryParseExact(text, TimeFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out dt)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture,
                DateTimeStyles.NoCurrentDateDefault, out dt))
        {
            time = dt.TimeOfDay;
            return true;
        }
        return false;
    }

    // Combines the date picker value with a parsed time of day.
    public static DateTime? Combine(DateTime? date, TimeSpan? time) =>
        date is null ? null : date.Value.Date + (time ?? TimeSpan.Zero);

    public static string Format(DateTime? due, bool hasTime)
    {
        if (due is null) return "no due date";
        var date = due.Value.ToString("ddd d MMM yyyy", CultureInfo.CurrentCulture);
        // "t" has to stand alone to mean short time; inside a custom pattern it is
        // just the AM/PM letter.
        return hasTime
            ? date + ", " + due.Value.ToString("t", CultureInfo.CurrentCulture)
            : date;
    }

    public enum Level { Normal, Soon, Overdue }

    public static Level LevelOf(DateTime? due, bool hasTime, DateTime now)
    {
        if (due is null) return Level.Normal;

        // Without a time of day there is no deadline to count 24 hours back from,
        // so compare whole days: due today or tomorrow counts as soon.
        if (!hasTime)
        {
            var days = (due.Value.Date - now.Date).Days;
            if (days < 0) return Level.Overdue;
            return days <= 1 ? Level.Soon : Level.Normal;
        }

        if (due.Value < now) return Level.Overdue;
        return due.Value <= now.AddDays(1) ? Level.Soon : Level.Normal;
    }

    // Shown in the archive: "2 Aug 2026, 10:11 AM".
    public static string Stamp(DateTime when) =>
        when.ToString("d MMM yyyy", CultureInfo.CurrentCulture)
        + ", " + when.ToString("t", CultureInfo.CurrentCulture);

    // Round-trips back into the time text box.
    public static string TimeText(DateTime? due, bool hasTime) =>
        due is not null && hasTime ? due.Value.ToString("HH:mm") : "";
}
