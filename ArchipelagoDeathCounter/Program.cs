using System.Numerics;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDeathCounter;
using Backbone;
using ImGuiNET;
using Raylib_cs;
using static Backbone.Backbone;
using Color = Raylib_cs.Color;

UserInterface ui = new();
ui.Loop();

class UserInterface() : Backbone.Backbone("DeathLinkipelago", 650, 700)
{
    public int Counter = 0;
    private ArchipelagoClient Client = new();
    private long LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public override void Init()
    {
        Raylib.SetTargetFPS(30);
        Client.Init();
    }

    public override void Update()
    {
        var nextUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var delta = nextUpdate - LastUpdate;
        Client.Update(delta / 1000f);
        LastUpdate = nextUpdate;
    }

    public override void Render()
    {
        Client.Render();
    }
}

public class ArchipelagoClient
{
    public const long UUID = 0x0AF5F0AC;

    public APConnection Connection = new();
    public Vector4 Red = Color.Red.ToV4();
    public Vector4 Green = Color.Green.ToV4();
    public Vector4 Gold = Color.Gold.ToV4();
    public Vector4 Blue = Color.Blue.ToV4();
    public Vector4 SkyBlue = Color.SkyBlue.ToV4();
    public string[]? Error;
    public string[] PlayerNames = [];
    public string LastPersonToBlame = "N/A";
    public float SaveCooldown = 3;
    public float DeathCooldown = 30;
    public float TimeSinceLastDeath;
    public float LongestTimeSinceLastDeath;
    public bool HasChangedSinceSave;
    public bool HasDeathButton;
    public bool SendTrapsAfterGoal;
    
    public Dictionary<string, int> Deaths = [];

    public void Init() { Connection.LoadInitData(); }

    public void Update(float deltaTime)
    {
        if (Connection.Connected == -1) return;

        TimeSinceLastDeath += deltaTime;

        if (DeathCooldown < 3)
        {
            DeathCooldown += deltaTime;
        }
        else if (Connection.GetItemAmount("Death Trap") > 0)
        {
            if (Connection.GetItemAmount("Death Shield") > 0)
            {
                Connection.UsedItems["Death Shield"]++;
            }
            else
            {
                if (Connection.Locations.Count != 0 || SendTrapsAfterGoal)
                {
                    Connection.SendDeathLink("The Will of God");
                }

                AddDeath(Connection.Slot);
            }

            Connection.UsedItems["Death Trap"]++;
            HasChangedSinceSave = true;
            DeathCooldown = 0;
        }

        if (SaveCooldown < 30)
        {
            SaveCooldown += deltaTime;
        }

        if (!HasChangedSinceSave || SaveCooldown < 30) return;
        HasChangedSinceSave = false;
        Connection.SaveData(Deaths);
        SaveCooldown = 0;
    }

    public void Render()
    {
        switch (Connection.Connected)
        {
            case -1:
                RenderLogin();
                break;
            default:
                if (HasDeathButton && ImGui.Button("Send Death"))
                {
                    Connection.SendDeathLink("Stupidity");
                }

                if (HasChangedSinceSave && SaveCooldown < 30)
                {
                    ImGui.TextColored(Red, "HAS NOT SAVED! DO NOT CLOSE");
                }
                else
                {
                    ImGui.TextColored(Green, "Safe to close");
                }

                if (ImGui.Button("Gain Coin"))
                {
                    Connection.HeldItems["Death Coin"]++;
                }

                if (Connection.Locations.Count != 0)
                {
                    ImGui.Text($"Victory: {Connection.Locations.Count} Remaining");
                }
                else
                {
                    ImGui.TextColored(Green, "Victory!");
                }

                ImGui.NewLine();
                RenderItemTable();
                ImGui.NewLine();
                RenderDeathTable();
                
                ImGui.Text($"Time since last death: [{GetTime(TimeSinceLastDeath)}]");
                ImGui.Text(
                    $"Longest Time since last death: [{GetTime(Math.Max(LongestTimeSinceLastDeath, TimeSinceLastDeath))}]");
                ImGui.Text($"Last recorded death link was from: [{LastPersonToBlame}]");
                
                ImGui.NewLine();
                RenderShopTable();

                break;
        }
    }

    public void RenderLogin()
    {
        ImGui.InputText("Address", ref Connection.Address, 255);
        ImGui.InputInt("Port", ref Connection.Port);
        ImGui.InputText("Password", ref Connection.Password, 255);
        ImGui.InputText("Slot", ref Connection.Slot, 255);
        if (ImGui.Button("Connect"))
        {
            Error = Connection.TryConnect(this);
        }

        if (Error is null) return;
        foreach (var line in Error)
        {
            ImGui.TextColored(Red, line);
        }
    }

    public void RenderItemTable()
    {
        if (ImGui.BeginTable("Item Table", 4, TableFlags))
        {
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("Available");
            ImGui.TableSetupColumn("Used");
            ImGui.TableSetupColumn("Total");
            ImGui.TableHeadersRow();
            foreach (var (item, amount) in Connection.HeldItems)
            {
                if (item == "Progressive Death Shop") continue;
                var trueAmount = amount - Connection.UsedItems[item];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(item);
                ImGui.TableNextColumn();
                ImGui.Text($"{trueAmount}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Connection.UsedItems[item]}");
                ImGui.TableNextColumn();
                ImGui.Text($"{Connection.HeldItems[item]}");
                ImGui.TableNextColumn();
            }
        }

        ImGui.EndTable();
    }

    public void RenderDeathTable()
    {
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
    }

    public void RenderShopTable()
    {
        if (ImGui.BeginTable("Shop Table", 4, TableFlags | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Loc");
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("For");
            ImGui.TableSetupColumn("Buy");
            ImGui.TableHeadersRow();
            foreach (var (id, location) in Connection.Locations)
            {
                if (id >= (Connection.HeldItems["Progressive Death Shop"] + 1) * 10) break;
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"Item {id + 1} ");
                ImGui.TableNextColumn();
                ImGui.TextColored(GetItemColor(location), $"{location.ItemName} ".Replace("Trap", "Sheild"));
                ImGui.TableNextColumn();
                ImGui.Text($"{location.Player.Name} ");
                ImGui.TableNextColumn();

                if (ImGui.Button($"Buy Item {id + 1}"))
                {
                    Connection.BuyCheck(id, this);
                }

                ImGui.TableNextColumn();
            }
        }

        ImGui.EndTable();
    }
    
    public void AddDeath(string slot)
    {
        if (!PlayerNames.Contains(slot))
        {
            slot = PlayerNames.FirstOrDefault(name => slot.EndsWith($" ({name})"), slot);
        }

        LastPersonToBlame = slot;
        Deaths.TryAdd(slot, 0);
        Deaths[slot]++;
        Connection.HeldItems["Death Coin"]++;
        LongestTimeSinceLastDeath = Math.Max(LongestTimeSinceLastDeath, TimeSinceLastDeath);
        TimeSinceLastDeath = 0;
        HasChangedSinceSave = true;
    }

    public string GetTime(double time)
    {
        var sec = time % 60;
        time = (float)Math.Floor(time / 60f);
        var min = time % 60;
        time = (float)Math.Floor(time / 60f);
        var hour = time % 24;
        var days = (float)Math.Floor(time / 24f);

        StringBuilder sb = new();
        if (days > 0) sb.Append(days).Append("d ");
        if (hour > 0) sb.Append(hour).Append("hr ");
        if (min > 0) sb.Append(min).Append("m ");
        if (sec > 0) sb.Append($"{sec:#0.00}").Append("s ");
        return sb.ToString().TrimEnd();
    }

    public Vector4 GetItemColor(ScoutedItemInfo item)
    {
        if (item.Flags.HasFlag(ItemFlags.Advancement)) return Gold;
        if (item.Flags.HasFlag(ItemFlags.Trap) || item.Flags.HasFlag(ItemFlags.NeverExclude)) return Blue;
        return SkyBlue;
    }
}