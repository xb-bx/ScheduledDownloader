using Avalonia.Controls;

namespace ScheduledDownloader.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += (o, e) => 
        {
            e.Cancel = true;
            Hide();
        };
    }
}
