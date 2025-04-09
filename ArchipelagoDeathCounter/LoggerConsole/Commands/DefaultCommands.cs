using System.Globalization;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json.Linq;
using Raylib_cs;
using RayWork.Commands;
using static ArchipelagoDeathCounter.LoggerConsole.Logger.Level;
using static UserInterface;

namespace ArchipelagoDeathCounter.LoggerConsole.Commands;

public static class DefaultCommands
{
    [Command("help"), Help("Provides information on all commands")]
    public static LogReturn Help(string[] args)
    {
        if (args.Length > 0)
            return !CommandRegister.TryGetHelp(args[0], out var specificHelp)
                ? new LogReturn($"Command [{args[0]}] Does Not Exist Or Does Not Have Help", SoftError)
                : new LogReturn($":\n{FormatHelp(args[0], specificHelp!)}\n:");

        StringBuilder sb = new();
        sb.Append(":\n");
        foreach (var (command, help) in CommandRegister.GetHelp())
        {
            sb.Append(FormatHelp(command, help)).Append('\n');
        }

        sb.Append(':');

        return new LogReturn(sb.ToString());

        string FormatHelp(string command, string help)
            => $"""
                - {command}
                    -> {help.Replace("\n", "\n    -> ")}
                """;
    }

    [Command("setfps"), Help("setfps [n > 0]\nsets fps to n")]
    public static LogReturn SetFps(string[] args)
    {
        if (args.Length < 1) return new LogReturn("Not Enough Arguments", SoftError);
        if (!int.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var fpsset) || fpsset <= 0)
            return new LogReturn($"[{args[0]}] <- IS NOT A VALID NUMBER", SoftError);
        Raylib.SetTargetFPS(fpsset);
        return new LogReturn($"Fps set to [{fpsset}]");
    }

    [Command("death"),
     Help(
         "death [slot] [amount = 1]\nAdds [amount] to [slot] in the death table\nDoes add death coins\nDoes NOT send a death link")]
    public static LogReturn AddDeaths(string[] args)
    {
        if (Client.Connection.Connected == -1) return new LogReturn("Login first");
        if (args.Length < 1) return new LogReturn("Not Enough Arguments", SoftError);
        var slot = args[0];
        var amount = args.Length < 2 ? 1 : int.TryParse(args[1], out var amt) ? amt : 1;
        Client.AddDeath(slot, amount);
        return new LogReturn($"Added [{amount}] to [{slot}]");
    }

    [Command("refreshlocations"), Help("Refreshes the locations, shouldn't need to do this")]
    public static LogReturn ReloadLocations(string[] args)
    {
        if (Client.Connection.Connected == -1) return new LogReturn("Login first");
        Client.Connection.ReloadLocations();
        return new LogReturn("Reloaded Locations");
    }

    [Command("syncdeathswithserver"),
     Help("Syncs the death count with the server\nThis is a way to share deaths with other deathlinkipelago clients\nThis is considered cheating")]
    public static LogReturn SyncServerDeaths(string[] args)
    {
        if (Client.Connection.Connected == -1) return new LogReturn("Login first");

        try
        {
            var deathCount = Client.Connection.Session.DataStorage[Scope.Global, "Deathlinkipelago_Deaths"];
            if (deathCount is not null && deathCount != "" && deathCount != "()")
            {
                var deathDict = deathCount.To<Dictionary<string, int>>() ?? [];
                foreach (var (slot, amount) in deathDict)
                {
                    if (Client.Deaths.TryGetValue(slot, out var deaths) && deaths >= amount) continue;
                    Client.AddDeath(slot, amount - deaths);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(e);
            return new LogReturn("Sync failed, Contact dev");
        }

        Client.OrderDeaths();
        Client.Connection.Session.DataStorage[Scope.Global, "Deathlinkipelago_Deaths"] = JObject.FromObject(Client.Deaths);
        return new LogReturn("Deaths Synced");
    }

    [Command("setscrollbacklength"), Help("setscrollbacklenth [amount = 200]\nDefault is 200\nSets max scrollback for console and text client\nIf you want this setting to save, it must be entered before connecting")]
    public static LogReturn SetScrollbackLength(string[] args)
    {
        var amount = args.Length < 1 ? 200 : int.TryParse(args[0], out var amt) ? amt : 200;
        GameConsole.MaxScrollback = amount;
        return new LogReturn($"Set length to [{amount}] ({(Client.Connection.Connected == -1 ? "Connect to save value" : "Value will not be saved")})");
    }
}