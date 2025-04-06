using System.Numerics;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Backbone;
using ImGuiNET;
using Newtonsoft.Json;
using static Backbone.Backbone;
using Color = Raylib_cs.Color;

UserInterface ui = new();
ui.Loop();

class UserInterface() : Backbone.Backbone("DeathLinkipelago", 500, 550)
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
    public long MaxLocations;
    public long LocationsChecked;
    public bool HasDeathButton;
    public string[] PlayerNames = [];
    public Dictionary<string, object> SlotData = [];

    public Dictionary<string, int> Deaths = [];

    public Dictionary<string, int> UsedItems = new()
    {
        ["The Urge to Die"] = 1,
        ["Death Trap"] = 0,
        ["Death Shield"] = 0
    };

    public Dictionary<string, int> HeldItems = new()
    {
        ["The Urge to Die"] = 0,
        ["Death Trap"] = 0,
        ["Death Shield"] = 0
    };

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
        CheckChecks();

        while (Session.Items.Any())
        {
            var item = Session.Items.DequeueItem();
            HeldItems[item.ItemName]++;
        }

        if (DeathCooldown < 3)
        {
            DeathCooldown += deltaTime;
        }
        else if (HeldItems["Death Trap"] - UsedItems["Death Trap"] > 0)
        {
            if (HeldItems["Death Shield"] - UsedItems["Death Shield"] > 0)
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
        Session.DataStorage[Scope.Slot, "Used"] = $"{UsedItems["Death Trap"]}|{UsedItems["Death Shield"]}";
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

                if (LocationsChecked < MaxLocations)
                {
                    ImGui.Text(
                        $"Next Check: {LocationsChecked + 1} Total Deaths | Victory: {MaxLocations} Total Deaths");
                }
                else
                {
                    ImGui.TextColored(Green, "Victory!");
                }

                if (ImGui.BeginTable("Item Table", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Item");
                    ImGui.TableSetupColumn("Amount");
                    ImGui.TableHeadersRow();
                    foreach (var (item, amount) in HeldItems)
                    {
                        var trueAmount = amount - UsedItems[item];
                        if (trueAmount <= 0) continue;
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(item);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{trueAmount}");
                        ImGui.TableNextColumn();
                    }
                }

                ImGui.EndTable();
                ImGui.Separator();
                ImGui.Separator();

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

                if (HasChangedSinceSave && SaveCooldown < 30)
                {
                    ImGui.TextColored(Red, "HAS NOT SAVED! DO NOT CLOSE");
                }
                else
                {
                    ImGui.TextColored(Green, "Safe to close");
                }

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

        foreach (var item in Session.Items.AllItemsReceived)
        {
            HeldItems[item.ItemName]++;
        }

        PlayerNames = Session.Players.AllPlayers.Select(player => $"{player.Name}").ToArray();
        
        MaxLocations = (long) SlotData["death_check_amount"];
        LocationsChecked = Session.Locations.AllLocationsChecked.Count;

        HasDeathButton = (bool) SlotData["has_funny_button"];
        
        var deathCount = Session.DataStorage[Scope.Slot, "Deaths"];
        if (deathCount != "")
        {
            Deaths = JsonConvert.DeserializeObject<Dictionary<string, int>>(deathCount)!;
        }

        var used = Session.DataStorage[Scope.Slot, "Used"].To<string>();
        if (used == "") return;
        var usedSplit = used.Split('|').Select(int.Parse).ToArray();
        UsedItems["Death Trap"] = usedSplit[0];
        UsedItems["Death Shield"] = usedSplit[1];
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
        HasChangedSinceSave = true;
    }

    public void CheckChecks()
    {
        if (LocationsChecked >= MaxLocations) return;
        if (Deaths.Values.Sum() < LocationsChecked + 1) return;
        Session.Locations.CompleteLocationChecks(UUID + LocationsChecked);
        LocationsChecked++;
        if (LocationsChecked < MaxLocations) return;
        StatusUpdatePacket status = new()
        {
            Status = ArchipelagoClientState.ClientGoal
        };
        Session.Socket.SendPacket(status);
    }
}