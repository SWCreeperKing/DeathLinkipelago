using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArchipelagoDeathCounter;

public class APConnection
{
    public ArchipelagoSession Session;
    public string Address = "archipelago.gg";
    public string Password = "";
    public string Slot = "Slot";
    public string Game = "DeathLinkipelago";
    public int Port = 12345;
    public int Connected = -1;
    public Dictionary<string, object> SlotData = [];
    public Dictionary<long, ScoutedItemInfo> Locations = [];
    public APInventory Inventory = new();

    public Dictionary<string, int> HeldItems
    {
        get => Inventory.HeldItems;
        set => Inventory.HeldItems = value;
    }

    public Dictionary<string, int> UsedItems
    {
        get => Inventory.UsedItems;
        set => Inventory.UsedItems = value;
    }

    public int GetItemAmount(string item) => Inventory.GetItemAmount(item);

    public void SaveData(Dictionary<string, int> deaths)
    {
        Session.DataStorage[Scope.Slot, "Used"] =
            $"{UsedItems["Death Trap"]}|{UsedItems["Death Shield"]}|{UsedItems["Death Coin"]}";
        Session.DataStorage[Scope.Slot, "Deaths"] = JsonConvert.SerializeObject(deaths);
    }

    public void LoadInitData()
    {
        if (!File.Exists("init.txt")) return;
        var file = File.ReadAllText("init.txt").Replace("\r", "").Split('\n');
        Slot = file[0];
        Port = int.TryParse(file[1], out var port) ? port : Port;
    }

    public void Update()
    {
        while (Session.Items.Any())
        {
            var item = Session.Items.DequeueItem();
            Inventory.HeldItems[item.ItemName]++;
        }
    }

    public void BuyCheck(long id, ArchipelagoClient apClient)
    {
        if (Locations.Count == 0) return;
        if (GetItemAmount("Death Coin") < 1) return;

        var item = Locations[id];
        Session.Locations.CompleteLocationChecks(item.LocationId);
        Locations.Remove(id);
        UsedItems["Death Coin"]++;

        apClient.HasChangedSinceSave = true;
        if (Locations.Count != 0) return;
        StatusUpdatePacket status = new()
        {
            Status = ArchipelagoClientState.ClientGoal
        };
        Session.Socket.SendPacket(status);
    }

    public string[]? TryConnect(ArchipelagoClient apClient)
    {
        try
        {
            Session = ArchipelagoSessionFactory.CreateSession(Address, Port);

            var result = Session.TryConnectAndLogin(Game, Slot,
                ItemsHandlingFlags.RemoteItems | ItemsHandlingFlags.IncludeOwnItems, tags: ["DeathLink"],
                password: Password);

            if (!result.Successful) return ((LoginFailure)result).Errors;

            if (result is not LoginSuccessful loginSuccessful) return ["Login was not Successful (idk why)"];

            SlotData = loginSuccessful.SlotData;
            OnConnection(apClient);
        }
        catch (Exception e)
        {
            return new LoginFailure(e.GetBaseException().Message).Errors;
        }

        return null;
    }

    public void OnConnection(ArchipelagoClient apClient)
    {
        File.WriteAllLines("init.txt", [Slot, $"{Port}"]);

        Connected++;

        apClient.PlayerNames = Session.Players.AllPlayers.Select(player => $"{player.Name}").ToArray();

        var missing = Session.Locations.AllMissingLocations.ToArray();
        var scoutedLocations = Session.Locations.ScoutLocationsAsync(missing).Result!;

        foreach (var (id, location) in scoutedLocations.OrderBy(loc => loc.Key))
        {
            Locations[id - ArchipelagoClient.UUID] = location;
        }

        apClient.HasDeathButton = (bool)SlotData["has_funny_button"];
        apClient.SendTrapsAfterGoal = (bool)SlotData["send_traps_after_goal"];

        var deathCount = Session.DataStorage[Scope.Slot, "Deaths"];
        if (deathCount != "")
        {
            apClient.Deaths = JsonConvert.DeserializeObject<Dictionary<string, int>>(deathCount)!;
            HeldItems["Death Coin"] = apClient.Deaths.Values.Sum();
        }

        if (scoutedLocations.Count == 0)
        {
            StatusUpdatePacket status = new()
            {
                Status = ArchipelagoClientState.ClientGoal
            };
            Session.Socket.SendPacket(status);
        }

        Session.Socket.PacketReceived += packet =>
        {
            if (packet is not BouncedPacket bouncedPacket) return;
            if (!bouncedPacket.Tags.Contains("DeathLink")) return;
            var source = bouncedPacket.Data.TryGetValue("source", out var sourceToken)
                ? sourceToken.ToString()
                : "Unknown";
            apClient.AddDeath(source);
        };

        var used = Session.DataStorage[Scope.Slot, "Used"].To<string>();
        if (used == "") return;
        var usedSplit = used.Split('|').Select(int.Parse).ToArray();
        UsedItems["Death Trap"] = usedSplit[0];
        UsedItems["Death Shield"] = usedSplit[1];
        UsedItems["Death Coin"] = usedSplit[2];
    }

    public void SendDeathLink(string cause)
        => Session.Socket.SendPacketAsync(new BouncePacket
                   {
                       Tags = new List<string> { "DeathLink" },
                       Data = new Dictionary<string, JToken>
                       {
                           { "time", DateTime.UtcNow.ToUnixTimeStamp() },
                           { "source", Slot },
                           { "cause", cause }
                       }
                   })
                  .GetAwaiter()
                  .GetResult();
}