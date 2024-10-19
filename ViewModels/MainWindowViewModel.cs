using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Data;
using DynamicData.Binding;
using FluentFTP;
using ReactiveUI;
using ScheduledDownloader.Models;

namespace ScheduledDownloader.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private CancellationTokenSource cts = new();

    private TimeSpan time = new TimeSpan(12, 00, 00);
    private bool started;

    public AvaloniaList<PCConfig> Configs { get; set; } = new();
    public AvaloniaList<string> Logs { get; set; } = new();
    public ICommand DownloadCommand { get; set; }
    public ICommand AddCommand { get; set; }

    public bool Started { get => started; set => this.RaiseAndSetIfChanged(ref started, value); }

    public TimeSpan Time
    {
        get => time;
        set => this.RaiseAndSetIfChanged(ref time, value);
    }
    public void Save()
    {
        var cfg = new Config()
        {
            Configs = Configs.ToArray(),
            Time = Time,
        };
        File.WriteAllText("config.json", JsonSerializer.Serialize(cfg, new JsonSerializerOptions { IgnoreReadOnlyProperties = true }));
    }
    public MainWindowViewModel()
    {
        DownloadCommand = ReactiveCommand.CreateFromTask(Download, this.WhenAnyValue(x => x.Started, x => x == false));
        AddCommand = ReactiveCommand.Create(Add, this.WhenAnyValue(x => x.Started, x => x == false));
        if (File.Exists("config.json"))
        {
            var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            Configs = new(cfg!.Configs);
            Time = cfg.Time;
        }
        this.Configs.TrackItemPropertyChanged(delegate
        {
            Save();
        });
        this.Configs.CollectionChanged += delegate
        {
            Save();
        };
        this.WhenAnyValue(x => x.Time).Subscribe(_ => Save());
    }
    public void Add()
    {
        Configs.Add(new PCConfig()
        {
            Enabled = true,
            IP = "127.0.0.1",
            Port = 21,
            Path = "/",
            SavePath = "C:\\Downloads",
        });
    }
    public void Start()
    {
        Started = true;
        Task.Factory.StartNew(async () => await ScheduledDownload(cts.Token), TaskCreationOptions.LongRunning);
    }
    public void Stop()
    {
        cts.Cancel();
        Started = false;
        cts = new();
    }
    private async Task ScheduledDownload(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            var now = DateTime.Now;
            try
            {
                if (now.Hour == Time.Hours && now.Minute == Time.Minutes)
                {
                    await Download(cancellation);
                }
                var time = TimeOnly.FromDateTime(now).ToTimeSpan();
                var diff = Time - time;
                if (diff.TotalMinutes < 30)
                {
                    await Task.Delay(30 * 1000, cancellation);
                }
                else
                {
                    await Task.Delay(25 * 60 * 1000, cancellation);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
    private void DirEnsureExists(string dir)
    {
        if (Directory.Exists(dir))
        {
            return;
        }
        else
        {
            var parent = Directory.GetParent(dir);
            if (parent != null)
                DirEnsureExists(parent.FullName);
            Directory.CreateDirectory(dir);
        }
    }
    private async Task Download(CancellationToken cancellation)
    {
        Logs.Clear();
        foreach (var pc in Configs.Where(x => x.Enabled))
        {
            if (cancellation.IsCancellationRequested)
            {
                Logs.Add("Cancelled");
                throw new OperationCanceledException();
            }
            Logs.Add($"Started download for {pc.IP}:{pc.Port}");
            try
            {
                DirEnsureExists(pc.SavePath);
                var client = new AsyncFtpClient(pc.IP, "admin", "1", pc.Port);
                await client.Connect();
                var datepath = DateTime.Now.ToString("yy/MM/dd");
                var dir = new Uri(new Uri("ftp://notrelevant.shit" + pc.Path + (pc.Path.EndsWith("/") ? "" : "/")), new Uri(datepath, UriKind.Relative));
                Logs.Add($"Downloading dir {dir.AbsolutePath}");
                await client.DownloadDirectory(Path.Combine(pc.SavePath, datepath), dir.AbsolutePath, FtpFolderSyncMode.Update, token: cancellation);
                Logs.Add($"Successfuly downloaded dir {dir}");
            }
            catch (OperationCanceledException)
            {
                Logs.Add("Cancelled");
                throw;
            }
            catch (Exception e)
            {
                Logs.Add($"Error {e.ToString()}");
            }
        }
    }

}
