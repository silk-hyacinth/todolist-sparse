using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TodoApp;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TodoItem> _items = new();
    private readonly ObservableCollection<RecurringTask> _recurring = new();

    // The recurring tasks currently due; these sit at the top of the list.
    private readonly ListCollectionView _recurringDue;

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(10) };
    private DateTime _today = DateTime.Today;

    public MainWindow()
    {
        InitializeComponent();

        // Say so in the title when working on some other folder (e.g. test data),
        // so a test window can't be mistaken for the real list.
        if (!Storage.IsDefaultFolder)
            Title += " — " + Path.GetFileName(Path.TrimEndingDirectorySeparator(Storage.Folder));
        foreach (var item in Sorted(Storage.Load<TodoItem>(Storage.TodoFile)))
            _items.Add(item);
        foreach (var task in Storage.Load<RecurringTask>(Storage.RecurringFile))
        {
            // Saved before start dates existed: count from when it was last done.
            if (task.Start == default) task.Start = task.LastDone?.Date ?? DateTime.Today;
            _recurring.Add(task);
        }

        _recurringDue = new ListCollectionView(_recurring)
        {
            Filter = task => ((RecurringTask)task).IsDue,
        };
        _recurringDue.SortDescriptions.Add(new SortDescription(nameof(RecurringTask.DueDate), ListSortDirection.Ascending));
        _recurringDue.SortDescriptions.Add(new SortDescription(nameof(RecurringTask.Title), ListSortDirection.Ascending));

        List.ItemsSource = new CompositeCollection
        {
            new CollectionContainer { Collection = _recurringDue },
            new CollectionContainer { Collection = _items },
        };

        NewFrequency.ItemsSource = Enum.GetValues<Frequency>();
        NewFrequency.SelectedIndex = 0;
        UpdateNote();

        // Due-date colours and recurring resets both depend on the clock, so
        // nudge them along if the app is left open, and check again whenever
        // the window comes back into focus.
        _clock.Tick += (_, _) =>
        {
            foreach (var item in _items) item.RefreshDue();
            CheckDayRollover();
        };
        _clock.Start();
        Activated += (_, _) => CheckDayRollover();
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

    // Recurring tasks come due at midnight, so a checked-off or not-yet-started
    // task can only appear once the date has changed.
    private void CheckDayRollover()
    {
        if (DateTime.Today == _today) return;
        _today = DateTime.Today;
        RefreshRecurring();
    }

    // Re-applies the due filter and sort after anything changes.
    private void RefreshRecurring()
    {
        foreach (var task in _recurring) task.RefreshStatus();
        _recurringDue.Refresh();
        UpdateNote();
    }

    private void Add_Click(object sender, RoutedEventArgs e) => AddItem();

    private void NewEntry_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddItem();
    }

    private void NewRecurring_Changed(object sender, RoutedEventArgs e)
    {
        var recurring = NewRecurring.IsChecked == true;
        NewTime.Visibility = recurring ? Visibility.Collapsed : Visibility.Visible;
        NewFrequency.Visibility = recurring ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddItem()
    {
        var title = NewTitle.Text.Trim();
        if (title.Length == 0) return;

        if (NewRecurring.IsChecked == true)
        {
            _recurring.Add(new RecurringTask
            {
                Title = title,
                Frequency = (Frequency)NewFrequency.SelectedItem,
                Start = NewDue.SelectedDate ?? DateTime.Today,
            });
        }
        else
        {
            if (!TryReadDue(out var due, out var hasTime)) return;
            InsertSorted(new TodoItem { Title = title, Due = due, HasTime = hasTime });
        }

        NewTitle.Clear();
        NewTime.Clear();
        NewDue.SelectedDate = null;
        // Back to a one-off, so the next thing typed doesn't repeat by accident.
        NewRecurring.IsChecked = false;
        NewFrequency.SelectedIndex = 0;
        NewTitle.Focus();
        Save();
    }

    private bool TryReadDue(out DateTime? due, out bool hasTime)
    {
        due = null;
        hasTime = false;

        if (!Dates.TryParseTime(NewTime.Text, out var time))
        {
            MessageBox.Show(this, "Couldn't read that time. Try something like 14:30.", "Todo");
            return false;
        }

        // A time on its own has nothing to hang off, so it needs a date too.
        if (time is not null && NewDue.SelectedDate is null)
        {
            MessageBox.Show(this, "Pick a date to go with that time.", "Todo");
            return false;
        }

        due = Dates.Combine(NewDue.SelectedDate, time);
        hasTime = time is not null;
        return true;
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

    // Checking off a recurring task hides it until it's next due; it stays in
    // File > Recurring tasks, greyed out, in the meantime.
    private void RecurringDone_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecurringTask task) return;

        task.LastDone = DateTime.Now;
        RefreshRecurring();
        Save();
    }

    private void RecurringEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecurringTask task) return;

        var dialog = new RecurringEditWindow(task) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        RefreshRecurring();
        Save();
    }

    private void Recurring_Click(object sender, RoutedEventArgs e)
    {
        new RecurringWindow(_recurring, onChanged: () => { RefreshRecurring(); Save(); })
            { Owner = this }.ShowDialog();
    }

    private void Archive_Click(object sender, RoutedEventArgs e)
    {
        new ArchiveWindow(Restore) { Owner = this }.ShowDialog();
    }

    // Called by the archive window when an item is put back on the list.
    // Removed recurring tasks go back to being recurring.
    private void Restore(TodoItem item)
    {
        if (item.Recurs is not null)
            _recurring.Add(RecurringTask.FromArchive(item));
        else
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

    // Both files are tiny, so every change simply writes both.
    private void Save()
    {
        try
        {
            Storage.Save(Storage.TodoFile, _items);
            Storage.Save(Storage.RecurringFile, _recurring);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Couldn't save:\n" + ex.Message, "Todo");
        }
        UpdateNote();
    }

    private void UpdateNote() =>
        EmptyNote.Text = $"{_items.Count + _recurringDue.Count} open · saved in {Storage.Folder}";
}
