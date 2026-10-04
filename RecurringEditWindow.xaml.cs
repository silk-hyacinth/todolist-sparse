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
        FrequencyBox.ItemsSource = RepeatOption.Frequencies;
        FrequencyBox.SelectedItem = new RepeatOption(task.Frequency);
        StartBox.SelectedDate = task.Start.Date;
        TimeBox.Text = Dates.TimeText(task.Start, task.HasTime);
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

        if (!Dates.TryParseTime(TimeBox.Text, out var time))
        {
            MessageBox.Show(this, "Couldn't read that time. Try something like 5:00.", "Edit recurring task");
            return;
        }

        _task.Title = title;
        _task.Frequency = ((RepeatOption)FrequencyBox.SelectedItem).Frequency!.Value;
        _task.Start = Dates.Combine(start, time)!.Value;
        _task.HasTime = time is not null;
        DialogResult = true;
    }
}
