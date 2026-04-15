using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

    public MainWindow()
    {
        InitializeComponent();
        
        // Sync initial value from App
        if (Application.Current is App app)
        {
            ReminderIntervalMinutes = app.ReminderIntervalMinutes;
        }
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is TextBox textBox)
            {
                // Move focus to another element (e.g., the Save button) to trigger LostFocus update
                SaveButton.Focus();
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Enforce focus change in case user clicked button while still in textbox
        SaveButton.Focus();

        if (Application.Current is App app)
        {
            app.ReminderIntervalMinutes = this.ReminderIntervalMinutes;
            app.UpdateTimer();
        }

        // Hide window instead of closing
        this.Hide();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Cancel the close operation and just hide the window
        e.Cancel = true;
        this.Hide();
    }
}