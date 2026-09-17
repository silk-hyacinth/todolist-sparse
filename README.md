# Todo

A small WPF todo list for Windows. Items are sorted by due date, checking one off
moves it to an archive file, and nothing leaves the machine.

## Running it

    dotnet run --project TodoApp.csproj

For a copy you can pin to the taskbar:

    dotnet publish -c Release -o app

then make a shortcut to `app\TodoApp.exe`.

## What it does

- Add an item with a title, an optional due date, and an optional time of day.
- The list stays sorted by due date; undated items sit at the bottom.
- The due date under each item turns amber when it's due within a day and muted
  red once it's overdue.
- The checkbox archives an item, stamped with when it was completed.
- **File > Archive** lists everything completed, newest first, and can restore
  an item back to the list.
- **File > Calendar** shows a month grid of what's due each day, with undated
  items listed below.

## Where the data lives

Two JSON files in `%APPDATA%\SimpleTodo`:

- `todos.json` — open items
- `archive.json` — completed items

Set `SIMPLETODO_DIR` to point both somewhere else, which is handy for testing
against throwaway data.

Saves are written to a temp file and moved into place, so an interrupted write
can't truncate the real file. A file that won't parse is renamed aside rather
than crashing the app on launch.
