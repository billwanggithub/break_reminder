using System.Windows;
using System.Media;

namespace BreakReminder;

public partial class ReminderWindow : Window
{
    public ReminderWindow()
    {
        InitializeComponent();
        
        // 播放系統提示音 (驚嘆號音效) 1次
        SystemSounds.Exclamation.Play();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
