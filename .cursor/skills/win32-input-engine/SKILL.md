---
name: win32-input-engine
description: Windows SendInput, idle detection, global hotkeys, failsafe, and timer rules for Takt's mouse jiggler and autoclicker engines. Use when implementing or changing jiggle/click loops, RegisterHotKey, SetThreadExecutionState, coordinates, DPI, or failsafe behavior.
---

# Win32 input engine

All pointer synthesis goes through `SendInput`. Do not use `mouse_event`. Do not drive click/jiggle intervals with `DispatcherTimer`.

## SendInput

- Relative moves: `MOUSEEVENTF_MOVE` (and `MOUSEEVENTF_MOVE_NOCOALESCE` when available)
- Clicks: separate DOWN then UP events; double-click = two pairs with a short gap
- Hold mode: DOWN on start, UP on stop — never leave the button stuck on cancel/panic
- Absolute: `MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK`, coordinates 0–65535 over the **virtual screen** (`SM_XVIRTUALSCREEN` / `SM_CXVIRTUALSCREEN`), not primary-monitor pixels

Normalize: `x65535 = (pixelX - virtualLeft) * 65535 / max(virtualWidth - 1, 1)`

## Jiggler

| Mode | Behavior |
|------|----------|
| Nudge | +amp then −amp (restore) |
| Circle | points on a circle of radius amp |
| Figure8 | lemniscate around origin |
| Random | small random delta, clamp to amp |
| Invisible | no cursor move; `SetThreadExecutionState` only |

`SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | optional ES_DISPLAY_REQUIRED)`. On stop: `ES_CONTINUOUS` alone.

Optional: wait until `GetLastInputInfo` idle exceeds threshold; pause while the user is actively inputting; skip visible moves when a fullscreen window is foreground.

## Clicker

- Interval in milliseconds on a background loop (`PeriodicTimer` or `TimeProvider` + delay)
- Jitter: delay = interval ± random(0..jitter), never below 1 ms
- Burst: N clicks, then pause interval
- Repeat: infinite, click count, or wall-clock duration
- Position: current cursor, or absolute virtual-desktop XY
- Start delay before the first click
- Never block the WPF dispatcher thread

## Hotkeys

Use `RegisterHotKey` on the main window `HwndSource` hook (`WM_HOTKEY`). Unregister on close/rebind. Panic hotkey stays registered whenever the app runs. `RegisterHotKey` fails on collisions — surface that in the UI, do not crash.

## Failsafe

Poll cursor position on the engine thread (e.g. 50 ms). If the pointer is inside the configured screen corner rectangle (default 4–8 px), stop both engines and release any held buttons.

## Cleanup

On session end: stop engines, mouse UP if holding, `SetThreadExecutionState(ES_CONTINUOUS)`, `UnregisterHotKey` for every id, dispose tray icon.
