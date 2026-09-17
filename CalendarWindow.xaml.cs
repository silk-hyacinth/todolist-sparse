using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TodoApp;

// One square in the month grid.
public class DayCell
{
    public string DayText { get; init; } = "";
    public List<string> Entries { get; init; } = new();
    public Brush DayColor { get; init; } = Brushes.Black;
    public FontWeight DayWeight { get; init; } = FontWeights.Normal;
}

public partial class CalendarWindow : Window
{
    private readonly List<TodoItem> _items;
    private DateTime _month;

    public CalendarWindow(IEnumerable<TodoItem> items)
    {
        InitializeComponent();
        _items = items.ToList();
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        BuildWeekHeader();
        NoDate.ItemsSource = _items.Where(i => i.Due is null).OrderBy(i => i.Title).ToList();
        Build();
    }

    private static DayOfWeek FirstDay => CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

    private void BuildWeekHeader()
    {
        var names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)FirstDay + i) % 7);
            WeekHeader.Children.Add(new TextBlock
            {
                Text = names[(int)day],
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
        }
    }

    private void Build()
    {
        MonthLabel.Text = _month.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        // Back up to the first cell of the week the 1st falls in, then always draw
        // six weeks so the grid doesn't change height as you page through months.
        var offset = ((int)_month.DayOfWeek - (int)FirstDay + 7) % 7;
        var start = _month.AddDays(-offset);

        var cells = new List<DayCell>();
        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var inMonth = date.Month == _month.Month && date.Year == _month.Year;
            var isToday = date == DateTime.Today;

            cells.Add(new DayCell
            {
                DayText = date.Day.ToString(),
                DayColor = inMonth ? Brushes.Black : Brushes.Silver,
                DayWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Entries = _items
                    .Where(item => item.Due is not null && item.Due.Value.Date == date)
                    .OrderBy(item => item.Due!.Value)
                    .Select(Entry)
                    .ToList(),
            });
        }

        Days.ItemsSource = cells;
    }

    private static string Entry(TodoItem item) =>
        item.HasTime
            ? item.Due!.Value.ToString("t", CultureInfo.CurrentCulture) + "  " + item.Title
            : item.Title;

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        _month = _month.AddMonths(-1);
        Build();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        _month = _month.AddMonths(1);
        Build();
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        Build();
    }
}
