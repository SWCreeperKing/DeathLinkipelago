using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.Commands;
using static DeathLinkipelago.Scripts.MainController;

namespace DeathLinkipelago.Scripts.Commands;

public class BuyCommand : IApCommandInterface
{
    public string Command { get; set; } = "buy";
    public int MinArgumentLength { get; set; } = 1;
    
    public void RunCommand(ApClient client, ChatPrintJsonPacket message, string[] args)
    {
        if (!int.TryParse(args[0], out var itemNumber) || !client.MissingLocations.ContainsKey(itemNumber))
        {
            client.Say($"Item number [{itemNumber}] doesn't exist or is already bought");
            return;
        }

        if (GetItemAmount("Death Coin") <= 0)
        {
            client.Say("Not enough Death Coins to buy, but I will prioritize for later");
            PrioritizedItems.Add(itemNumber);
            HasChangedSinceLastSave = RefreshUI = true;
            return;
        }
        
        BuyItem(itemNumber);
    }
}