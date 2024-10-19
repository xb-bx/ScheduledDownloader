using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ScheduledDownloader.ViewModels;
using ScheduledDownloader.Views;

namespace ScheduledDownloader;

public partial class App : Application
{
    private ApplicationViewModel vm = new();
    public App() 
    {
        this.DataContext = vm;
    }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            vm.Window = (MainWindow)desktop.MainWindow;
            ((MainWindowViewModel)desktop.MainWindow.DataContext).Window = (MainWindow)desktop.MainWindow;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
