---
name: wpf-compact-desktop
description: Compact professional WPF UI conventions for Takt — main window, settings tabs, tray, themes, and German/English resources. Use when editing XAML, ViewModels, tray, settings layout, colors, typography, or localization.
---

# WPF compact desktop

Takt should feel like a small professional utility, not a game overlay and not a settings dump.

## Main window

- Compact (~420×320), no giant dashboard
- Two equal cards: Jiggler | Clicker
- Each card: status dot + label, primary toggle, one or two live controls (mode or interval), hotkey hint
- Gear opens Settings; closing the window hides to tray if that option is on, otherwise exit
- Optional always-on-top from settings

## Settings

Put density behind a gear. Tabs: Jiggler, Clicker, Hotkeys, Appearance, General. Validate numeric ranges in the ViewModel. Bind two-way to `AppSettings`; persist via `SettingsService` debounce.

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
