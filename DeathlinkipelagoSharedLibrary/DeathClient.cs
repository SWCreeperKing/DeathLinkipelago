using Archipelago.MultiClient.Net.Models;
using CreepyUtil.Archipelago;
using static Archipelago.MultiClient.Net.Enums.HintStatus;

namespace DeathlinkipelagoSharedLibrary;

public class DeathClient : ApClient
{
    public void Process(double delta)
    {
        refreshUI = false;
        newHinted = [];
        newItems = [];
        UpdateConnection();
        if (Session?.Socket is null || IsConnected) return;
        
        if (PushUpdatedVariables(false, out var hints))
        {
            newHinted = hints
                         .Where(hint => hint.FindingPlayer == PlayerSlot && hint.Status == Priority)
                         .Select(hint => hint.LocationId - UUID)
                         .ToArray();
            refreshUI = true;
        }

        newItems = GetOutstandingItems().ToArray()!;
    }

    public void FunnyButtonPress()
    {
        if (IsConnected) return;
        SendDeathLink(FunnyButtonMessages[Random.Next(FunnyButtonMessages.Length)]);
    }
}