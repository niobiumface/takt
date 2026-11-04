---
name: wpf-compact-desktop
description: Compact professional WPF UI conventions for Takt — main window, settings tabs, tray, themes, and German/English resources. Use when editing XAML, ViewModels, tray, settings layout, colors, typography, or localization.
---

# WPF compact desktop

Takt should feel like a small professional utility, not a game overlay and not a settings dump.

## Main window

- Compact (about 520×700 once the typer card is present), no giant dashboard
- Two equal cards on the first row: Jiggler | Clicker. Typer is a full-width card under them. Below about 500 px the cards stack in one column
- A running card uses an accent stripe, a paused card an amber stripe. The clicker card shows button, action, and repeat on one line
- Each card: accent stripe and status pill while running, amber while paused, primary toggle, the live controls, hotkey hint
- Path icons for stop and settings. No emoji glyphs
- Themed checkbox, slider, scrollbar, and an accent focus ring
- Dark or light title bar via `DWMWA_USE_IMMERSIVE_DARK_MODE`, matching the theme
- Gear opens Settings; closing the window hides to tray if that option is on, otherwise exit
- Optional always-on-top from settings

## Settings

Put density behind a gear and the tray menu. The settings window uses a left sidebar (general, hotkeys, clicker, jiggler and presence, working hours, typer, notifications) and a search box that filters those sections. Validate and clamp in the settings normalizer. Bind two-way to `AppSettings` so engines see changes live; persist via `SettingsService` debounce. Every value can reset to its default. Profiles store a full copy of the settings.

## Visual language

- Default **dark** theme; light theme available
- Accent color user-selectable (used on toggles, status-running, primary buttons)
- Status: muted = idle, accent = running, amber = paused (idle-wait / schedule)
- Type: Segoe UI; 12–13 px body, 11 px hints, 16–18 px card titles
- Spacing: 8/12/16; corner radius 8 on cards
- No neon, no rainbow, no fake acrylic clutter

Themes live in `Themes/Dark.xaml` and `Themes/Light.xaml`. Swap `MergedDictionaries` at runtime; keep DynamicResource for colors.

## Tray

`System.Windows.Forms.NotifyIcon` (enable `UseWindowsForms`). Context: show window, toggle jiggler, toggle clicker, panic stop, exit. Left-click shows the window. Tooltip reflects running state.

## i18n

`Resources/Strings.en.xaml` (default) and `Strings.de.xaml`. Switch by replacing the strings dictionary. UI language defaults to English; German is selectable in Settings → Appearance. Code-behind fallbacks should be English.

## MVVM

`CommunityToolkit.Mvvm` (`ObservableObject`, `RelayCommand`, `ObservableProperty`). Code-behind only for view concerns (hotkey capture focus, tray click, window hide). No business logic in code-behind.
