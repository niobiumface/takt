# Takt

A small Windows utility that combines a **mouse jiggler**, an **autoclicker**, and a **typer**. Native WPF, no installer noise, no telemetry.

Jiggler, clicker, and typer run independently and can be on at the same time. The UI stays compact; detailed options live in Settings.

Requires Windows 10/11 and the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) for the default build.

## Features

**Jiggler**

- Modes: nudge, circle, figure-8, random, or invisible (sleep/display only)
- Operating radius so the cursor stays inside a circle around the start point or a fixed center
- Set the radius on screen (click center, then edge)
- Interval with optional jitter, amplitude, idle wait, pause on real input, pause on fullscreen, time window
- Prevent system sleep and keep the display on, separately, and only while the jiggler is actually running
- Circle, figure-8, and random stay inside the operating radius. Amplitude applies to nudge only
- Invisible mode nudges one pixel and restores it immediately, so idle timers reset without a visible jump

**Keep presence**

- One switch runs a quiet jiggler on a 30–60 second interval (default 45 s, configurable, clamped to 5–120 s)
- After each nudge, Takt checks that Windows idle time actually reset. If not, it can tap F15 or Shift (keys that type nothing) and shows a warning
- While presence is on, sleep, the display, and the screensaver are asked to stay off. A group-policy lock is not bypassed; Takt says so if that policy exists or the session locks anyway
- Optional working hours, for example Monday–Friday 08:00–17:00
- The typer does not pause presence. Injected keys are not treated as real user input
- A status you set yourself in Teams (Away, Busy, and the rest) is not changed

**Autoclicker**

- Left, right, middle, X1, X2, and wheel
- Single, double, or hold (hold releases the button that was pressed)
- Interval, jitter, burst, start delay
- Repeat forever, N clicks, or a duration
- Click at the cursor, at one picked point, or through a sequence of points. Sequences can be saved by name. Test one click runs a single click and stops
- Sequence points keep their own delay and button. The picker is a crosshair on each monitor; Esc cancels
- Optional sound and tray notice when a run finishes
- A timer can stop every running function after a number of minutes or at a clock time. The remaining time is in the status line. A count or duration run also shows how many clicks, passes, or seconds are left

**Typer**

- Types text you enter or save as snippets. It does not read the keyboard
- Characters per minute with variation, start delay, and repeat by count or duration
- Unicode input via `SendInput`
- Placeholders `{date}`, `{time}`, `{datetime}`, and `{clipboard}`, with formats in Settings. Test once types the current text a single time
- Import the clipboard or a `.txt` file into the text box. The typer still only sends text you put there

**Macro**

- Records your own pointer and plays it back in the open window
- The editor lists steps so you can edit, delete, reorder, insert a delay or a text step, and set a loop count. Playback speed is 0.5×, 1×, or 2×. Show path draws the recorded points and does not click. Each point keeps an offset from the foreground window at record time and playback follows that window when it is still open. Several named macros can be stored the same way as text snippets
- Save or load that list as a JSON file
- Stops with panic. Nothing is hidden and no target process is chosen. Recording starts only when you start it

**App**

- Global hotkeys (customizable), including while recording a new one. Pause (default `F5`) remembers which functions were running and starts those again. Panic still stops everything
- Tray icon colored by state: idle, paused, clicker, jiggler or presence, and typer
- Compact always-on-top window with a start/stop switch per function, from the main window, the tray, or Ctrl+Shift+M. Its position is remembered
- Named profiles in the same settings file, including optional switching when a foreground app you name is in front
- Command palette with Ctrl+K
- A skippable first-run wizard, reopened from Settings
- Windows 11 Mica and rounded corners when the system supports them, with the normal theme background otherwise
- Local daily stats in `%AppData%\Takt\stats.json` (clicks, typed characters, presence time), a 7-day view, reset, a CSV export, and an opt-out. The last three settings files are kept next to `settings.json` in AppData before each real change. Import brings named profiles along when the file contains them
- A self-check for hotkeys, idle reset, and lock or screensaver policy. Policy is reported and not bypassed
- On a crash, held buttons and keys are released, execution state and the 1 ms timer are restored, the error is logged, and one restart is offered
- Start with Windows, start minimized, always on top
- Dark/light theme and accent color, dark title bar in the dark theme
- English by default; German under Settings → Appearance → Language
- Settings in `%AppData%\Takt\settings.json` (version 2, migrated from older files). Logs in `%AppData%\Takt\logs`

## Settings

The main window keeps two cards side by side, and stacks them when the window is narrower than about 500 px. Pace chips set a jiggler interval, a click interval, or a typing speed in one click. The last seven days are a row of counts. A running or paused card shows a stripe in the accent or amber color. The clicker card states button, action, and repeat. Ctrl+K jumps to an action or to a single setting. Count, duration, and the fixed point are shown only for the mode that uses them, and burst pause has its own field.

The gear button and the tray menu open a settings window. A sidebar lists the sections, and the search box filters them. Changes apply immediately and are saved in `%AppData%\Takt\settings.json` (never next to the exe). Each value has its own reset, Ctrl+Z undoes the last saved change, and the whole file can be exported, imported, or restored after a confirmation. Tooltips explain the controls. If the theme is System, Takt follows a Windows theme change while it is open.

- **General:** light, dark, or Windows theme, accent, German or English, autostart, start minimized, close to tray or exit, always on top, profiles.
- **Hotkeys:** record, clear, or reset each shortcut. Duplicates and shortcuts Windows refuses are shown on the row.
- **Clicker:** interval, jitter percent, button including X1/X2, single or double, hold duration, count, start delay, failsafe corner, precise timing.
- **Jiggler and presence:** interval 5–120 s, movement (invisible nudge, small move, small circle), fallback key F15, Shift, Ctrl, or off, screensaver, display, and sleep separately, pause while you are active.
- **Working hours:** several windows per weekday, days off, and outside those hours pause, stop, or only show a hint. Presence and the jiggler follow it.
- **Typer:** speed, jitter, a pause after punctuation, optional typos that are corrected, start delay, repeat count, placeholder formats, and saved snippets.
- **Macro:** loop count, step list, inserted delays, save and load.
- **Notifications:** end sound, tray notices, log level, open the log folder, update check, undo.
- **Stats and checks:** stop timer, app rules, 7-day stats, self-check.

Profiles such as office and home store a full copy of these settings. Switch them from the main window or from settings. Older settings files are migrated to version 2. A status you set yourself in Teams is not changed, and Takt does not bypass a group-policy lock.
- Optional update check against GitHub releases. It does not run unless you turn it on

## Safety

- **Panic stop** (default `F9`) turns jiggler, clicker, typer, and macro off
- Moving the mouse into a corner of the monitor under the pointer (default: top-left, 8 px) does the same and releases a held button
- No admin rights, no process hiding, no game-specific bypass

Takt is a desktop utility. Using an autoclicker in a game or online service may violate that product’s terms.

## Hotkeys

| Action | Default |
| --- | --- |
| Toggle jiggler | `F6` |
| Toggle clicker | `F7` |
| Toggle both | `F8` |
| Panic stop | `F9` |
| Pause and resume | `F5` |
| Pick click position | `F10` |
| Toggle typer | `F11` |
| Play macro | `F12` |
| Compact window | `Ctrl+Shift+M` |

Rebind them in Settings → Hotkeys.

## Run

From source:

```powershell
dotnet run --project src/Takt/Takt.csproj
```

Published exe (after the commands below): `publish/win-x64/Takt.exe`

## Build

```powershell
dotnet build Takt.sln -c Release
dotnet test Takt.sln -c Release
```

## Publish

Framework-dependent single file (small; needs the Desktop Runtime):

```powershell
dotnet publish src/Takt/Takt.csproj -c Release -p:PublishProfile=win-x64
```

Output: `publish/win-x64/Takt.exe`

Self-contained (larger; no runtime install):

```powershell
dotnet publish src/Takt/Takt.csproj -c Release -p:PublishProfile=win-x64-selfcontained
```

Output: `publish/win-x64-selfcontained/Takt.exe`

Close Takt before publishing. Windows will lock `Takt.exe` while it is running.
