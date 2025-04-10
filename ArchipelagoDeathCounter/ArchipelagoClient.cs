using Archipelago.MultiClient.Net.Enums;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.UIBackbone;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using Raylib_cs;

namespace ArchipelagoDeathCounter;

public class ArchipelagoClient(LoginInfo info, long gameUUID) : ApUIClient(info, gameUUID)
{
    public readonly Dictionary<ArchipelagoClientState, string> PlayerStatusEnumName =
        Enum.GetValues<ArchipelagoClientState>()
            .ToDictionary(state => state, state => Enum.GetName(state)!.Replace("Client", ""));

    public string[]? Error;
    public string LastPersonToBlame = "";
    public float SaveCooldown = 1.5f;
    public float DeathCooldown = 30;
    public double TimeSinceLastDeath;
    public double LongestTimeSinceLastDeath;
    public bool HasChangedSinceSave;
    public bool HasDeathButton;
    public bool SendTrapsAfterGoal;
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

    public int GetItemAmount(string item) => HeldItems[item] - UsedItems[item];

    public void Update(float deltaTime)
    {
        if (!IsConnected) return;

        PushUpdatedVariables();

        foreach (var item in GetOutstandingItems())
        {
            HeldItems[item!.ItemName]++;
        }

        if (LastPersonToBlame != "")
        {
            TimeSinceLastDeath += deltaTime;
        }

        if (DeathCooldown < 1.5)
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
                if (MissingLocations.Count != 0 || SendTrapsAfterGoal)
                {
                    SendDeathLink("The Will of God");
                }
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
        this["Used"] = $"{UsedItems["Death Trap"]}|{UsedItems["Death Shield"]}|{UsedItems["Death Coin"]}";
        this["Deaths"] = JObject.FromObject(Deaths);
        SaveCooldown = 0;
    }

    public void Render()
    {
        if (HasDeathButton && ImGui.Button("Send Death"))
        {
            SendDeathLink("Stupidity");
        }

        if (HasChangedSinceSave && SaveCooldown < 30)
        {
            ImGui.TextColored(Red, "HAS NOT SAVED! DO NOT CLOSE");
        }
        else
        {
            ImGui.TextColored(Green, "Safe to close");
        }

        if (MissingLocations.Count != 0)
        {
            ImGui.Text($"Victory: {MissingLocations.Count} Remaining");
        }
        else
        {
            ImGui.TextColored(Green, "Victory!");
        }

        ImGui.NewLine();
        RenderItemTable();
        ImGui.NewLine();
        RenderDeathTable();

        ImGui.Text($"Time since last death: [{TimeSinceLastDeath.GetAsTime()}]");
        ImGui.Text(
            $"Longest Time since last death: [{Math.Max(LongestTimeSinceLastDeath, TimeSinceLastDeath).GetAsTime()}]");
        ImGui.Text(
            $"Last recorded death link was from: [{(LastPersonToBlame == "" ? "N/A" : LastPersonToBlame)}]");

        ImGui.NewLine();
        RenderShopTable();
    }

    public void RenderItemTable()
    {
        if (ImGui.BeginTable("Item Table", 4, Backbone.Backbone.TableFlags))
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
            }
        }

        ImGui.EndTable();
    }

    public void RenderDeathTable()
    {
        if (ImGui.BeginTable("Death Table", 2, Backbone.Backbone.TableFlags))
        {
            ImGui.TableSetupColumn("To Blame");
            ImGui.TableSetupColumn("Amount");
            ImGui.TableHeadersRow();
            foreach (var (blame, amount) in Deaths)
            {
                if (amount == 0) continue;
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(blame);
                ImGui.TableNextColumn();
                ImGui.Text($"{amount}");
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
        var canAfford = GetItemAmount("Death Coin") > 0;
        ImGui.Text($"Shop Level: [{HeldItems["Progressive Death Shop"]}]");
        if (ImGui.BeginTable("Shop Table", 4,
                Backbone.Backbone.TableFlags | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Loc");
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("For");
            ImGui.TableSetupColumn("Buy");
            ImGui.TableHeadersRow();
            var hintedShopItems = Hints.Where(hint => hint.FindingPlayer == PlayerSlot)
                                       .Select(hint => hint.LocationId - UUID)
                                       .ToArray();
            foreach (var (id, location) in MissingLocations.OrderByDescending(
                         kv => hintedShopItems.Contains(kv.Key)))
            {
                if (id >= (HeldItems["Progressive Death Shop"] + 1) * 10)
                {
                    if (hintedShopItems.Contains(id)) continue;
                    break;
                }

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextColored(hintedShopItems.Contains(id) ? Blue : White, $"Item {id + 1} ");
                ImGui.TableNextColumn();
                ImGui.TextColored(GetItemColor(location),
                    $"{location.ItemName} ".Replace(" Trap", " Sheild").Replace(" Shield", " Tarp"));
                ImGui.TableNextColumn();
                PrintPlayerName(location.Player.Slot);
                ImGui.TableNextColumn();

                if (canAfford)
                {
                    if (ImGui.Button($"Buy Item {id + 1}"))
                    {
                        BuyCheck(id, this);
                    }
                }
                else
                {
                    ImGui.TextColored(Red, "Cannot Afford");
                }
            }
        }

        ImGui.EndTable();
    }

    public void RenderHintsTable()
    {
        if (ImGui.BeginTable("Hint Table", 7, Backbone.Backbone.TableFlags | ImGuiTableFlags.ScrollX))
        {
            ImGui.TableSetupColumn("Copy");
            ImGui.TableSetupColumn("Receiving Player");
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("Finding Player");
            ImGui.TableSetupColumn("Priority");
            ImGui.TableSetupColumn("Location");
            ImGui.TableSetupColumn("Entrance");
            ImGui.TableHeadersRow();
            var i = 0;
            try
            {
                foreach (var hint in Hints)
                {
                    if (hint.Found) continue;
                    var itemName = ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer);
                    var location = LocationIdToLocationName(hint.LocationId, hint.FindingPlayer);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.PushID($"copy: {i++}");
                    if (ImGui.Button("Copy"))
                    {
                        var entrance = hint.Entrance.Trim() == "" ? "Vanilla Entrance" : hint.Entrance;
                        Raylib.SetClipboardText(
                            $"`{PlayerNames[hint.ReceivingPlayer]}`'s __{itemName}__ is in `{PlayerNames[hint.FindingPlayer]}`'s world at **{location}**\n-# {entrance}");
                    }

                    ImGui.PopID();
                    ImGui.TableNextColumn();
                    PrintPlayerName(hint.ReceivingPlayer);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(GetItemColor(hint.ItemFlags), $"{itemName} ");
                    ImGui.TableNextColumn();
                    PrintPlayerName(hint.FindingPlayer);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(HintStatusColor[hint.Status], HintStatusText[hint.Status]);
                    ImGui.TableNextColumn();
                    ImGui.TextColored(Green, location);
                    ImGui.TableNextColumn();
                    if (hint.Entrance.Trim() == "")
                    {
                        ImGui.TextColored(White, "Vanilla");
                    }
                    else
                    {
                        ImGui.TextColored(ChillBlue, hint.Entrance);
                    }
                }
            }
            catch (Exception)
            {
                //ignored
            }
        }

        ImGui.EndTable();
    }

    public void RenderPlayerTable()
    {
        if (ImGui.BeginTable("Player List", 4, Backbone.Backbone.TableFlags | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Slot");
            ImGui.TableSetupColumn("Player");
            ImGui.TableSetupColumn("Game");
            ImGui.TableSetupColumn("Status");
            ImGui.TableHeadersRow();
            foreach (var player in Session.Players.AllPlayers)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{player.Slot}");
                ImGui.TableNextColumn();
                PrintPlayerName(player.Slot);
                ImGui.TableNextColumn();
                ImGui.TextColored(SkyBlue, player.Game);
                ImGui.TableNextColumn();
                ImGui.Text(PlayerStatusEnumName[PlayerStates[player.Slot]]);
            }
        }

        ImGui.EndTable();
    }

    public void BuyCheck(long id, ArchipelagoClient apClient)
    {
        if (GetItemAmount("Death Coin") < 1) return;

        SendLocation(id);
        UsedItems["Death Coin"]++;

        apClient.HasChangedSinceSave = true;
        if (MissingLocations.Count != 0) return;
        Session.SetGoalAchieved();
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
        HeldItems["Death Coin"] += amount;
        OrderDeaths();
        LongestTimeSinceLastDeath = Math.Max(LongestTimeSinceLastDeath, TimeSinceLastDeath);
        TimeSinceLastDeath = 0;
        HasChangedSinceSave = true;
    }

    public void OrderDeaths() => Deaths = Deaths.OrderBy(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
}