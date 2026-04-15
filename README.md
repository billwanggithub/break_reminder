# Break Reminder 休息提醒小幫手

A Windows desktop application that reminds you to take breaks at regular intervals. Runs quietly in the system tray and pops up a notification when it's time to rest.

## Features

- System tray icon with double-click to open settings
- Configurable reminder interval (default: 45 minutes)
- Always-on-top notification window with sound alert
- Auto-start with Windows (via registry)
- Settings persisted to `%LocalAppData%\BreakReminder\settings.json`

## Requirements

- Windows 10 (1809) or later
- .NET 10 SDK

## Build & Run

```bash
dotnet build
dotnet run
```

## Usage

1. Launch the app — it minimizes to the system tray
2. Double-click the tray icon to open settings
3. Set your desired reminder interval in minutes
4. Click "儲存並隱藏" (Save and Hide)
5. When the timer fires, a reminder window appears — take a break!

Right-click the tray icon for quick actions:
- **設定 (Settings)** — open settings window
- **現在休息 (Break Now)** — trigger a reminder immediately
- **結束 (Exit)** — quit the application
