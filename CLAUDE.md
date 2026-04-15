# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build
dotnet run
```

This is a .NET 10 WPF application targeting `net10.0-windows10.0.19041.0`. No test projects exist.

## Architecture

Single-project WPF desktop app — a break reminder that runs in the system tray and periodically shows a notification window.

**App.xaml.cs** is the central controller. It owns:
- `DispatcherTimer` for scheduling reminders (default: 45 minutes)
- Taskbar notification icon (via Hardcodet.NotifyIcon.Wpf) with context menu
- Settings persistence to `%LocalAppData%\BreakReminder\settings.json` (JSON with `IntervalMinutes` key)
- Windows auto-startup via registry (`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`)

**MainWindow** — Settings UI. Uses a `DependencyProperty` for two-way binding to the interval value. Hides on close (does not exit app).

**ReminderWindow** — Modal notification shown on timer tick. Plays system sound, always-on-top.

This is **not MVVM** — App.xaml.cs acts as both model and controller with event-handler-based UI code.

## Dependencies

- **Hardcodet.NotifyIcon.Wpf** (2.0.1) — system tray icon
- **Microsoft.Toolkit.Uwp.Notifications** (7.1.3) — Windows toast notifications

## Localization

UI text is hardcoded in Traditional Chinese with some bilingual (Chinese/English) strings in the taskbar tooltip and context menu.
