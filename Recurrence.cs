using System.Text.Json.Serialization;

namespace TodoApp;

// How often a recurring task comes back. Saved by name rather than number, so
// adding or reordering members never changes the meaning of existing files.
[JsonConverter(typeof(JsonStringEnumConverter<Frequency>))]
public enum Frequency { Daily, Weekly, Monthly, Yearly }

// A recurring task comes due on its start date and then every step after it:
// weekly from a Wednesday means every Wednesday, monthly from the 15th means
// every 15th. Occurrences are always counted from the start rather than from
// the previous one, so a start on 31 Jan lands on the last day of shorter
// months and goes back to the 31st afterwards instead of drifting.
public static class Recurrence
{
    private enum Unit { Days, Months }

    // To add a frequency, add the enum member and its step here.
    private static (Unit, int) Step(Frequency frequency) => frequency switch
    {
        Frequency.Daily => (Unit.Days, 1),
        Frequency.Weekly => (Unit.Days, 7),
        Frequency.Monthly => (Unit.Months, 1),
        Frequency.Yearly => (Unit.Months, 12),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null),
    };

    // The latest time it came due on or before `day`, or null before it starts.
    public static DateTime? LatestOccurrence(this Frequency frequency, DateTime start, DateTime day)
    {
        start = start.Date;
        day = day.Date;
        return day < start ? null : Occurrence(frequency, start, IndexAt(frequency, start, day));
    }

    // The first time it comes due after `day`, or the start itself if that's later.
    public static DateTime NextOccurrence(this Frequency frequency, DateTime start, DateTime day)
    {
        start = start.Date;
        day = day.Date;
        return day < start ? start : Occurrence(frequency, start, IndexAt(frequency, start, day) + 1);
    }

    // "daily", "weekly", ... for use in sentences.
    public static string Describe(this Frequency frequency) => frequency.ToString().ToLowerInvariant();

    private static DateTime Occurrence(Frequency frequency, DateTime start, int n)
    {
        var (unit, count) = Step(frequency);
        return unit == Unit.Days ? start.AddDays(count * n) : start.AddMonths(count * n);
    }

    // Which occurrence `day` falls in, for a day on or after the start.
    private static int IndexAt(Frequency frequency, DateTime start, DateTime day)
    {
        var (unit, count) = Step(frequency);
        var n = unit == Unit.Days
            ? (day - start).Days / count
            : ((day.Year - start.Year) * 12 + day.Month - start.Month) / count;

        // Counting whole months ignores the day of the month, so this can be one
        // too far: monthly from the 20th, the 10th of a later month still belongs
        // to the previous occurrence.
        return n > 0 && Occurrence(frequency, start, n) > day ? n - 1 : n;
    }
}
