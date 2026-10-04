using System.Text.Json.Serialization;

namespace TodoApp;

// How often a recurring task comes back. Saved by name rather than number, so
// adding or reordering members never changes the meaning of existing files.
[JsonConverter(typeof(JsonStringEnumConverter<Frequency>))]
public enum Frequency { Daily, Weekly, Monthly, Yearly }

// A recurring task comes due at its start (a date, and a time of day if it has
// one) and then every step after it: weekly from Wednesday 9am means every
// Wednesday at 9am, monthly from the 15th means every 15th. Occurrences are
// always counted from the start rather than from the previous one, so a start
// on 31 Jan lands on the last day of shorter months and goes back to the 31st
// afterwards instead of drifting.
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

    // The latest time it came due at or before `now`, or null before it starts.
    public static DateTime? LatestOccurrence(this Frequency frequency, DateTime start, DateTime now) =>
        now < start ? null : Occurrence(frequency, start, IndexAt(frequency, start, now));

    // The first time it comes due after `now`, or the start itself if that's later.
    public static DateTime NextOccurrence(this Frequency frequency, DateTime start, DateTime now) =>
        now < start ? start : Occurrence(frequency, start, IndexAt(frequency, start, now) + 1);

    // "daily", "weekly", ... for use in sentences.
    public static string Describe(this Frequency frequency) => frequency.ToString().ToLowerInvariant();

    private static DateTime Occurrence(Frequency frequency, DateTime start, int n)
    {
        var (unit, count) = Step(frequency);
        return unit == Unit.Days ? start.AddDays(count * n) : start.AddMonths(count * n);
    }

    // Which occurrence `now` falls in, for a moment at or after the start.
    private static int IndexAt(Frequency frequency, DateTime start, DateTime now)
    {
        var (unit, count) = Step(frequency);
        var n = unit == Unit.Days
            ? (now.Date - start.Date).Days / count
            : ((now.Year - start.Year) * 12 + now.Month - start.Month) / count;

        // That only counts whole days or months, so it can be one too far: 2am
        // still belongs to yesterday's occurrence of something due daily at 5am,
        // and monthly from the 20th, the 10th still belongs to last month's.
        return n > 0 && Occurrence(frequency, start, n) > now ? n - 1 : n;
    }
}

// One entry in a "how often" menu. Frequency is null for a one-off task. It's a
// record, so two options for the same frequency count as equal, which is what
// lets a menu select the option matching a task.
public sealed record RepeatOption(Frequency? Frequency)
{
    public static RepeatOption Once { get; } = new((Frequency?)null);

    public static List<RepeatOption> Frequencies =>
        Enum.GetValues<Frequency>().Select(f => new RepeatOption(f)).ToList();

    public override string ToString() => Frequency?.Describe() ?? "once";
}
