using System.IO;
using Takt.Helpers;
using Takt.Models;

namespace Takt.Services;

internal static class AppLog
{
    private static readonly object Gate = new();

    public static LogLevel Minimum { get; set; } = LogLevel.Info;

    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Takt", "logs");

    public static string ReadRecent(int maxLines = 80)
    {
        try
        {
            var path = Path.Combine(Folder, "takt.log");
            if (!File.Exists(path))
                return "";
            return LogTail.LastLines(File.ReadAllText(path), maxLines);
        }
        catch
        {
            return "";
        }
    }

    public static void Info(string message)
    {
        if (Minimum < LogLevel.Info)
            return;
        Write("INFO", message, null);
    }

    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Takt", "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "takt.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {level} {message}";
            if (exception is not null)
                line += " " + exception;
            lock (Gate)
            {
                var info = new FileInfo(path);
                if (info.Exists && info.Length > 512 * 1024)
                {
                    var previous = path + ".1";
                    if (File.Exists(previous))
                        File.Delete(previous);
                    File.Move(path, previous);
                }

                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // logging must not take the app down
        }
    }
}
