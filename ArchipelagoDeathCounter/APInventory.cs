namespace ArchipelagoDeathCounter;

public class APInventory
{
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
}