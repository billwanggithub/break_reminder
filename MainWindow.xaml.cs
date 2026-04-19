using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BreakReminder;

public partial class MainWindow : Window
{
    public int ReminderIntervalMinutes
    {
        get { return (int)GetValue(ReminderIntervalMinutesProperty); }
        set { SetValue(ReminderIntervalMinutesProperty, value); }
    }

    public static readonly DependencyProperty ReminderIntervalMinutesProperty =
        DependencyProperty.Register("ReminderIntervalMinutes", typeof(int), typeof(MainWindow), new PropertyMetadata(45));

    public bool PlaySound
    {
        get { return (bool)GetValue(PlaySoundProperty); }
        set { SetValue(PlaySoundProperty, value); }
    }

    public static readonly DependencyProperty PlaySoundProperty =
        DependencyProperty.Register("PlaySound", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

    public ObservableCollection<ReminderRow> Rows { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        if (v != null)
            this.Title = $"{this.Title} v{v.Major}.{v.Minor}.{v.Build}";

        if (Application.Current is App app)
        {
            ReminderIntervalMinutes = app.ReminderIntervalMinutes;
            PlaySound = app.PlaySound;
            foreach (var r in app.ScheduledReminders)
                Rows.Add(ReminderRow.FromModel(r));
        }

        ReminderList.ItemsSource = Rows;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox)
        {
            SaveButton.Focus();
        }
    }

    private void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        Rows.Add(new ReminderRow
        {
            Enabled = true,
            TimeText = "09:00",
            Mon = true, Tue = true, Wed = true, Thu = true, Fri = true,
            Message = "提醒",
        });
    }

    private void DeleteReminder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ReminderRow row)
        {
            Rows.Remove(row);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.Focus();

        if (Application.Current is not App app) return;

        app.ReminderIntervalMinutes = this.ReminderIntervalMinutes;
        app.PlaySound = this.PlaySound;

        app.ScheduledReminders.Clear();
        foreach (var row in Rows)
        {
            var model = row.ToModel();
            if (model != null)
                app.ScheduledReminders.Add(model);
        }

        app.UpdateTimer();
        app.SaveSettings();

        this.Hide();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
    }
}

public class ReminderRow : INotifyPropertyChanged
{
    private bool _enabled = true;
    private string _timeText = "09:00";
    private string _message = "";
    private bool _mon, _tue, _wed, _thu, _fri, _sat, _sun;

    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    public string TimeText { get => _timeText; set => Set(ref _timeText, value); }
    public string Message { get => _message; set => Set(ref _message, value); }
    public bool Mon { get => _mon; set => Set(ref _mon, value); }
    public bool Tue { get => _tue; set => Set(ref _tue, value); }
    public bool Wed { get => _wed; set => Set(ref _wed, value); }
    public bool Thu { get => _thu; set => Set(ref _thu, value); }
    public bool Fri { get => _fri; set => Set(ref _fri, value); }
    public bool Sat { get => _sat; set => Set(ref _sat, value); }
    public bool Sun { get => _sun; set => Set(ref _sun, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static ReminderRow FromModel(ScheduledReminder r) => new()
    {
        Enabled = r.Enabled,
        TimeText = r.Time.ToString("HH:mm"),
        Message = r.Message,
        Mon = (r.Days & DayOfWeekMask.Mon) != 0,
        Tue = (r.Days & DayOfWeekMask.Tue) != 0,
        Wed = (r.Days & DayOfWeekMask.Wed) != 0,
        Thu = (r.Days & DayOfWeekMask.Thu) != 0,
        Fri = (r.Days & DayOfWeekMask.Fri) != 0,
        Sat = (r.Days & DayOfWeekMask.Sat) != 0,
        Sun = (r.Days & DayOfWeekMask.Sun) != 0,
    };

    public ScheduledReminder? ToModel()
    {
        if (!TimeOnly.TryParse(TimeText, out var t))
            return null;

        var mask = DayOfWeekMask.None;
        if (Mon) mask |= DayOfWeekMask.Mon;
        if (Tue) mask |= DayOfWeekMask.Tue;
        if (Wed) mask |= DayOfWeekMask.Wed;
        if (Thu) mask |= DayOfWeekMask.Thu;
        if (Fri) mask |= DayOfWeekMask.Fri;
        if (Sat) mask |= DayOfWeekMask.Sat;
        if (Sun) mask |= DayOfWeekMask.Sun;

        return new ScheduledReminder
        {
            Enabled = Enabled,
            Time = t,
            Days = mask,
            Message = Message,
        };
    }
}
