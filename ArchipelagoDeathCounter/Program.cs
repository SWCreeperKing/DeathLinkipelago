using System.Numerics;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDeathCounter;
using ArchipelagoDeathCounter.LoggerConsole;
using ArchipelagoDeathCounter.LoggerConsole.Commands;
using Backbone;
using ImGuiNET;
using Raylib_cs;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using static Backbone.Backbone;
using Color = Raylib_cs.Color;

UserInterface ui = new();
ui.Loop();

class UserInterface() : Backbone.Backbone("DeathLinkipelago", 650, 700)
{
    public int Counter = 0;
    public static ArchipelagoClient Client = new();
    public static TextClient MessageClient = new();
    public long LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public override void Init()
    {
        Logger.Initialize();
        Logger.Log("init");
        CommandRegister.RegisterCommandFile(typeof(DefaultCommands));
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
        if (!ImGui.BeginTabBar("tabs")) return;
        if (ImGui.BeginTabItem("Deathlinkipelago"))
        {
            Client.Render();
            ImGui.EndTabItem();
        }

        if (Client.Connection.Connected != -1)
        {
            if (ImGui.BeginTabItem("Text Client"))
            {
                MessageClient.Render();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Hints"))
            {
                Client.RenderHintsTable();
                ImGui.EndTabItem();
            }
        }

        if (ImGui.BeginTabItem("Console"))
        {
            Logger.GameConsole.Render();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}

public class ArchipelagoClient
{
    public const long UUID = 0x0AF5F0AC;

    public APConnection Connection = new();
    public Vector4 White = Color.White.ToV4();
    public Vector4 Red = Color.Red.ToV4();
    public Vector4 Green = Color.Green.ToV4();
    public Vector4 Gold = Color.Gold.ToV4();
    public Vector4 Blue = Color.Blue.ToV4();
    public Vector4 SkyBlue = Color.SkyBlue.ToV4();
    public Vector4 Purple = Color.Purple.ToV4();
    public string[]? Error;
    public string[] PlayerNames = [];
    public string[] PlayerGames = [];
    public string LastPersonToBlame = "";
    public float SaveCooldown = 1.5f;
    public float DeathCooldown = 30;
    public float TimeSinceLastDeath;
    public float LongestTimeSinceLastDeath;
    public bool HasChangedSinceSave;
    public bool HasDeathButton;
    public bool SendTrapsAfterGoal;
    public Hint[] Hints = [];
    public Dictionary<long, string> ItemIdToName = [];
    public Dictionary<string, int> Deaths = [];

    public void Init() { Connection.LoadInitData(); }

    public void Update(float deltaTime)
    {
        if (Connection.Connected == -1) return;

        Connection.Update();

        if (LastPersonToBlame != "")
        {
            TimeSinceLastDeath += deltaTime;
        }

        if (DeathCooldown < 1.5)
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
                ImGui.Text(
                    $"Last recorded death link was from: [{(LastPersonToBlame == "" ? "N/A" : LastPersonToBlame)}]");

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
            Error = Connection.TryConnect();
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
        var canAfford = Connection.GetItemAmount("Death Coin") > 0;
        ImGui.Text($"Shop Level: [{Connection.HeldItems["Progressive Death Shop"]}]");
        if (ImGui.BeginTable("Shop Table", 4, TableFlags | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Loc");
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("For");
            ImGui.TableSetupColumn("Buy");
            ImGui.TableHeadersRow();
            var hintedShopItems = Hints.Where(hint => hint.FindingPlayer == Connection.PlayerSlot)
                                       .Select(hint => hint.LocationId - UUID)
                                       .ToArray();
            foreach (var (id, location) in Connection.Locations.OrderByDescending(kv => hintedShopItems.Contains(kv.Key)))
            {
                if (id >= (Connection.HeldItems["Progressive Death Shop"] + 1) * 10) break;
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextColored(hintedShopItems.Contains(id) ? Blue : White, $"Item {id + 1} ");
                ImGui.TableNextColumn();
                ImGui.TextColored(GetItemColor(location),
                    $"{location.ItemName} ".Replace(" Trap", " Sheild").Replace(" Shield", " Tarp"));
                ImGui.TableNextColumn();
                ImGui.Text($"{location.Player.Name} ");
                ImGui.TableNextColumn();

                if (canAfford)
                {
                    if (ImGui.Button($"Buy Item {id + 1}"))
                    {
                        Connection.BuyCheck(id, this);
                    }
                }
                else
                {
                    ImGui.TextColored(Red, "Cannot Afford");
                }

                ImGui.TableNextColumn();
            }
        }

        ImGui.EndTable();
    }

    public void RenderHintsTable()
    {
        if (ImGui.BeginTable("Hint Table", 4, TableFlags | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Receiving Player");
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("Finding Player");
            ImGui.TableSetupColumn("Priority");
            ImGui.TableHeadersRow();
            foreach (var hint in Hints)
            {
                if (!ItemIdToName.TryGetValue(hint.ItemId, out var itemName))
                {
                    itemName = ItemIdToName[hint.ItemId] =
                        Connection.Session.Items.GetItemName(hint.ItemId, PlayerGames[hint.ReceivingPlayer]);
                }

                if (hint.Found) continue;
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                Connection.PrintPlayerName(hint.ReceivingPlayer);
                ImGui.TableNextColumn();
                ImGui.TextColored(GetItemColor(hint.ItemFlags), $"{itemName} ");
                ImGui.TableNextColumn();
                Connection.PrintPlayerName(hint.FindingPlayer);
                ImGui.TableNextColumn();
                PrintHintStatus(hint.Status);
                ImGui.TableNextColumn();
            }
        }

        ImGui.EndTable();
    }

    public void AddDeath(string slot, int amount = 1)
    {
        if (!PlayerNames.Contains(slot))
        {
            slot = PlayerNames.FirstOrDefault(name => slot.EndsWith($" ({name})"), slot);
        }

        LastPersonToBlame = slot;
        Deaths.TryAdd(slot, 0);
        Deaths[slot] += amount;
        Connection.HeldItems["Death Coin"] += amount;
        OrderDeaths();
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

    public Vector4 GetItemColor(ItemFlags item)
    {
        if (item.HasFlag(Advancement)) return Gold;
        if (item.HasFlag(Trap)) return Red;
        return item.HasFlag(NeverExclude) ? Blue : SkyBlue;
    }

    public Vector4 GetItemColor(ScoutedItemInfo item)
    {
        if (item.ItemName.Contains(" Shield")) return Red;
        if (item.Flags.HasFlag(Advancement)) return Gold;
        if (item.Flags.HasFlag(Trap) || item.Flags.HasFlag(NeverExclude)) return Blue;
        return SkyBlue;
    }


    public void PrintHintStatus(HintStatus status)
    {
        switch (status)
        {
            case HintStatus.Found:
                ImGui.TextColored(Green, "Found");
                break;
            case HintStatus.Unspecified:
                ImGui.TextColored(White, "Unspecified");
                break;
            case HintStatus.NoPriority:
                ImGui.TextColored(SkyBlue, "No Priority");
                break;
            case HintStatus.Avoid:
                ImGui.TextColored(Red, "Avoid");
                break;
            case HintStatus.Priority:
                ImGui.TextColored(Purple, "Priority");
                break;
        }
    }

    public void OrderDeaths() => Deaths = Deaths.OrderBy(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
}