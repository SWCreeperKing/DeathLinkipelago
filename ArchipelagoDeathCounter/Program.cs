﻿using System.Numerics;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Backbone;
using ImGuiNET;
using Newtonsoft.Json;
using Raylib_cs;
using static Backbone.Backbone;
using Color = Raylib_cs.Color;

UserInterface ui = new();
ui.Loop();

class UserInterface() : Backbone.Backbone("Death Counter", 500, 550)
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
    public PlayerInfo[] PlayerInfos;
    public bool HasChangedSinceSave;
    public float SaveCooldown = 10;
    public float DeathCooldown = 30;
    public long MaxLocations;
    public long LocationsChecked;

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
        if (DeathCooldown < 10)
        {
            DeathCooldown += deltaTime;
        }
        else if (HeldItems["Death Trap"] > 0)
        {
            if (HeldItems["Death Shield"] - UsedItems["Death Shield"] > 0)
            {
                UsedItems["Death Shield"]++;
                Console.WriteLine("death link blocked");
            }
            else
            {
                DeathLink.SendDeathLink(new(Slot, "The Will of God"));
                Console.WriteLine("sent death link");
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
        Session.DataStorage["Used"] = $"{UsedItems["Death Trap"]}|{UsedItems["Death Shield"]}";
        Session.DataStorage["Deaths"] = JsonConvert.SerializeObject(Deaths);
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
                if (ImGui.Button("Send Death"))
                {
                    DeathLink.SendDeathLink(new(Slot, "Stupidity"));
                }
                
                ImGui.Text($"Next Check: Death #[{LocationsChecked + 1}] | Total req: {MaxLocations}");
                
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

                var result = Session.TryConnectAndLogin(Game, Slot, ItemsHandlingFlags.AllItems, tags: ["DeathLink"],
                    password: Password);
                DeathLink.OnDeathLinkReceived += OnDeathLink;

                if (!result.Successful)
                {
                    Error = ((LoginFailure)result).Errors;
                    return;
                }

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
        PlayerInfos = Session.Players.AllPlayers.ToArray();
        Session.Items.ItemReceived += OnItemReceived;

        foreach (var item in Session.Items.AllItemsReceived)
        {
            HeldItems[item.ItemName]++;
        }

        MaxLocations = Session.Locations.AllLocations.Count;
        LocationsChecked = Session.Locations.AllLocationsChecked.Count;

        var deathCount = Session.DataStorage["Deaths"];
        if (deathCount != "")
        {
            Deaths = JsonConvert.DeserializeObject<Dictionary<string, int>>(deathCount)!;
        }
        
        var used = Session.DataStorage["Used"].To<string>();
        if (used == "") return;
        var usedSplit = used.Split('|').Select(int.Parse).ToArray();
        UsedItems["Death Trap"] = usedSplit[0];
        UsedItems["Death Shield"] = usedSplit[1];
    }

    public void OnDeathLink(DeathLink deathLink)
    {
        var toBlame = deathLink.Source;
        if (PlayerInfos.First(player => player.Name == toBlame).Game == Game) return;
        Deaths.TryAdd(toBlame, 0);
        Deaths[toBlame]++;
        HasChangedSinceSave = true;
        
        if (LocationsChecked >= MaxLocations) return;
        if (Deaths.Values.Sum() < LocationsChecked + 1) return;
        Session.Locations.CompleteLocationChecks(UUID + LocationsChecked);
        LocationsChecked++;
    }

    public void OnItemReceived(ReceivedItemsHelper helper)
    {
        var item = helper.PeekItem();
        HeldItems[item.ItemName]++;
    }
}