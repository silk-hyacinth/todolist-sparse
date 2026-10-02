using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace TodoApp;

// Every recurring task, including ones done for now. Works on the main
// window's collection directly, and tells it when something changes so it can
// save and refresh.
public partial class RecurringWindow : Window
{
    private readonly ObservableCollection<RecurringTask> _tasks;
    private readonly ListCollectionView _view;
    private readonly Action _onChanged;

    public RecurringWindow(ObservableCollection<RecurringTask> tasks, Action onChanged)
    {
        InitializeComponent();
        _tasks = tasks;
        _onChanged = onChanged;

        // Due ones first; done and not-yet-started ones sink to the bottom.
        // Within each, soonest date first.
        _view = new ListCollectionView(tasks);
        _view.SortDescriptions.Add(new SortDescription(nameof(RecurringTask.IsDue), ListSortDirection.Descending));
        _view.SortDescriptions.Add(new SortDescription(nameof(RecurringTask.DueDate), ListSortDirection.Ascending));
        _view.SortDescriptions.Add(new SortDescription(nameof(RecurringTask.Title), ListSortDirection.Ascending));
        List.ItemsSource = _view;
        UpdateNote();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecurringTask task) return;

        task.LastDone = null;
        Changed();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecurringTask task) return;

        var dialog = new RecurringEditWindow(task) { Owner = this };
        if (dialog.ShowDialog() == true) Changed();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RecurringTask task) return;

        // Archive first, so a failed write leaves the task where it was.
        try
        {
            Storage.Append(Storage.ArchiveFile, task.ToArchive(DateTime.Now));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Couldn't archive that task:\n" + ex.Message, "Recurring tasks");
            return;
        }

        _tasks.Remove(task);
        Changed();
    }

    private void Changed()
    {
        _onChanged();
        _view.Refresh();
        UpdateNote();
    }

    private void UpdateNote()
    {
        var done = _tasks.Count(t => t.State == RecurringState.Done);
        var upcoming = _tasks.Count(t => t.State == RecurringState.Upcoming);
        Note.Text = $"{_tasks.Count} recurring · {done} done for now"
            + (upcoming > 0 ? $" · {upcoming} not started yet" : "");
    }
}
