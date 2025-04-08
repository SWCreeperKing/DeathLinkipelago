using ArchipelagoDeathCounter.LoggerConsole.Commands;
using Backbone;
using ImGuiNET;
using static ArchipelagoDeathCounter.LoggerConsole.Logger;
using static ArchipelagoDeathCounter.LoggerConsole.Logger.Level;
using static Backbone.Backbone;
using static ImGuiNET.ImGuiInputTextFlags;
using static Raylib_cs.Color;

namespace ArchipelagoDeathCounter.LoggerConsole;

public static class GameConsole
{
    public static bool IsConsoleOpen;

    public static readonly Dictionary<Level, uint> Colors = new()
    {
        [Info] = DarkGreen.ToUint(),
        [Debug] = Orange.ToUint(),
        [Warning] = Yellow.ToUint(),
        [SoftError] = Red.ToUint(),
        [Error] = Red.ToUint(),
        [Other] = Blue.ToUint(),
        [Special] = SkyBlue.ToUint()
    };

    public static int MaxScrollback = 200;
    public static bool ScrollToBottom = true;

    private static Queue<LogMessage> Scrollback = [];
    private static string Input = "";
    private static bool ToScroll;

    public static void Render()
    {
        var wSize = ImGui.GetContentRegionAvail();
        ImGui.BeginChild("console.child", wSize with { Y = wSize.Y - 50 }, ImGuiChildFlags.Borders);
        {
            foreach (var message in Scrollback)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Colors[message.Level]);
                ImGui.Text(message.Message);
                ImGui.PopStyleColor();
            }

            if (ScrollToBottom && ToScroll)
            {
                ImGui.SetScrollHereY();
                ToScroll = false;
            }
            else if (ToScroll)
            {
                ToScroll = false;
            }
        }

        ImGui.EndChild();

        if (!ImGui.InputText("Command", ref Input, 999, EnterReturnsTrue)) return;
        CommandRegister.RunCommand(Input);
        UpdateScrollBack();
        Input = "";
        Console.WriteLine(Scrollback.Count);
    }

    public static void LogMessage(object? sender, LogReceivedEventArgs args)
        => Scrollback.Enqueue(new LogMessage(args));

    public static void LogMessage(LogMessage message)
    {
        Scrollback.Enqueue(message);
        UpdateScrollBack();
    }

    private static void UpdateScrollBack()
    {
        while (Scrollback.Count > MaxScrollback) Scrollback.Dequeue();
        if (ScrollToBottom) ToScroll = true;
    }

    public static void ToggleConsole() => IsConsoleOpen = !IsConsoleOpen;
}

public readonly struct LogMessage(LogReceivedEventArgs args)
{
    public readonly Level Level = args.LogMessageLevel;
    public readonly string Message = Format(args);
}