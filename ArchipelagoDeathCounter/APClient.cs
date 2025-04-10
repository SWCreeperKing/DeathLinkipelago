using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDeathCounter.LoggerConsole;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using static ArchipelagoClient;
using static UserInterface;

namespace ArchipelagoDeathCounter;

public class APConnection
{
    public ArchipelagoSession Session;
    public string Address = "archipelago.gg";
    public string Password = "";
    public string Slot = "Slot";
    public string Game = "DeathLinkipelago";
    public int PlayerSlot;
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
        Session.DataStorage[Scope.Slot, "Deaths"] = JObject.FromObject(deaths);
    }

    public void LoadInitData()
    {
        if (!File.Exists("init.txt")) return;
        var file = File.ReadAllText("init.txt").Replace("\r", "").Split('\n');
        Slot = file[0];
        Port = int.TryParse(file[1], out var port) ? port : Port;
        if (file.Length >= 3)
        {
            GameConsole.MaxScrollback = int.TryParse(file[2], out var amt) ? amt : 200;
        }
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
        Session.SetGoalAchieved();
    }

    public string[]? TryConnect()
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
            OnConnection();
        }
        catch (Exception e)
        {
            return new LoginFailure(e.GetBaseException().Message).Errors;
        }

        return null;
    }

    public void OnConnection()
    {
        File.WriteAllLines("init.txt", [Slot, $"{Port}", $"{GameConsole.MaxScrollback}"]);
        Connected++;

        PlayerSlot = Session.Players.ActivePlayer.Slot;
        Client.PlayerNames = Session.Players.AllPlayers.Select(player => $"{player.Name}").ToArray();
        Client.PlayerGames = Session.Players.AllPlayers.Select(player => player.Game).ToArray();
        Client.HasDeathButton = (bool)SlotData["has_funny_button"];
        Client.SendTrapsAfterGoal = (bool)SlotData["send_traps_after_goal"];

        Session.DataStorage.TrackHints(hints
            => Client.Hints =
                hints
                   .OrderBy(hint
                        => hint.ReceivingPlayer == PlayerSlot
                            ? Client.PlayerNames.Length + 1
                            : hint.ReceivingPlayer)
                   .ThenBy(hint => GetStatusNum(hint.Status))
                   .ThenBy(hint => GetItemNum(hint.ItemFlags))
                   .ThenBy(hint
                        => hint.FindingPlayer == PlayerSlot
                            ? Client.PlayerNames.Length + 1
                            : hint.FindingPlayer)
                   .ThenBy(hint => hint.LocationId)
                   .ToArray());

        ReloadLocations();
        Session.Socket.PacketReceived += OnPacketReceived;

        try
        {
            var deathCount = Session.DataStorage[Scope.Slot, "Deaths"];
            if (deathCount is not null && deathCount != "" && deathCount != "()")
            {
                Client.Deaths = deathCount.To<Dictionary<string, int>>() ?? [];
                HeldItems["Death Coin"] = Client.Deaths.Values.Sum();
            }
        }
        catch (Exception e)
        {
            Logger.Log(e);
            Client.Deaths = [];
        }

        var used = Session.DataStorage[Scope.Slot, "Used"].To<string>();
        if (used == "") return;
        var usedSplit = used.Split('|').Select(int.Parse).ToArray();
        UsedItems["Death Trap"] = usedSplit[0];
        UsedItems["Death Shield"] = usedSplit[1];
        UsedItems["Death Coin"] = usedSplit[2];
    }

    public void ReloadLocations()
    {
        var missing = Session.Locations.AllMissingLocations.ToArray();
        var scoutedLocations = Session.Locations.ScoutLocationsAsync(missing).Result!;

        foreach (var (id, location) in scoutedLocations.OrderBy(loc => loc.Key))
        {
            Locations[id - UUID] = location;
        }

        if (scoutedLocations.Count != 0) return;
        Session.SetGoalAchieved();
    }

    public void SendDeathLink(string cause)
    {
        Logger.Log("Sending Deathlink");
        Session.Socket.SendPacketAsync(new BouncePacket
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

    public void OnPacketReceived(ArchipelagoPacketBase packet)
    {
        switch (packet)
        {
            case ChatPrintJsonPacket message:
                MessageClient.SendMessage(new ChatMessagePacket(message));
                break;
            case BouncedPacket bouncedPacket:
                if (!bouncedPacket.Tags.Contains("DeathLink")) return;
                var source = bouncedPacket.Data.TryGetValue("source", out var sourceToken)
                    ? sourceToken.ToString()
                    : "Unknown";
                Logger.Log($"Received Deathlink: [{JsonConvert.SerializeObject(bouncedPacket.Data)}]");
                Client.AddDeath(source);
                break;
            case PrintJsonPacket updatePacket:
                Logger.Log(JsonConvert.SerializeObject(updatePacket.Data));
                if (updatePacket.Data.Length == 1)
                {
                    MessageClient.SendMessage(new ServerMessagePacket(updatePacket.Data.First().Text!));
                }

                if (updatePacket.Data.Length < 2) break;
                if (updatePacket.Data.First().Text!.StartsWith("[Hint]: "))
                {
                    if (updatePacket.Data.Last().HintStatus!.Value == HintStatus.Found) break;
                    MessageClient.SendMessage(new HintMessagePacket(updatePacket.Data));
                }
                else if (updatePacket.Data[1].Text is " found their " or " sent ")
                {
                    ItemLog.SendMessage(updatePacket.Data.Select(mp => new MessagePart(mp)).ToArray());
                }

                break;
        }
    }

    public void PrintPlayerName(int slot)
        => ImGui.TextColored(
            slot == PlayerSlot ? DarkPurple : slot == 0 ? Gold : DirtyWhite, Client.PlayerNames[slot]);

    public int GetStatusNum(HintStatus status)
        => status switch
        {
            HintStatus.Found => 0,
            HintStatus.Unspecified => 3,
            HintStatus.NoPriority => 2,
            HintStatus.Avoid => 4,
            HintStatus.Priority => 1,
        };

    public int GetItemNum(ItemFlags item)
    {
        if (item.HasFlag(Advancement)) return 0;
        if (item.HasFlag(Trap)) return 10;
        return item.HasFlag(NeverExclude) ? 1 : 2;
    }
}