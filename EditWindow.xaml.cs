using System.Windows;

namespace TodoApp;

public partial class EditWindow : Window
{
    private readonly TodoItem _item;

    public EditWindow(TodoItem item)
    {
        InitializeComponent();
        _item = item;
        TitleBox.Text = item.Title;
        DueBox.SelectedDate = item.Due;
        TimeBox.Text = Dates.TimeText(item.Due, item.HasTime);
        TitleBox.Focus();
        TitleBox.SelectAll();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        DueBox.SelectedDate = null;
        TimeBox.Clear();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        if (title.Length == 0)
        {
            MessageBox.Show(this, "Give it a title.", "Edit item");
            return;
        }

        if (!Dates.TryParseTime(TimeBox.Text, out var time))
        {
            MessageBox.Show(this, "Couldn't read that time. Try something like 14:30.", "Edit item");
            return;
        }

        // A time on its own has nothing to hang off, so it needs a date too.
        if (time is not null && DueBox.SelectedDate is null)
        {
            MessageBox.Show(this, "Pick a date to go with that time.", "Edit item");
            return;
        }

        _item.Title = title;
        _item.Due = Dates.Combine(DueBox.SelectedDate, time);
        _item.HasTime = time is not null;
        DialogResult = true;
    }
}
