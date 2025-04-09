using System.Numerics;
using ArchipelagoDeathCounter.LoggerConsole.Commands;
using Backbone;
using ImGuiNET;
using static ArchipelagoDeathCounter.LoggerConsole.Logger;
using static ArchipelagoDeathCounter.LoggerConsole.Logger.Level;
using static Raylib_cs.Color;

namespace ArchipelagoDeathCounter.LoggerConsole;

public class GameConsole : Messenger<LogMessage>
{
    public static readonly Dictionary<Level, Vector4> Colors = new()
    {
        [Info] = DarkGreen.ToV4(),
        [Debug] = Orange.ToV4(),
        [Warning] = Yellow.ToV4(),
        [SoftError] = Red.ToV4(),
        [Error] = Red.ToV4(),
        [Other] = Blue.ToV4(),
        [Special] = SkyBlue.ToV4()
    };
    public void SendMessage(object? sender, LogReceivedEventArgs args)
        => SendMessage(new LogMessage(args));

    protected override void OnSentMessage(string message) => CommandRegister.RunCommand(message);

    protected override void RenderMessage(LogMessage message) => ImGui.TextColored(Colors[message.Level], message.Message);
}

public readonly struct LogMessage(LogReceivedEventArgs args)
{
    public readonly Level Level = args.LogMessageLevel;
    public readonly string Message = Format(args);
}