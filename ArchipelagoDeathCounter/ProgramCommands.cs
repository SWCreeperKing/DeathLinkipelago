using Archipelago.MultiClient.Net.Enums;
using CreepyUtil.Archipelago.UIBackbone.LoggerConsole;
using CreepyUtil.Archipelago.UIBackbone.LoggerConsole.Commands;
using Newtonsoft.Json.Linq;
using static CreepyUtil.Archipelago.UIBackbone.LoggerConsole.Logger.Level;
using static UserInterface;

namespace ArchipelagoDeathCounter;

public static class ProgramCommands
{
    [Command("death"),
     Help(
         "death [slot] [amount = 1]\nAdds [amount] to [slot] in the death table\nDoes add death coins\nDoes NOT send a death link")]
    public static LogReturn AddDeaths(string[] args)
    {
        if (IsNotConnected()) return new LogReturn("Login first");
        if (args.Length < 1) return new LogReturn("Not Enough Arguments", SoftError);
        var slot = string.Join(" ", args[..^1]);
        var amount = args.Length < 2 ? 1 : int.TryParse(args[^1], out var amt) ? amt : 1;
        Client.AddDeath(slot, amount);
        return new LogReturn($"Added [{amount}] to [{slot}]");
    }

    [Command("refreshlocations"), Help("Refreshes the locations, shouldn't need to do this")]
    public static LogReturn ReloadLocations(string[] args)
    {
        if (IsNotConnected()) return new LogReturn("Login first");
        Client.ReloadLocations();
        return new LogReturn("Reloaded Locations");
    }

    [Command("syncdeathswithserver"),
     Help(
         "Syncs the death count with the server\nThis is a way to share deaths with other deathlinkipelago clients\nThis is considered cheating")]
    public static LogReturn SyncServerDeaths(string[] args)
    {
        if (IsNotConnected()) return new LogReturn("Login first");

        try
        {
            var deathCount = Client.Session.DataStorage[Scope.Global, "Deathlinkipelago_Deaths"];
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
        Client.Session.DataStorage[Scope.Global, "Deathlinkipelago_Deaths"] = JObject.FromObject(Client.Deaths);
        return new LogReturn("Deaths Synced");
    }

    [Command("setscrollbacklength"),
     Help(
         "setscrollbacklenth [amount = 200]\nDefault is 200\nSets max scrollback for console and text client\nIf you want this setting to save, it must be entered before connecting")]
    public static LogReturn SetScrollbackLength(string[] args)
    {
        var amount = args.Length < 1 ? 200 : int.TryParse(args[0], out var amt) ? amt : 200;
        GameConsole.MaxScrollback = amount;
        return new LogReturn(
            $"Set length to [{amount}] ({(IsNotConnected() ? "Connect to save value" : "Value will not be saved")})");
    }

    public static bool IsNotConnected() => Client is null || !Client.IsConnected;
}