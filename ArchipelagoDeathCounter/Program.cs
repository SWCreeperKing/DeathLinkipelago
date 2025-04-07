using System.Numerics;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Backbone;
using ImGuiNET;
using Newtonsoft.Json;
using static Backbone.Backbone;
using Color = Raylib_cs.Color;

UserInterface ui = new();
ui.Loop();

class UserInterface() : Backbone.Backbone("DeathLinkipelago", 650, 700)
{
    public int Counter = 0;
    private ArchipelagoClient Client = new();
    private long LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public override void Init() { Client.Init(); }

    public override void Update()
    {
        var nextUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var delta = nextUpdate - LastUpdate;
        Client.Update(delta / 1000f);
        LastUpdate = nextUpdate;
    }

    public override void Render() { Client.Render(); }
}

class ArchipelagoClient
{
    public const long UUID = 0x0AF5F0AC;

    public ArchipelagoSession Session;
    public DeathLinkService DeathLink;

    public Vector4 Red = Color.Red.ToV4();
    public Vector4 Green = Color.Green.ToV4();
    public string Address = "archipelago.gg";
    public string Password = "";
    public string Slot = "Slot";
    public string Game = "DeathLinkipelago";
    public int Port = 12345;
    public string[]? Error;
    public int Connected = -1;
    public bool HasChangedSinceSave;
    public float SaveCooldown = 3;
    public float DeathCooldown = 30;
    public bool HasDeathButton;
    public string[] PlayerNames = [];
    public Dictionary<string, object> SlotData = [];

    public Dictionary<string, int> Deaths = [];

    public Dictionary<string, int> UsedItems = new()
    {
        ["Death Trap"] = 0,
        ["Death Shield"] = 0,
        ["Death Coin"] = 0
    };

    public Dictionary<string, int> HeldItems = new()
    {
        ["Progressive Death Shop"] = 0,
        ["Death Trap"] = 0,
        ["Death Shield"] = 0,
        ["Death Coin"] = 0
    };

    public Dictionary<long, ScoutedItemInfo> Locations = [];

    public void Init()
    {
        if (!File.Exists("init.txt")) return;
        var file = File.ReadAllText("init.txt").Replace("\r", "").Split('\n');
        Slot = file[0];
        Port = int.TryParse(file[1], out var port) ? port : Port;
    }

    public void Update(float deltaTime)
    {
        if (Connected == -1) return;

        while (Session.Items.Any())
        {
            var item = Session.Items.DequeueItem();
            HeldItems[item.ItemName]++;
        }

        if (DeathCooldown < 3)
        {
            DeathCooldown += deltaTime;
        }
        else if (GetItemAmount("Death Trap") > 0)
        {
            if (GetItemAmount("Death Shield") > 0)
            {
                UsedItems["Death Shield"]++;
            }
            else
            {
                DeathLink.SendDeathLink(new(Slot, "The Will of God"));
                Deaths.TryAdd(Slot, 0);
                Deaths[Slot]++;
            }

            UsedItems["Death Trap"]++;
            HasChangedSinceSave = true;
            DeathCooldown = 0;
        }

        if (SaveCooldown < 30)
        {
            SaveCooldown += deltaTime;
        }

        if (!HasChangedSinceSave || SaveCooldown < 30) return;
        HasChangedSinceSave = false;
        Session.DataStorage[Scope.Slot, "Used"] =
            $"{UsedItems["Death Trap"]}|{UsedItems["Death Shield"]}|{UsedItems["Death Coin"]}";
        Session.DataStorage[Scope.Slot, "Deaths"] = JsonConvert.SerializeObject(Deaths);
        SaveCooldown = 0;
    }

    public void Render()
    {
        switch (Connected)
        {
            case -1:
                RenderLogin();
                break;
            default:
                if (HasDeathButton && ImGui.Button("Send Death"))
                {
                    DeathLink.SendDeathLink(new(Slot, "Stupidity"));
                }

                if (HasChangedSinceSave && SaveCooldown < 30)
                {
                    ImGui.TextColored(Red, "HAS NOT SAVED! DO NOT CLOSE");
                }
                else
                {
                    ImGui.TextColored(Green, "Safe to close");
                }

                if (Locations.Count != 0)
                {
                    ImGui.Text($"Victory: {Locations.Count} Remaining");
                }
                else
                {
                    ImGui.TextColored(Green, "Victory!");
                }

                ImGui.NewLine();

                if (ImGui.BeginTable("Item Table", 4, TableFlags))
                {
                    ImGui.TableSetupColumn("Item");
                    ImGui.TableSetupColumn("Available");
                    ImGui.TableSetupColumn("Used");
                    ImGui.TableSetupColumn("Total");
                    ImGui.TableHeadersRow();
                    foreach (var (item, amount) in HeldItems)
                    {
                        if (item == "Progressive Death Shop") continue;
                        var trueAmount = amount - UsedItems[item];
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(item);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{trueAmount}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{UsedItems[item]}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{HeldItems[item]}");
                        ImGui.TableNextColumn();
                    }
                }

                ImGui.EndTable();
                ImGui.NewLine();

                if (ImGui.BeginTable("Death Table", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("To Blame");
                    ImGui.TableSetupColumn("Amount");
                    ImGui.TableHeadersRow();
                    foreach (var (blame, amount) in Deaths)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(blame);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{amount}");
                        ImGui.TableNextColumn();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Total");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{Deaths.Values.Sum()}");
                }

                ImGui.EndTable();
                ImGui.NewLine();

                if (ImGui.BeginTable("Shop Table", 4, TableFlags | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Loc");
                    ImGui.TableSetupColumn("Item");
                    ImGui.TableSetupColumn("For");
                    ImGui.TableSetupColumn("Buy");
                    ImGui.TableHeadersRow();
                    foreach (var (id, location) in Locations)
                    {
                        if (id >= (HeldItems["Progressive Death Shop"] + 1) * 10) break;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($" Item {id + 1} ");
                        ImGui.TableNextColumn();
                        ImGui.Text($" {location.ItemName} ".Replace("Trap", "Sheild"));
                        ImGui.TableNextColumn();
                        ImGui.Text($" {location.Player.Name} ");
                        ImGui.TableNextColumn();

                        if (ImGui.Button($"Buy Item {id + 1}"))
                        {
                            BuyCheck(id);
                        }
                        
                        ImGui.TableNextColumn();
                    }
                }

                ImGui.EndTable();
                
                break;
        }
    }

    public void RenderLogin()
    {
        ImGui.InputText("Address", ref Address, 255);
        ImGui.InputInt("Port", ref Port);
        ImGui.InputText("Password", ref Password, 255);
        ImGui.InputText("Slot", ref Slot, 255);
        if (ImGui.Button("Connect"))
        {
            Error = null;
            try
            {
                Session = ArchipelagoSessionFactory.CreateSession(Address, Port);
                DeathLink = Session.CreateDeathLinkService();

                var result = Session.TryConnectAndLogin(Game, Slot,
                    ItemsHandlingFlags.RemoteItems | ItemsHandlingFlags.IncludeOwnItems, tags: ["DeathLink"],
                    password: Password);

                DeathLink.OnDeathLinkReceived += OnDeathLink;

                if (!result.Successful)
                {
                    Error = ((LoginFailure)result).Errors;
                    return;
                }

                if (result is not LoginSuccessful loginSuccessful)
                {
                    Error = ["Login was not Successful (idk why)"];
                    return;
                }

                SlotData = loginSuccessful.SlotData;
                OnConnection();
            }
            catch (Exception e)
            {
                Error = new LoginFailure(e.GetBaseException().Message).Errors;
            }
        }

        if (Error is null) return;
        foreach (var line in Error)
        {
            ImGui.TextColored(Red, line);
        }
    }

    public void OnConnection()
    {
        File.WriteAllLines("init.txt", [Slot, $"{Port}"]);

        Connected++;

        PlayerNames = Session.Players.AllPlayers.Select(player => $"{player.Name}").ToArray();

        var missing = Session.Locations.AllMissingLocations.ToArray();
        var scoutedLocations = Session.Locations.ScoutLocationsAsync(missing).Result!;

        foreach (var (id, location) in scoutedLocations.OrderBy(loc => loc.Key))
        {
            Locations[id - UUID] = location;
        }
        
        HasDeathButton = (bool)SlotData["has_funny_button"];

        var deathCount = Session.DataStorage[Scope.Slot, "Deaths"];
        if (deathCount != "")
        {
            Deaths = JsonConvert.DeserializeObject<Dictionary<string, int>>(deathCount)!;
            HeldItems["Death Coin"] = Deaths.Values.Sum();
        }

        if (scoutedLocations.Count == 0)
        {
            StatusUpdatePacket status = new()
            {
                Status = ArchipelagoClientState.ClientGoal
            };
            Session.Socket.SendPacket(status);
        }

        var used = Session.DataStorage[Scope.Slot, "Used"].To<string>();
        if (used == "") return;
        var usedSplit = used.Split('|').Select(int.Parse).ToArray();
        UsedItems["Death Trap"] = usedSplit[0];
        UsedItems["Death Shield"] = usedSplit[1];
        UsedItems["Death Coin"] = usedSplit[2];
    }

    public void OnDeathLink(DeathLink deathLink)
    {
        var toBlame = deathLink.Source;

        if (!PlayerNames.Contains(toBlame))
        {
            toBlame = PlayerNames.First(name => toBlame.EndsWith($" ({name})"));
        }

        Deaths.TryAdd(toBlame, 0);
        Deaths[toBlame]++;
        HeldItems["Death Coin"]++;
        HasChangedSinceSave = true;
    }

    public void BuyCheck(long id)
    {
        if (Locations.Count == 0) return;
        if (GetItemAmount("Death Coin") < 1) return;

        var item = Locations[id];
        Session.Locations.CompleteLocationChecks(item.LocationId);
        Locations.Remove(id);
        UsedItems["Death Coin"]++;

        HasChangedSinceSave = true;
        if (Locations.Count != 0) return;
        StatusUpdatePacket status = new()
        {
            Status = ArchipelagoClientState.ClientGoal
        };
        Session.Socket.SendPacket(status);
    }

    public int GetItemAmount(string item) => HeldItems[item] - UsedItems[item];
}