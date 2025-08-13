using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.Commands;
using static DeathLinkipelago.Scripts.MainController;

namespace DeathLinkipelago.Scripts.Commands;

public class InventoryCheckCommand : IApCommandInterface
{
    public string Command { get; set; } = "inv";
    public int MinArgumentLength { get; set; } = 1;
    
    public void RunCommand(ApClient client, ChatPrintJsonPacket message, string[] args)
    {
        if (!Settings.Data.GetBool("inv_command", true)) return;
        switch (args[0].ToLower())
        {
            case "shield":
                client.Say($"I have [{GetItemAmount("Death Shield")}] Death Shields");
                break;
            case "coin":
                client.Say($"I have [{GetItemAmount("Death Coin")}] Death Coins");
                break;
            default:
                client.Say($"Argument: [{args[0]}] is not recognized, try shield or coin instead");
                break;
        }
    }
}