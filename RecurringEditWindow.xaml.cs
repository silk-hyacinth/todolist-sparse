using System.Windows;

namespace TodoApp;

public partial class RecurringEditWindow : Window
{
    private readonly RecurringTask _task;

    public RecurringEditWindow(RecurringTask task)
    {
        InitializeComponent();
        _task = task;
        TitleBox.Text = task.Title;
        FrequencyBox.ItemsSource = Enum.GetValues<Frequency>();
        FrequencyBox.SelectedItem = task.Frequency;
        StartBox.SelectedDate = task.Start;
        TitleBox.Focus();
        TitleBox.SelectAll();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        if (title.Length == 0)
        {
            MessageBox.Show(this, "Give it a title.", "Edit recurring task");
            return;
        }

        if (StartBox.SelectedDate is not { } start)
        {
            MessageBox.Show(this, "Pick the day it starts.", "Edit recurring task");
            return;
        }

        _task.Title = title;
        _task.Frequency = (Frequency)FrequencyBox.SelectedItem;
        _task.Start = start;
        DialogResult = true;
    }
}
