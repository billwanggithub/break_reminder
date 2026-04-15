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

namespace BreakReminder;

public partial class App : Application
{
    private TaskbarIcon _notifyIcon = null!;
    private DispatcherTimer _timer = null!;
    private MainWindow _settingsWindow = null!;
    private string _settingsPath = null!;
    
    // Interval in minutes
    public int ReminderIntervalMinutes { get; set; } = 45;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BreakReminder");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        LoadSettings();

        // Initialize Settings Window
        _settingsWindow = new MainWindow();

        // Create the taskbar icon
        _notifyIcon = new TaskbarIcon();
        
        // Extract default application icon
        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        _notifyIcon.ToolTipText = "休息提醒小幫手 (雙擊開啟設定)";
        _notifyIcon.DoubleClickCommand = new RelayCommand(ShowSettings);

        // Context Menu
        ContextMenu contextMenu = new ContextMenu();
        
        MenuItem settingsItem = new MenuItem { Header = "設定 (Settings)" };
        settingsItem.Click += (s, ev) => ShowSettings();
        
        MenuItem breakNowItem = new MenuItem { Header = "立刻休息 (Break Now)" };
        breakNowItem.Click += (s, ev) => ShowNotification();

        MenuItem exitItem = new MenuItem { Header = "離開 (Exit)" };
        exitItem.Click += (s, ev) => Application.Current.Shutdown();

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(breakNowItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);
        _notifyIcon.ContextMenu = contextMenu;

        // Auto startup setup
        SetStartup(true);

        // Initialize Timer
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
        UpdateTimer();
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, int>>(json);
                if (dict != null && dict.TryGetValue("IntervalMinutes", out int minutes))
                {
                    ReminderIntervalMinutes = minutes > 0 ? minutes : 45;
                }
            }
            catch { ReminderIntervalMinutes = 45; }
        }
    }

    private void SaveSettings()
    {
        try
        {
            var dict = new System.Collections.Generic.Dictionary<string, int> { { "IntervalMinutes", ReminderIntervalMinutes } };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(dict));
        }
        catch { }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        ShowNotification();
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

    private void ShowNotification()
    {
        // 顯示 WPF 彈出視窗及提示音
        Application.Current.Dispatcher.Invoke(() =>
        {
            var reminder = new ReminderWindow();
            reminder.Show();
            reminder.Activate();
        });

        // Reset timer after showing notification
        _timer.Stop();
        _timer.Start();
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

