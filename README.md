# Takt

A small Windows utility that combines a **mouse jiggler** and an **autoclicker**. Native WPF, no installer noise, no telemetry.

Jiggler and clicker run independently and can be on at the same time. The UI stays compact; detailed options live in Settings.

Requires Windows 10/11 and the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) for the default build.

## Features

**Jiggler**

- Modes: nudge, circle, figure-8, random, or invisible (sleep/display only)
- Operating radius so the cursor stays inside a circle around the start point or a fixed center
- Set the radius on screen (click center, then edge)
- Interval with optional jitter, amplitude, idle wait, pause on real input, pause on fullscreen, time window
- Prevent system sleep and keep the display on, separately

**Autoclicker**

- Left / right / middle button
- Single, double, or hold
- Interval, jitter, burst, start delay
- Repeat forever, N clicks, or a duration
- Click at the cursor or at a picked screen position

**App**

- Global hotkeys (customizable)
- Tray icon, start with Windows, start minimized, always on top
- Dark/light theme and accent color
- English by default; German under Settings → Appearance → Language
- Settings in `%AppData%\Takt\settings.json`

## Safety

- **Panic stop** (default `F9`) turns both engines off
- Moving the mouse into a screen corner (default: top-left, 8 px) does the same and releases a held button
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
