using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace TodoApp;

public class TodoItem : INotifyPropertyChanged
{
    private string _title = "";
    private DateTime? _due;
    private bool _hasTime;

    public string Title
    {
        get => _title;
        set { _title = value; OnChanged(nameof(Title)); }
    }

    public DateTime? Due
    {
        get => _due;
        set { _due = value; OnChanged(nameof(Due)); RefreshDue(); }
    }

    // False means the date was given without a time of day.
    public bool HasTime
    {
        get => _hasTime;
        set { _hasTime = value; OnChanged(nameof(HasTime)); RefreshDue(); }
    }

    // Only set when the item is archived.
    public DateTime? CompletedAt { get; set; }

    [JsonIgnore]
    public string DueText => Dates.Format(_due, _hasTime);

    [JsonIgnore]
    public string ArchiveText =>
        "completed on: "
        + (CompletedAt is null ? "not recorded" : Dates.Stamp(CompletedAt.Value))
        + "  ·  due " + DueText;

    // Muted enough not to shout, dark enough to read on white.
    private static readonly Brush SoonBrush = Freeze(Color.FromRgb(0xC2, 0x8A, 0x3C));
    private static readonly Brush OverdueBrush = Freeze(Color.FromRgb(0xBE, 0x5A, 0x5A));

    [JsonIgnore]
    public Brush DueColor => Dates.LevelOf(_due, _hasTime, DateTime.Now) switch
    {
        Dates.Level.Overdue => OverdueBrush,
        Dates.Level.Soon => SoonBrush,
        _ => Brushes.Gray,
    };

    // The colour depends on the current time, so it goes stale if the app is left
    // open. The main window ticks this over periodically.
    public void RefreshDue()
    {
        OnChanged(nameof(DueText));
        OnChanged(nameof(DueColor));
    }

    private static Brush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
