using System.Collections.ObjectModel;
using System.Windows;

namespace TodoApp;

public partial class ArchiveWindow : Window
{
    private readonly ObservableCollection<TodoItem> _archived = new();
    private readonly Action<TodoItem> _onRestore;

    public ArchiveWindow(Action<TodoItem> onRestore)
    {
        InitializeComponent();
        _onRestore = onRestore;

        // Most recently completed first.
        foreach (var item in Storage.Load(Storage.ArchiveFile)
                     .OrderByDescending(i => i.CompletedAt ?? DateTime.MinValue))
            _archived.Add(item);

        List.ItemsSource = _archived;
        UpdateNote();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not TodoItem item) return;

        _archived.Remove(item);
        try
        {
            Storage.Save(Storage.ArchiveFile, _archived);
        }
        catch (Exception ex)
        {
            _archived.Add(item);
            MessageBox.Show(this, "Couldn't update the archive:\n" + ex.Message, "Archive");
            return;
        }

        item.CompletedAt = null;
        _onRestore(item);
        UpdateNote();
    }

    private void UpdateNote() => Note.Text = $"{_archived.Count} archived";
}
