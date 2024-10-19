using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ScheduledDownloader.Views;

namespace ScheduledDownloader.ViewModels;

public class ApplicationViewModel : ViewModelBase
{
    public MainWindow Window { get; set; } = null!;
    public void Open()
    {
        Window?.Show();
    }
    public void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime application)
        {
            application.Shutdown();
        }
    }
}
