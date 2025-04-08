using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;
using static Raylib_cs.TraceLogLevel;
using static ArchipelagoDeathCounter.LoggerConsole.Logger.Level;

namespace ArchipelagoDeathCounter.LoggerConsole;

public static class Logger
{
    public enum Level
    {
        Info,
        Debug,
        Special,
        Warning,
        SoftError,
        Error,
        Other
    }

    public static readonly string CrashSave = $"{Directory.GetCurrentDirectory().Replace('\\', '/')}/CrashLogs";
    public static readonly string StatusSave = $"{Directory.GetCurrentDirectory().Replace('\\', '/')}/CrashLogs";
    public static readonly string Guid = System.Guid.NewGuid().ToString();

    private static readonly List<string> LogList = [];

    public static bool ShowDebugLogs = true;

    private static bool HasError;

    public static event EventHandler<LogReceivedEventArgs>? LogReceived;

    public static unsafe void Initialize()
    {
        Raylib.SetTraceLogCallback(&RayLog);

        LogReceived += GameConsole.LogMessage;
        LogReceived += (_, args) =>
        {
            if (args.LogMessageLevel is not Level.Error) return;
            Console.ForegroundColor = args.LogMessageLevel switch
            {
                Level.Info => ConsoleColor.DarkGreen,
                Level.Debug => ConsoleColor.DarkCyan,
                Level.Warning => ConsoleColor.Yellow,
                Level.Error or SoftError => ConsoleColor.Red,
                Other => ConsoleColor.Blue,
                Special => ConsoleColor.Cyan,
                _ => throw new ArgumentOutOfRangeException(nameof(args.LogMessageLevel), args.LogMessageLevel, null)
            };

            Console.WriteLine(args);
            Console.ForegroundColor = ConsoleColor.White;
        };
    }

    // i want to turn this to [typeof(CallConvCdecl)] REALLY BADLY but it causes my ide to hallucinate
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void RayLog(int logLevel, sbyte* text, sbyte* args)
    {
        Log(logLevel switch
            {
                (int) All => Other,
                (int) Trace or (int) TraceLogLevel.Debug => Level.Debug,
                (int) TraceLogLevel.Info or (int) None => Level.Info,
                (int) TraceLogLevel.Warning => Level.Warning,
                (int) TraceLogLevel.Error or (int) Fatal => Level.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            }, $"{Logging.GetLogMessage(new IntPtr(text), new IntPtr(args))}", "raylib");
    }

    public static void Log(string? text, string sender = "app") => Log(Level.Debug, text!, sender);
    public static void Log(object text, string sender = "app") => Log(Level.Debug, text.ToString()!, sender);
    public static void Log(Exception e, string sender = "app") => Log(Level.Error, $"{e.Message}\n{e.StackTrace}", sender);

    public static T LogReturn<T>(T t)
    {
        Log(t!.ToString()!);
        return t;
    }

    public static void Log(Level level, string text, string sender)
    {
        if (!ShowDebugLogs && level is Level.Debug) return;
        var time = $"{DateTime.Now:G}";
        LogList.Add(Format(level, time, sender, text));
        LogReceived?.Invoke(null, new LogReceivedEventArgs(level, time, text.Trim(), sender));
    }

    public static void Log(Level level, object? text, string sender = "app") => Log(level, text!.ToString()!, sender);

    public static void WriteLog(bool isCrash = true)
    {
        HasError = false;
        var dir = isCrash ? "CrashLogs" : "StatusLogs";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var file = isCrash
            ? $"CrashLogs/Crash {DateTime.Now:u}.log".Replace(' ', '_').Replace(':', '-')
            : $"StatusLogs/Status {Guid}.log";

        using var sw = File.CreateText(file);
        sw.Write(string.Join("\n", LogList));
        sw.Close();

        Console.WriteLine(
            $"SAVED {(isCrash ? "CRASH" : "STATUS")} LOG AT: {Directory.GetCurrentDirectory().Replace('\\', '/')}/{file}");
    }

    public static string Format(LogReceivedEventArgs args)
        => Format(args.LogMessageLevel, args.TimeOfMessage, args.Sender, args.LogMessage);

    public static string Format(Level level, string time, string sender, string text)
    {
        if (level is Level.Error)
        {
            HasError = true;
            return $"[{time}]  [{level}] From [{sender}]  Sent Error:\n\t{text.Trim()}";
        }

        return $"[{time}]  [{level}] From [{sender}]  [{text.Trim()}]";
    }

    public static void CheckWrite()
    {
        if (!HasError) return;
        WriteLog();
    }
}