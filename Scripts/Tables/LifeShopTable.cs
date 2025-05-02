using System.Linq;
using Godot;
using static DeathLinkipelago.Scripts.MainController;
using static DeathLinkipelago.Scripts.Tables.DeathShopTable;

namespace DeathLinkipelago.Scripts.Tables;

public partial class LifeShopTable : TextTable
{
    public string[] Items = ["Death Trap", "Death Shield", "Death Coin"];
    [Export] private MainController _Main;
    private string _CannotAffordText = $"[color={Red}]Cannot Afford[/color]";

    public override void _Ready()
    {
        MetaClicked += s =>
        {
            var item = (string)s;
            InventoryUsed["Life Coin"]++;
            Inventory[item]++;
            LifeShopBought[item]++;
            RefreshUI = true;
        };
    }

    public void Update()
    {
        UpdateData(Items.Where(s => s is not "Death Trap" || MainController.Config.HasFunnyButton)
                        .Select(str => new[]
                         {
                             _Main["Life Coin"] > 0
                                 ? $"   [color={Green}][url=\"{str}\"]Buy[/url][/color]       "
                                 : _CannotAffordText,
                             str
                         })
                        .ToList());
    }
}