using System;
using System.IO;

namespace ScheduledDownloader;
public class Logger 
{
    private StreamWriter writer;
    public Logger(string file = "log.txt") 
    {
        writer = new StreamWriter(file, true);
    }
    public void Log(string str) 
    {
        writer.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} {str}");
        writer.Flush();
    }
    public void Close() 
    {
        writer.Close();
    }
}
