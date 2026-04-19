using System.Configuration;
using System.Data;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Reflection;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BreakReminder;

public partial class App : Application
{
    private TaskbarIcon _notifyIcon = null!;
    private DispatcherTimer _timer = null!;
    private DispatcherTimer _scheduleTimer = null!;
    private MainWindow _settingsWindow = null!;
    private string _settingsPath = null!;
    private ReminderWindow? _reminderWindow;
    private readonly Queue<string?> _pendingMessages = new();

    public int ReminderIntervalMinutes { get; set; } = 45;

    public bool PlaySound { get; set; } = false;

    public ObservableCollection<ScheduledReminder> ScheduledReminders { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BreakReminder");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        LoadSettings();

        _settingsWindow = new MainWindow();

        _notifyIcon = new TaskbarIcon();

        string? exePath = Environment.ProcessPath;
        if (exePath != null)
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        _notifyIcon.ToolTipText = "休息提醒小幫手 (雙擊開啟設定)";
        _notifyIcon.DoubleClickCommand = new RelayCommand(ShowSettings);

        ContextMenu contextMenu = new ContextMenu();

        MenuItem settingsItem = new MenuItem { Header = "設定 (Settings)" };
        settingsItem.Click += (s, ev) => ShowSettings();

        MenuItem breakNowItem = new MenuItem { Header = "立刻休息 (Break Now)" };
        breakNowItem.Click += (s, ev) => ShowNotification(null);

        MenuItem exitItem = new MenuItem { Header = "離開 (Exit)" };
        exitItem.Click += (s, ev) => Application.Current.Shutdown();

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(breakNowItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);
        _notifyIcon.ContextMenu = contextMenu;

        SetStartup(true);

        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
        UpdateTimer();

        _scheduleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _scheduleTimer.Tick += ScheduleTimer_Tick;
        _scheduleTimer.Start();
    }

    private void LoadSettings()
    {
        bool hasScheduledKey = false;

        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("IntervalMinutes", out var intervalEl) && intervalEl.TryGetInt32(out int minutes))
                {
                    ReminderIntervalMinutes = minutes > 0 ? minutes : 45;
                }

                if (root.TryGetProperty("PlaySound", out var soundEl)
                    && (soundEl.ValueKind == JsonValueKind.True || soundEl.ValueKind == JsonValueKind.False))
                {
                    PlaySound = soundEl.GetBoolean();
                }

                if (root.TryGetProperty("ScheduledReminders", out var arrEl) && arrEl.ValueKind == JsonValueKind.Array)
                {
                    hasScheduledKey = true;
                    foreach (var item in arrEl.EnumerateArray())
                    {
                        var reminder = ParseReminder(item);
                        if (reminder != null)
                            ScheduledReminders.Add(reminder);
                    }
                }
            }
            catch
            {
                ReminderIntervalMinutes = 45;
            }
        }

        if (!hasScheduledKey)
        {
            SeedDefaultReminders();
            SaveSettings();
        }
    }

    private static ScheduledReminder? ParseReminder(JsonElement el)
    {
        try
        {
            var r = new ScheduledReminder
            {
                Enabled = el.TryGetProperty("Enabled", out var en) && en.GetBoolean(),
                Message = el.TryGetProperty("Message", out var msg) ? (msg.GetString() ?? "") : "",
            };

            if (el.TryGetProperty("Time", out var timeEl) && timeEl.GetString() is string timeStr
                && TimeOnly.TryParse(timeStr, out var t))
            {
                r.Time = t;
            }

            if (el.TryGetProperty("Days", out var daysEl) && daysEl.GetString() is string daysStr
                && Enum.TryParse<DayOfWeekMask>(daysStr, out var mask))
            {
                r.Days = mask;
            }

            if (el.TryGetProperty("LastFiredDate", out var lfdEl) && lfdEl.ValueKind == JsonValueKind.String
                && DateOnly.TryParse(lfdEl.GetString(), out var lfd))
            {
                r.LastFiredDate = lfd;
            }

            return r;
        }
        catch
        {
            return null;
        }
    }

    private void SeedDefaultReminders()
    {
        ScheduledReminders.Clear();
        ScheduledReminders.Add(new ScheduledReminder { Enabled = true, Time = new TimeOnly(11, 55), Days = DayOfWeekMask.Weekdays, Message = "吃飯囉！" });
        ScheduledReminders.Add(new ScheduledReminder { Enabled = true, Time = new TimeOnly(19, 0), Days = DayOfWeekMask.Weekdays, Message = "下班時間到！" });
        ScheduledReminders.Add(new ScheduledReminder { Enabled = true, Time = new TimeOnly(0, 0), Days = DayOfWeekMask.All, Message = "該睡覺了！" });
    }

    public void SaveSettings()
    {
        try
        {
            var reminders = new List<object>();
            foreach (var r in ScheduledReminders)
            {
                var obj = new Dictionary<string, object?>
                {
                    ["Enabled"] = r.Enabled,
                    ["Time"] = r.Time.ToString("HH:mm"),
                    ["Days"] = r.Days.ToString(),
                    ["Message"] = r.Message,
                };
                if (r.LastFiredDate.HasValue)
                    obj["LastFiredDate"] = r.LastFiredDate.Value.ToString("yyyy-MM-dd");
                reminders.Add(obj);
            }

            var payload = new Dictionary<string, object>
            {
                ["IntervalMinutes"] = ReminderIntervalMinutes,
                ["PlaySound"] = PlaySound,
                ["ScheduledReminders"] = reminders,
            };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        ShowNotification(null);
    }

    private void ScheduleTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);
        bool anyFired = false;

        foreach (var r in ScheduledReminders)
        {
            if (!r.Enabled) continue;
            if (r.LastFiredDate == today) continue;
            if (!r.Days.Matches(now.DayOfWeek)) continue;
            if (currentTime < r.Time) continue;
            if ((currentTime.ToTimeSpan() - r.Time.ToTimeSpan()) > TimeSpan.FromMinutes(2)) continue;

            r.LastFiredDate = today;
            anyFired = true;
            ShowNotification(r.Message);
        }

        if (anyFired) SaveSettings();
    }

    public void UpdateTimer()
    {
        _timer.Stop();
        _timer.Interval = TimeSpan.FromMinutes(ReminderIntervalMinutes);
        _timer.Start();

        SaveSettings();

        if (_notifyIcon != null)
            _notifyIcon.ToolTipText = $"休息提醒小幫手 (每 {ReminderIntervalMinutes} 分鐘提醒)";
    }

    private void ShowNotification(string? message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_reminderWindow != null)
            {
                _pendingMessages.Enqueue(message);
                return;
            }

            OpenReminderWindow(message);
        });
    }

    private void OpenReminderWindow(string? message)
    {
        _timer.Stop();

        _reminderWindow = new ReminderWindow(message);
        _reminderWindow.Closed += (_, _) =>
        {
            _reminderWindow = null;

            if (_pendingMessages.Count > 0)
            {
                var next = _pendingMessages.Dequeue();
                Application.Current.Dispatcher.BeginInvoke(new Action(() => OpenReminderWindow(next)));
            }
            else
            {
                _timer.Start();
            }
        };
        _reminderWindow.Show();
        _reminderWindow.Activate();
    }

    private void ShowSettings()
    {
        if (_settingsWindow.IsVisible)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
            _settingsWindow.Activate();
        }
        else
        {
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
    }

    private void SetStartup(bool enable)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enable)
            {
                string? appPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (appPath != null)
                {
                    key?.SetValue("BreakReminderApp", $"\"{appPath}\"");
                }
            }
            else
            {
                key?.DeleteValue("BreakReminderApp", false);
            }
        }
        catch (Exception)
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) { _execute = execute; }
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
