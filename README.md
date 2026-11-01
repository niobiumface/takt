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
- Click at the cursor, at one picked point, or through a sequence of points
- Optional sound and tray notice when a run finishes

**Typer**

- Types text you enter or save as snippets. It does not read the keyboard
- Characters per minute with variation, start delay, and repeat by count or duration
- Unicode input via `SendInput`

**Macro**

- Records your own pointer and plays it back in the open window
- Stops with panic. Nothing is hidden and no target process is chosen

**App**

- Global hotkeys (customizable), including while recording a new one
- Tray icon that shows off, running, and paused
- Named profiles in the same settings file
- Start with Windows, start minimized, always on top
- Dark/light theme and accent color, dark title bar in the dark theme
- English by default; German under Settings → Appearance → Language
- Settings in `%AppData%\Takt\settings.json` (versioned). Logs in `%AppData%\Takt\logs`
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
| Pick click position | `F10` |
| Toggle typer | `F11` |
| Play macro | `F12` |

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
