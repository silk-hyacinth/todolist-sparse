# Todo

todo app without all the ads and email and sign in and laggy styling etc.

## how to run

    dotnet run --project TodoApp.csproj

For a copy you can pin to the taskbar:

    dotnet publish -c Release -o app

then make a shortcut to `app\TodoApp.exe`.

## features

- Add an item with a title, an optional due date, and an optional time of day.
- The list stays sorted by due date; undated items sit at the bottom.
- The due date under each item turns amber when it's due within a day and muted
  red once it's overdue.
- The checkbox archives an item, stamped with when it was completed.
- **File > Archive** lists everything completed, newest first, and can restore
  an item back to the list.
- **File > Calendar** shows a month grid of what's due each day, with undated
  items listed below.
- Tick **repeat** when adding to make a recurring task (daily, weekly, monthly
  or yearly). The date box becomes its start date (blank means today), and it
  repeats from there: weekly from a Wednesday means every Wednesday. Due ones
  sit at the top of the list; checking one off hides it until it's next due,
  rather than archiving it.
- **File > Recurring tasks** lists every recurring task, with ones done for now
  or not started yet greyed out at the bottom. From there you can undo a
  check-off, edit, or delete one, which moves it to the archive (restoring it
  brings it back on the same schedule).

## where to store the data

Three JSON files in `%APPDATA%\SimpleTodo`:

- `todos.json` — open items
- `recurring.json` — recurring tasks
- `archive.json` — completed items

if you want it somewhere else, set `SIMPLETODO_DIR` to point somewhere else.
