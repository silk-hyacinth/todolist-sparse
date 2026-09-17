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

## where to store the data

Two JSON files in `%APPDATA%\SimpleTodo`:

- `todos.json` — open items
- `archive.json` — completed items

if you want it somewhere else, set `SIMPLETODO_DIR` to point somewhere else.
