namespace BreakReminder;

[Flags]
public enum DayOfWeekMask
{
    None = 0,
    Sun = 1,
    Mon = 2,
    Tue = 4,
    Wed = 8,
    Thu = 16,
    Fri = 32,
    Sat = 64,
    Weekdays = Mon | Tue | Wed | Thu | Fri,
    All = Sun | Mon | Tue | Wed | Thu | Fri | Sat,
}

public static class DayOfWeekMaskExtensions
{
    public static DayOfWeekMask ToMask(this DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => DayOfWeekMask.Sun,
        DayOfWeek.Monday => DayOfWeekMask.Mon,
        DayOfWeek.Tuesday => DayOfWeekMask.Tue,
        DayOfWeek.Wednesday => DayOfWeekMask.Wed,
        DayOfWeek.Thursday => DayOfWeekMask.Thu,
        DayOfWeek.Friday => DayOfWeekMask.Fri,
        DayOfWeek.Saturday => DayOfWeekMask.Sat,
        _ => DayOfWeekMask.None,
    };

    public static bool Matches(this DayOfWeekMask mask, DayOfWeek day) =>
        (mask & day.ToMask()) != 0;
}

public class ScheduledReminder
{
    public bool Enabled { get; set; } = true;
    public TimeOnly Time { get; set; }
    public DayOfWeekMask Days { get; set; } = DayOfWeekMask.All;
    public string Message { get; set; } = "";
    public DateOnly? LastFiredDate { get; set; }
}
