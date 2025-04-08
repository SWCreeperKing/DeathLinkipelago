using System.Reflection;
using RayWork.Commands;
using static ArchipelagoDeathCounter.LoggerConsole.Logger;
using static ArchipelagoDeathCounter.LoggerConsole.Logger.Level;

namespace ArchipelagoDeathCounter.LoggerConsole.Commands;

public static class CommandRegister
{
    private static Dictionary<string, MethodInfo> Commands = [];
    private static Dictionary<string, string> CommandHelp = [];

    public static void RegisterCommandFile<T>() => RegisterCommandFile(typeof(T));

    public static void RegisterCommandFile(Type t)
    {
        Log($"Reading [{t.Name}]", "Command Register");
        foreach (var method in t.GetMethods().Where(m => m.GetCustomAttribute<CommandAttribute>() is not null))
        {
            if (!method.IsStatic)
            {
                Log(SoftError, $"[{method.Name}] From [{t.Name}] Is NOT Static", "Command Register");
                continue;
            }

            var returnType = method.ReturnParameter;
            if (returnType.ParameterType != typeof(void) && returnType.ParameterType != typeof(LogReturn))
            {
                Log(SoftError,
                    $"[{method.Name}] From [{t.Name}] Does NOT Have A Return Type Of `string` Or `void`",
                    "Command Register");
                continue;
            }

            var param = method.GetParameters();
            if (param[0].ParameterType != typeof(string[]))
            {
                Log(SoftError, $"[{method.Name}] From [{t.Name}] Does NOT Have A Parameter Type Of `string[]`",
                    "Command Register");
                continue;
            }

            var attribute = method.GetCustomAttribute<CommandAttribute>();
            var command = attribute!.Name;

            if (Commands.TryAdd(command, method))
            {
                Log($"Command [{command}] Was Added To Commands", "Command Register");

                var help = method.GetCustomAttribute<HelpAttribute>();
                if (help is null) continue;
                CommandHelp[command] = help.Help;
                continue;
            }

            Log(SoftError, $"Command Name [{command}] From [{t.Name}] Already Exists!",
                "Command Register");
        }
    }

    public static void RunCommand(string fullCommand)
    {
        Log(fullCommand, "User");
        var commandArgs = fullCommand.Split(' ');

        if (!Commands.TryGetValue(commandArgs[0], out var method))
        {
            Log(SoftError, $"Command [{commandArgs[0]}] Does NOT Exist", "User");
            return;
        }

        var result = method.Invoke(null, [commandArgs.Skip(1).ToArray()]);
        if (result is not LogReturn lr) return;
        Log(lr.Level, lr.Message, commandArgs[0]);
    }

    public static bool TryGetHelp(string key, out string? help) => CommandHelp.TryGetValue(key, out help);

    public static IEnumerable<(string key, string value)> GetHelp()
    {
        var keys = CommandHelp.Keys.ToArray();
        for (var i = 0; i < CommandHelp.Count; i++)
        {
            var key = keys[i];
            yield return (key, CommandHelp[key]);
        }
    }
}

public readonly struct LogReturn(string message, Level level = Special)
{
    public readonly Level Level = level;
    public readonly string Message = message;
}