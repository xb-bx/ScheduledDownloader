using System;

namespace ScheduledDownloader.Models;
public class Config
{
    public required PCConfig[] Configs { get; set; }
    public TimeSpan Time { get; set; }
}
