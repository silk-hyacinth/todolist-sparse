using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TodoApp;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TodoItem> _items = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(10) };

    public MainWindow()
    {
        InitializeComponent();
        foreach (var item in Sorted(Storage.Load(Storage.TodoFile)))
            _items.Add(item);
        List.DataContext = _items;
        UpdateNote();

        // Due-date colours depend on the clock, so nudge them along if the app
        // is left open across a deadline.
        _clock.Tick += (_, _) => { foreach (var item in _items) item.RefreshDue(); };
        _clock.Start();
    }

    private static IEnumerable<TodoItem> Sorted(IEnumerable<TodoItem> items) =>
        items.OrderBy(i => i.Due ?? DateTime.MaxValue).ThenBy(i => i.Title);

    // Keeps the list in due-date order without rebuilding it.
    private void InsertSorted(TodoItem item)
    {
        var all = Sorted(_items.Append(item)).ToList();
        _items.Insert(all.IndexOf(item), item);
    }

    private void Resort()
    {
        var sorted = Sorted(_items).ToList();
        _items.Clear();
        foreach (var item in sorted) _items.Add(item);
    }

    private void Add_Click(object sender, RoutedEventArgs e) => AddItem();

    private void NewEntry_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddItem();
    }

    private void AddItem()
    {
        var title = NewTitle.Text.Trim();
        if (title.Length == 0) return;

        if (!Dates.TryParseTime(NewTime.Text, out var time))
        {
            MessageBox.Show(this, "Couldn't read that time. Try something like 14:30.", "Todo");
            return;
        }

        // A time on its own has nothing to hang off, so it needs a date too.
        if (time is not null && NewDue.SelectedDate is null)
        {
            MessageBox.Show(this, "Pick a date to go with that time.", "Todo");
            return;
        }

        InsertSorted(new TodoItem
        {
            Title = title,
            Due = Dates.Combine(NewDue.SelectedDate, time),
            HasTime = time is not null,
        });

        NewTitle.Clear();
        NewTime.Clear();
        NewDue.SelectedDate = null;
        NewTitle.Focus();
        Save();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not TodoItem item) return;

        var dialog = new EditWindow(item) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        Resort();
        Save();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not TodoItem item) return;

        item.CompletedAt = DateTime.Now;
        _items.Remove(item);

        try
        {
            Storage.Append(Storage.ArchiveFile, item);
        }
        catch (Exception ex)
        {
            // Put it back rather than losing it if the archive can't be written.
            item.CompletedAt = null;
            InsertSorted(item);
            MessageBox.Show(this, "Couldn't archive that item:\n" + ex.Message, "Todo");
            return;
        }

        Save();
    }

    private void Archive_Click(object sender, RoutedEventArgs e)
    {
        new ArchiveWindow(Restore) { Owner = this }.ShowDialog();
    }

    // Called by the archive window when an item is put back on the list.
    private void Restore(TodoItem item)
    {
        InsertSorted(item);
        Save();
    }

    private void Calendar_Click(object sender, RoutedEventArgs e)
    {
        new CalendarWindow(_items) { Owner = this }.ShowDialog();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    // Clicking anywhere that isn't a row drops the selection.
    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsInsideRow(e.OriginalSource)) List.SelectedItem = null;
    }

    private static bool IsInsideRow(object? source)
    {
        var node = source as DependencyObject;
        while (node is not null)
        {
            if (node is ListBoxItem) return true;
            node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        }
        return false;
    }

    private void Save()
    {
        try
        {
            Storage.Save(Storage.TodoFile, _items);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Couldn't save:\n" + ex.Message, "Todo");
        }
        UpdateNote();
    }

    private void UpdateNote() =>
        EmptyNote.Text = $"{_items.Count} open · saved in {Storage.Folder}";
}
