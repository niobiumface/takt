---
name: takt-architecture
description: Product architecture, folder layout, feature map, and conventions for Takt, the Windows mouse jiggler, autoclicker, and typer WPF app. Use whenever working in this repo — adding features, changing settings, engines, UI, tests, publish, or deciding what belongs in the product.
---

# Takt architecture

Takt is a native **C# / WPF / .NET 8** (`net8.0-windows`) desktop utility. The repo folder is `mouse`; the product and assembly name is **Takt**. Do not introduce Electron, Python, WinUI, Avalonia, or extra UI frameworks.

## Stack

- WPF + `CommunityToolkit.Mvvm` only (plus framework refs such as `UseWindowsForms` for `NotifyIcon`)
- Settings JSON at `%AppData%\Takt\settings.json` (not next to the exe)
- Single instance via named mutex `Takt.SingleInstance`
- No admin rights, no telemetry, no stealth

## Layout

```
src/Takt/                 # WPF app
  Models/                 # POCO settings + enums
  Native/                 # P/Invoke only
  Services/               # engines, hotkeys, tray, persist, theme, i18n
  ViewModels/
  Views/                  # Settings, position picker
  Resources/              # Strings.de.xaml, Strings.en.xaml
  Themes/                 # Dark.xaml, Light.xaml
src/Takt.Tests/           # logic tests, no UI
```

Engines must not reference WPF controls. ViewModels talk to engines; Views bind to ViewModels.

## Feature map

**In scope:** independent jiggler, autoclicker, typer, and keep-presence (can run together), a visible mouse macro of the user's own pointer, click sequences, named profiles in the AppData settings file, global hotkeys, tray, failsafe corner on the monitor under the pointer, English default UI with German in settings, dark/light + accent, autostart (HKCU Run), always-on-top, start minimized, opt-in update check. Keep-presence resets system idle time on a short interval and does not override a status the user set in Teams.

**Out of scope:** process hiding, anti-cheat bypass, targeting a specific game window to evade detection, keyloggers, sending input to other sessions, portable-next-to-exe settings. The typer only plays text the user typed into Takt.

## Engine interaction

If the clicker uses **cursor position** and the jiggler is in a **visible** move mode, the cursor will drift. When both run and click position is `CurrentCursor`, the jiggler should prefer **Invisible** execution-state mode (or pause visible motion) so clicks stay on target. Fixed XY click positions are unaffected.

## Safety

- Panic hotkey and failsafe corner stop jiggler, clicker, typer, and macro
- `SetThreadExecutionState` is applied on the UI thread and only while the jiggler is effectively running (not while a schedule, fullscreen, or user input has paused it)
- Random jiggle moves anywhere inside the operating radius. Amplitude is only the nudge distance, and it is capped by that radius
- Restore `SetThreadExecutionState` and unregister hotkeys on exit
- One mutex; a second launch focuses the existing window

## Persistence and publish

- Autosave settings on change (debounce ~400 ms)
- `dotnet publish -c Release -r win-x64 --self-contained false` is the default small build
- Document self-contained as an optional larger build
- DPI is `PerMonitorV2` via `ApplicationHighDpiMode` in the csproj and `SetHighDpiMode` before the first window. The .NET 8 SDK warns (WFAC010) if `app.manifest` repeats that declaration, so the manifest does not duplicate it. Pickers are one window per monitor, sized with that monitor's DPI

## Manual check

After UI or input changes: start the app, toggle jiggler (visible + invisible), toggle clicker, type a short snippet, play a short macro, change a hotkey, panic-stop, failsafe corner on the monitor under the pointer, minimize to tray, restart and confirm settings, check a second monitor / DPI if possible.

For Win32 input details see [win32-input-engine](../win32-input-engine/SKILL.md). For UI see [wpf-compact-desktop](../wpf-compact-desktop/SKILL.md).
