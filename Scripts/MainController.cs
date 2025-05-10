using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using CreepyUtil.Archipelago;
using DeathLinkipelago.Scripts.Charts;
using DeathLinkipelago.Scripts.Commands;
using DeathLinkipelago.Scripts.Tables;
using Godot;
using Newtonsoft.Json;
using static DeathLinkipelago.Scripts.DeathTracker;
using Environment = System.Environment;

namespace DeathLinkipelago.Scripts;

public partial class MainController : Node
{
    public const string ApWorldCompatibilityVersion = "v.0.13.0";

    public static string SaveDir =
        $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/DeathLinkipelago";

    public static readonly Random Random = new();

    public static readonly string[] FunnyButtonMessages =
    [
        "Uh oh, you pressed the ~~goku~~ funny button", "Stupidity", "How did you survive? Funny button",
        "I fell down some stairs, shut up or I'll through you down a flight", "I tripped up the stairs",
        "Stepped on a crack", "Just F**king died, just like that",
        "I fell like some of these messages are too long, bah, it'll be fineeee~~~"
    ]; // totally not referencing dbza at all (total of 3 times) 

    public static ApClient Client;
    public static Config Config;
    public static ConcurrentDictionary<string, int> DeathCounter = [];
    public static double LastSave;
    public static double LastDeathTrap;
    public static double NextLifeCoin;
    public static int ShopLevel = 1;
    public static bool HasChangedSinceLastSave;
    public static long[] HintedItems = [];
    public static HashSet<long> PrioritizedItems = [];
    public static bool CanGoal;
    public static bool HasGoaled;

    public static Dictionary<string, int> Inventory = new()
    {
        ["Death Trap"] = 0,
        ["Death Shield"] = 0,
        ["Death Coin"] = 0,
        ["Life Coin"] = 0,
    };

    public static Dictionary<string, int> InventoryUsed = new(Inventory);
    public static Dictionary<string, int> LifeShopBought = new(Inventory);
    public static bool RefreshUI;

    [Export] private Control[] _Scenes = [];
    [Export] private Button _FunnyButton;
    [Export] private BasicTextTable _Inventory;
    [Export] private BasicTextTable _Deaths;
    [Export] private Label _GrassStatus;
    [Export] private Label _LifeCoinStatus;
    [Export] private Label _SaveStatus;
    [Export] private Label _ShopLevel;
    [Export] private Label _ShopItems;
    [Export] private DeathShopTable _DeathShop;
    [Export] private LifeShopTable _LifeShop;
    [Export] private Login _Login;
    [Export] public DeathTracker Tracker;

    public override void _EnterTree()
    {
        if (!Directory.Exists(SaveDir))
        {
            Directory.CreateDirectory(SaveDir);
        }
        else if (File.Exists($"{SaveDir}/data.json"))
        {
            var (address, password, slot, port) = JsonConvert.DeserializeObject<LoginInfo>(File
               .ReadAllText($"{SaveDir}/data.json")
               .Replace("\r", "")
               .Replace("\n", ""));

            _Login.Address = address;
            _Login.Password = password;
            _Login.Slot = slot;
            _Login.PortField = port;
        }
    }

    public override void _Ready()
    {
        _FunnyButton.Pressed += () => SendDeath(FunnyButtonMessages[Random.Next(FunnyButtonMessages.Length)]);
        SwitchScene(0);
    }

    public override void _Process(double delta)
    {
        if (LastDeathTrap > 0) LastDeathTrap -= delta;
        if (LastSave > 0) LastSave -= delta;
        if (NextLifeCoin > 0) NextLifeCoin -= delta;

        Client?.UpdateConnection();
        if (Client?.Session?.Socket is null || !Client.IsConnected || Config is null) return;

        if (Client.PushUpdatedVariables(false, out var hints))
        {
            HintedItems = hints
                         .Where(hint => hint.FindingPlayer == Client.PlayerSlot && hint.Status == HintStatus.Priority)
                         .Select(hint => hint.LocationId - Client.UUID)
                         .ToArray();
            RefreshUI = true;
        }

        foreach (var item in Client.GetOutstandingItems())
        {
            switch (item!.ItemName)
            {
                case "Death Grass":
                    CanGoal = true;
                    GoalCheck();
                    continue;
                case "Progressive Death Shop":
                    ShopLevel++;
                    RefreshUI = true;
                    continue;
            }

            if (!Inventory.ContainsKey(item!.ItemName)) continue;
            Inventory[item.ItemName]++;
            RefreshUI = true;
        }

        _GrassStatus.Text = $"Can touch grass? [{(CanGoal ? "yes!" : "not yet")}]"; 

        if (LastDeathTrap <= 0 && this["Death Trap"] > 0)
        {
            LastDeathTrap = 1.5f;
            if (this["Death Shield"] <= 0)
            {
                if (Client.MissingLocations.Count != 0 || Config.SendTrapsAfterGoal)
                {
                    SendDeath("Will of God");
                }
            }
            else
            {
                InventoryUsed["Death Shield"]++;
                Client.Say("The Death Trap was shielded");
            }

            InventoryUsed["Death Trap"]++;
            RefreshUI = true;
        }

        if (LastSave <= 0 && HasChangedSinceLastSave)
        {
            HasChangedSinceLastSave = false;
            LastSave = 15;
            SaveSlotData();
        }

        _SaveStatus.Text = LastSave <= 0 ? "Can Autosave" : $"Can auto save again in [{LastSave.GetAsTime()}]";
        _SaveStatus.Modulate = LastSave <= 0 ? Colors.Green : Colors.OrangeRed;

        if (Config.SecondsPerLifeCoin != 0)
        {
            if (NextLifeCoin <= 0)
            {
                Inventory["Life Coin"]++;
                NextLifeCoin = Config.SecondsPerLifeCoin;
                RefreshUI = true;
            }

            _LifeCoinStatus.Text = $"Next life coin in [{NextLifeCoin.GetAsTime()}]";
        }

        if (!RefreshUI) return;
        RefreshUI = false;

        _FunnyButton.Visible = Client is not null && Client.IsConnected && Config.HasFunnyButton;
        _ShopLevel.Text = $"Shop Lv.{ShopLevel}/{Math.Ceiling(Config.DeathCheckAmount / 10f)}";
        var totalItems = Config.DeathCheckAmount;
        var gottenItems = totalItems - Client.MissingLocations.Count;
        _ShopItems.Text =
            $"Items Bought [{gottenItems}/{Config.DeathCheckAmount}] ({(double)gottenItems / totalItems:##0.##%})";
        _Inventory.UpdateData(Inventory
                             .Select(kv =>
                              {
                                  var key = kv.Key;
                                  var used = InventoryUsed[key];
                                  var total = Inventory[key];
                                  return new[]
                                      { key, $"{total - used}", $"{used}", $"{total}" };
                              })
                             .ToList());
        _Deaths.UpdateData(DeathCounter.OrderByDescending(kv => kv.Value)
                                       .Select(kv => new[]
                                        {
                                            $"[color={BlameChart.MakeRandomColor(kv.Key).ToHtml()}]{kv.Key.Replace("[", "[lb]")}[/color]",
                                            $"{kv.Value}"
                                        })
                                       .ToList());
        _DeathShop.Update(Client.MissingLocations);
        _LifeShop.Update();

        HasChangedSinceLastSave = true;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest) return;
        SaveSlotData();
        File.WriteAllText($"{SaveDir}/data.json", JsonConvert.SerializeObject(_Login.GetLoginInfo()));
        GetTree().Quit();
    }

    public void SwitchScene(int index)
    {
        foreach (var node in _Scenes)
        {
            node.Visible = false;
        }

        _Scenes[index].Visible = true;
    }

    public void Connected()
    {
        HasGoaled = false;
        CanGoal = false;
        Client.OnDeathLinkPacketReceived += (_, packet) =>
        {
            var source = packet.Data.TryGetValue("source", out var sourceToken)
                ? sourceToken.ToString()
                : "Unknown";
            AddDeath(source);
            RefreshUI = true;
        };
        Reset();
        LoadSlotData();
        SwitchScene(1);
        BlameChart.RefreshUi = true;
        GoalCheck();
        Client.SendLocation(0);
        Client.RegisterCommand(new BuyCommand());
        Client.RegisterCommand(new InventoryCheckCommand());
    }

    public static void GoalCheck()
    {
        if (Client is null || !Client.IsConnected || !Client.Session.Socket.Connected) return;
        if (Client.MissingLocations.Count != 0 || !CanGoal || HasGoaled) return;
        HasGoaled = true;
        Client.Goal();
    }

    public void LoadSlotData()
    {
        var slotData = Client.SlotData;
        Config = new Config((long)(NextLifeCoin = (long)slotData["seconds_per_life_coin"]),
            (long)slotData["death_check_amount"],
            (bool)slotData["send_traps_after_goal"],
            (bool)slotData["has_funny_button"],
            (bool)slotData["use_global_counter"],
            (bool)slotData["send_scout_hints"]);

        _LifeCoinStatus.Visible = Config.SecondsPerLifeCoin != 0;
        PrioritizedItems = Client.GetFromStorage("prioritized items", def: PrioritizedItems);
        InventoryUsed = Client.GetFromStorage("inventory", def: InventoryUsed);
        var inv = Client.GetFromStorage("life inventory", def: Inventory);
        LifeShopBought = new Dictionary<string, int>(inv);
        Inventory = inv;
        Inventory["Life Coin"] = Client.GetFromStorage("life coins", def: 0);

        UpdateDeaths();
        RefreshUI = true;
    }

    public void SaveSlotData()
    {
        if (Client is null || !Client.IsConnected) return;
        Client.SendToStorage("death linking counter", DeathCounter,
            Config.UseGlobalCounter ? Scope.Global : Scope.Slot);
        Client.SendToStorage("inventory", InventoryUsed);
        Client.SendToStorage("life coins", Inventory["Life Coin"]);
        Client.SendToStorage("life inventory", LifeShopBought);
        Client.SendToStorage("prioritized items", PrioritizedItems);
    }

    public void UpdateDeaths()
    {
        var rawCounter = Client.GetFromStorage<Dictionary<string, int>>("death linking counter",
            Config.UseGlobalCounter ? Scope.Global : Scope.Slot, []);

        if (rawCounter.Count != 0)
        {
            foreach (var (key, value) in rawCounter)
            {
                if (DeathCounter.TryAdd(key, value)) continue;
                DeathCounter[key] = Math.Max(DeathCounter[key], value);
            }
        }

        Inventory["Death Coin"] = DeathCounter.Values.Sum() + LifeShopBought["Death Coin"];
        HasChangedSinceLastSave = true;
        RefreshUI = true;
    }

    public void SendDeath(string reason)
    {
        if (Client is null || !Client.IsConnected) return;
        Client.SendDeathLink(reason);
    }

    public static void AddDeath(string source)
    {
        Inventory["Death Coin"]++;
        PlayerDied(source);
        DeathCounter.TryAdd(source, 0);
        DeathCounter[source]++;
        NextLifeCoin = Config.SecondsPerLifeCoin;
    }

    public void Disconnect() => _Login.TryDisconnection();

    public static int GetItemAmount(string item) => Inventory[item] - InventoryUsed[item];
    public int this[string item] => GetItemAmount(item);

    public static void BuyItem(int itemNumber)
    {
        Client.SendLocation(itemNumber);
        InventoryUsed["Death Coin"]++;
        GoalCheck();
    }
}