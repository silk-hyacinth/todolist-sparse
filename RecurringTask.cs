using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TodoApp;

public enum RecurringState { Due, Done, Upcoming }

// A task that comes back on a schedule. Checking it off doesn't archive it; it
// just counts as done until it next comes due.
public class RecurringTask : INotifyPropertyChanged
{
    private string _title = "";
    private Frequency _frequency;
    private DateTime _start;
    private bool _hasTime;
    private DateTime? _lastDone;

    public string Title
    {
        get => _title;
        set { _title = value; OnChanged(nameof(Title)); }
    }

    public Frequency Frequency
    {
        get => _frequency;
        set { _frequency = value; OnChanged(nameof(Frequency)); RefreshStatus(); }
    }

    // When it first comes due; every later occurrence steps on from here. Its
    // time of day is when it comes back each time (midnight if HasTime is off).
    public DateTime Start
    {
        get => _start;
        set { _start = value; OnChanged(nameof(Start)); RefreshStatus(); }
    }

    // False means no time was given: it comes back at midnight, and its date is
    // shown without a time, same as a one-off task.
    public bool HasTime
    {
        get => _hasTime;
        set { _hasTime = value; OnChanged(nameof(HasTime)); RefreshStatus(); }
    }

    // When it was last checked off, or null if never (or un-done).
    public DateTime? LastDone
    {
        get => _lastDone;
        set { _lastDone = value; OnChanged(nameof(LastDone)); RefreshStatus(); }
    }

    // Upcoming before its start. Otherwise it's due from each occurrence
    // until it's checked off, then done until the next one.
    public RecurringState StateAt(DateTime now) =>
        _frequency.LatestOccurrence(_start, now) is not { } due ? RecurringState.Upcoming
        : _lastDone >= due ? RecurringState.Done
        : RecurringState.Due;

    // The next moment it comes due (or starts). Its state can't change before
    // then unless someone checks it off or edits it.
    public DateTime NextOccurrenceAt(DateTime now) => _frequency.NextOccurrence(_start, now);

    // The date to show: when it came due if it's waiting to be done, otherwise
    // when it next comes due.
    public DateTime DueDateAt(DateTime now) => StateAt(now) == RecurringState.Due
        ? _frequency.LatestOccurrence(_start, now)!.Value
        : NextOccurrenceAt(now);

    [JsonIgnore] public RecurringState State => StateAt(DateTime.Now);
    [JsonIgnore] public bool IsDue => State == RecurringState.Due;
    [JsonIgnore] public bool IsDone => State == RecurringState.Done;
    [JsonIgnore] public DateTime DueDate => DueDateAt(DateTime.Now);

    // "Fri 2 Oct 2026, repeats daily" or "Fri 2 Oct 2026, 5:00 AM, repeats daily",
    // in the same date format as one-off tasks.
    [JsonIgnore]
    public string StatusText =>
        Dates.Format(DueDate, _hasTime) + ", repeats " + _frequency.Describe() + State switch
        {
            RecurringState.Done => "  ·  done for now",
            RecurringState.Upcoming => "  ·  not started yet",
            _ => "",
        };

    // All of the above depend on the clock, so whoever is showing them calls
    // this when the clock may have moved on.
    public void RefreshStatus()
    {
        OnChanged(nameof(State));
        OnChanged(nameof(IsDue));
        OnChanged(nameof(IsDone));
        OnChanged(nameof(DueDate));
        OnChanged(nameof(StatusText));
    }

    // Archived as a TodoItem that remembers its schedule, so restoring it from
    // the archive brings it back as the same recurring task, not a one-off.
    public TodoItem ToArchive(DateTime removedAt) =>
        new() { Title = _title, Recurs = _frequency, RecursFrom = _start, CompletedAt = removedAt };

    // A start at exactly midnight reads the same as one with no time.
    public static RecurringTask FromArchive(TodoItem item) => new()
    {
        Title = item.Title,
        Frequency = item.Recurs ?? Frequency.Daily,
        Start = item.RecursFrom ?? DateTime.Today,
        HasTime = item.RecursFrom is { TimeOfDay.Ticks: > 0 },
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
