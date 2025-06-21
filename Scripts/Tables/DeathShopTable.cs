using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
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
            var text = (string)s;

            if (text.StartsWith("true%"))
            {
                PrioritizedItems.Add(long.Parse(text[5..]));
                HasChangedSinceLastSave = true;
            }
            else if (text.StartsWith("false%"))
            {
                PrioritizedItems.Remove(long.Parse(text[6..]));
                HasChangedSinceLastSave = true;
            }
            else
            {
                BuyItem(int.Parse(text));
            }

            RefreshUI = true;
        };
    }

    public void Update(Dictionary<long, ScoutedItemInfo> rawLocations)
    {
        if (Client is null || !Client.IsConnected || !Client.Session.Socket.Connected) return;
        var locations = rawLocations
                       .Where(kv => kv.Key <= ShopLevel * 10)
                       .OrderByDescending(kv => PrioritizedItems.Contains(kv.Key));

        if (Settings.Data.GetBool("separated_hinted", true))
        {
            locations = locations.ThenByDescending(kv => HintedItems.Contains(kv.Key));
        }

        if (MainController.Config.SendScoutHints)
        {
            Client.Session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locations
                      .Where(loc
                           => loc.Value.Flags.HasFlag(Advancement))
                      .Select(kv => kv.Value.LocationId)
                      .ToArray())
                  .GetAwaiter()
                  .GetResult();
        }

        UpdateData(locations
                  .Select(kv =>
                   {
                       var itemName = $"{kv.Value.ItemName}";
                       if (Settings.Data.GetBool("hide_traps", true))
                       {
                           itemName = itemName.Replace(" Trap", " Sheild");
                       }

                       if (Settings.Data.GetBool("hide_shields", true))
                       {
                           itemName = itemName.Replace(" Shield", " Tarp");
                       }

                       return new[]
                       {
                           _Main["Death Coin"] > 0
                               ? $"   [color={Green}][url={kv.Key}]Buy[/url][/color]       "
                               : _CannotAffordText,
                           $"{GetIndexFx(kv.Key)}",
                           $"[color={GetItemColor(kv.Value)}]{itemName}[/color]",
                           kv.Value.Player.Slot == Client.PlayerSlot
                               ? $"[color={Purple}]{Client.PlayerName}[/color]"
                               : kv.Value.Player.Name,
                           kv.Value.ItemGame,
                           PrioritizedItems.Contains(kv.Key)
                               ? $"[url=false%{kv.Key}]Remove Priority[/url]"
                               : $"[url=true%{kv.Key}]Prioritize[/url]"
                       };
                   })
                  .ToList());
    }

    public string GetIndexFx(long id)
    {
        var isHinted = HintedItems.Contains(id);
        var isPriority = PrioritizedItems.Contains(id);
        var both = isHinted && isPriority;
        var color = both ? Gold : isPriority ? Green : isHinted ? Blue : White;
        var text = $"[color={color}]Item {id}[/color]";

        if (isPriority)
        {
            text = $"[wave rate=5]{text}[/wave]";
        }

        if (isHinted)
        {
            text = $"[shake]{text}[/shake]";
        }

        return text;
    }

    public string GetItemColor(ScoutedItemInfo item)
    {
        if (item.ItemName.Contains(" Shield") && Settings.Data.GetBool("hide_shields", true)) return Red;
        if (item.ItemName.Contains(" Trap") && Settings.Data.GetBool("hide_traps", true)) return Blue;
        if (item.Flags.HasFlag(Advancement)) return Gold;
        if (item.Flags.HasFlag(Trap) || item.Flags.HasFlag(NeverExclude)) return Blue;
        return Beige;
    }
}