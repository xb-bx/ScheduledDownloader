using System.Net;
using Avalonia.Data;
using ReactiveUI;

namespace ScheduledDownloader.Models;
public class PCConfig : ReactiveObject
{
    private string ip;
    private int port;
    private string path;
    private string savePath;
    private bool enabled;
    public bool Enabled { get => enabled; set => this.RaiseAndSetIfChanged(ref enabled, value); }
    public string IP
    {
        get => ip;
        set
        {
            if (!IPAddress.TryParse(value, out _))
            {
                throw new DataValidationException("Неправильный IP");
            }
            this.RaiseAndSetIfChanged(ref ip, value);
        }
    }
    public int Port
    {
        get => port;
        set
        {
            if (value <= 0 || value > 65535)
            {
                throw new DataValidationException("Неправильный порт");
            }
            this.RaiseAndSetIfChanged(ref port, value);
        }
    }
    public string Path
    {
        get => path;
        set
        {
            if (!value.StartsWith("/"))
            {
                throw new DataValidationException("Неправильный путь");
            }
            this.RaiseAndSetIfChanged(ref path, value);
        }
    }
    public string SavePath
    {
        get => savePath;
        set
        {
            this.RaiseAndSetIfChanged(ref savePath, value);
        }
    }
}
