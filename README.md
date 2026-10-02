# Todo

fully local todo app without all the ads and email and sign in and laggy styling et cetera.

## how to run

    dotnet run --project TodoApp.csproj

For a copy you can pin to the taskbar:

    dotnet publish -c Release -o app

then make a shortcut to `app\TodoApp.exe`.

## How to use:
Shows all tasks in a simple list with due dates. Blue tasks are recurring, orange are near due, red are overdue. Press the checkbox on the left to mark a task as done; if it is a one time task checking it off will get moved to the archive, if it is recurring it will come back after the interval you set it to come back at (can archive recurring tasks in File -> Recurring tasks).

<img width="359" height="392" alt="Screenshot 2026-10-02 162318" src="https://github.com/user-attachments/assets/41c198dc-49fe-4608-9612-67d3496fff15" />

To set a date and time, select a date in the calendar and type the time in the box to its right. If you don't want a due date and/or time, just leave the respective fields blank. Click the repeat box to make the task recurring.

<img width="344" height="144" alt="Screenshot 2026-10-02 161826" src="https://github.com/user-attachments/assets/99a9a075-20e1-4b61-ac05-0e2323bdbe50" />

File -> Calendar shows a calendar of all tasks

<img width="539" height="464" alt="image" src="https://github.com/user-attachments/assets/a16c5e9c-3e26-413c-beff-05e7352ef87d" />

File -> Recurring Tasks shows a list of recurring tasks; here you can delete it (move to archive), edit it, or undo the completion of a task. (normally, pressing the checkmark for a recurring task will gray out the recurring task from this list until the time comes for it to recur, after which time it will come back to your main task list)

<img width="326" height="316" alt="image" src="https://github.com/user-attachments/assets/1f448219-732d-4a29-b3cd-92355b300da9" />

File -> Archive shows all tasks marked as done (including deleted recurring tasks). There is an option to restore tasks from the archive in case the task was mistakenly deleted.

<img width="359" height="394" alt="image" src="https://github.com/user-attachments/assets/71231153-874f-4eef-940a-fdc8b3663337" />

## where the data is stored

Three JSON files in `%APPDATA%\SimpleTodo`:

- `todos.json` — open items
- `recurring.json` — recurring tasks
- `archive.json` — completed items

if you want it somewhere else, set an environment variable `SIMPLETODO_DIR` to point somewhere else.
