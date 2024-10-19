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
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using DynamicData.Binding;
using FluentFTP;
using ReactiveUI;
using ScheduledDownloader.Models;
using ScheduledDownloader.Views;

namespace ScheduledDownloader.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private CancellationTokenSource cts = new();

    public MainWindow Window;
    private TimeSpan time = new TimeSpan(12, 00, 00);
    private bool started;
    private Logger logger = new();
    private string logPath = "log.txt";

    public AvaloniaList<PCConfig> Configs { get; set; } = new();
    public ICommand DownloadCommand { get; set; }
    public ICommand AddCommand { get; set; }
    public ICommand ChooseLogCommand { get; set; }

    public bool Started { get => started; set => this.RaiseAndSetIfChanged(ref started, value); }

    public string LogPath { get => logPath; set => this.RaiseAndSetIfChanged(ref logPath, value); }
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
            LogPath = LogPath,
        };
        File.WriteAllText("config.json", JsonSerializer.Serialize(cfg, new JsonSerializerOptions { IgnoreReadOnlyProperties = true }));
    }
    public MainWindowViewModel()
    {
        DownloadCommand = ReactiveCommand.CreateFromTask(Download, this.WhenAnyValue(x => x.Started, x => x == false));
        AddCommand = ReactiveCommand.Create(Add, this.WhenAnyValue(x => x.Started, x => x == false));
        ChooseLogCommand = ReactiveCommand.Create(ChooseLog, this.WhenAnyValue(x => x.Started, x => x == false));
        if (File.Exists("config.json"))
        {
            var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            Configs = new(cfg!.Configs);
            Time = cfg.Time;
            LogPath = cfg.LogPath;
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
        logger.Close();
        logger = new Logger(LogPath);
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
    private async Task ChooseLog()
    {
        var topLevel = TopLevel.GetTopLevel(Window);

        var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Лог",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            LogPath = Path.Combine(files[0].Path.AbsolutePath, "log.txt");
            logger.Close();
            logger = new Logger(LogPath);
        }
    }
    private async Task Download(CancellationToken cancellation)
    {
        foreach (var pc in Configs.Where(x => x.Enabled))
        {
            if (cancellation.IsCancellationRequested)
            {
                logger.Log("Cancelled");
                throw new OperationCanceledException();
            }
            logger.Log($"Started download for {pc.IP}:{pc.Port}");
            try
            {
                var client = new AsyncFtpClient(pc.IP, "admin", "1", pc.Port);
                await client.Connect();
                var path = pc.Path.Replace("%YEAR%", DateTime.Now.Year.ToString("0000")).Replace("%MONTH%", DateTime.Now.Month.ToString("00")).Replace("%DAY%", DateTime.Now.Day.ToString("00"));
                if (await client.DirectoryExists(path))
                {
                    var savepath = pc.SavePath.Replace("%YEAR%", DateTime.Now.Year.ToString()).Replace("%MONTH%", DateTime.Now.Month.ToString()).Replace("%DAY%", DateTime.Now.Day.ToString());
                    logger.Log($"Downloading dir {path}");
                    await client.DownloadDirectory(savepath, path, FtpFolderSyncMode.Update, token: cancellation);
                    logger.Log($"Successfuly downloaded dir {path} to {savepath}");

                }
                else
                {
                    var savepath = pc.SavePath + (pc.SavePath.EndsWith("/") ? "" : "/") + path.Substring(path.LastIndexOf("/") + 1);
                    logger.Log($"Downloading file {path}");
                    await client.DownloadFile(savepath, path);
                    logger.Log($"Successfuly downloaded file {path} to {savepath}");
                }
            }
            catch (OperationCanceledException)
            {
                logger.Log("Cancelled");
                throw;
            }
            catch (Exception e)
            {
                logger.Log($"Error {e.ToString()}");
            }
        }
    }

}
