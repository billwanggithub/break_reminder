using System.Windows;
using System.Media;

namespace BreakReminder;

public partial class ReminderWindow : Window
{
    public ReminderWindow(string? message = null)
    {
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(message))
        {
            MessageText.Text = message;
        }

        if (Application.Current is App app && app.PlaySound)
        {
            SystemSounds.Exclamation.Play();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
