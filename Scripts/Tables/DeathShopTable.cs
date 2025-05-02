using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using Godot;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using static DeathLinkipelago.Scripts.MainController;

namespace DeathLinkipelago.Scripts.Tables;

public partial class DeathShopTable : TextTable
{
    public static readonly string Beige = Colors.Beige.ToHtml();
    public static readonly string Blue = Colors.DeepSkyBlue.ToHtml();
    public static readonly string Red = Colors.Red.ToHtml();
    public static readonly string White = Colors.White.ToHtml();
    public static readonly string Purple = Colors.MediumPurple.ToHtml();
    public static readonly string Gold = Colors.Gold.ToHtml();
    public static readonly string Green = Colors.Green.ToHtml();

    [Export] private MainController _Main;
    private string _CannotAffordText = $"[color={Red}]Cannot Afford[/color]";

    public override void _Ready()
    {
        MetaClicked += s =>
        {
            if (Client is null || !Client.IsConnected) return;
            Client.SendLocation(int.Parse((string)s));
            InventoryUsed["Death Coin"]++;
            RefreshUI = true;
            if (Client.MissingLocations.Count != 0) return;
            Client.Goal();
        };
    }

    public void Update(Dictionary<long, ScoutedItemInfo> locations)
    {
        UpdateData(locations
                  .Where(kv => kv.Key < ShopLevel * 10)
                  .OrderByDescending(kv => HintedItems.Contains(kv.Key))
                  .Select(kv =>
                   {
                       var itemName = $"{kv.Value.ItemName}".Replace(" Trap", " Sheild")
                                                            .Replace(" Shield", " Tarp");
                       return new[]
                       {
                           _Main["Death Coin"] > 0 ? $"   [color={Green}][url={kv.Key}]Buy[/url][/color]       " : _CannotAffordText,
                           $"[color={(HintedItems.Contains(kv.Key) ? Blue : White)}]Item {kv.Key + 1}[/color]",
                           $"[color={GetItemColor(kv.Value)}]{itemName}[/color]",
                           kv.Value.Player.Slot == Client.PlayerSlot
                               ? $"[color={Purple}]{Client.PlayerName}[/color]"
                               : kv.Value.Player.Name,
                           kv.Value.ItemGame,
                       };
                   })
                  .ToList());
    }

    public string GetItemColor(ScoutedItemInfo item)
    {
        if (item.ItemName.Contains(" Shield")) return Red;
        if (item.ItemName.Contains(" Trap")) return Blue;
        if (item.Flags.HasFlag(Advancement)) return Gold;
        if (item.Flags.HasFlag(Trap) || item.Flags.HasFlag(NeverExclude)) return Blue;
        return Beige;
    }
}