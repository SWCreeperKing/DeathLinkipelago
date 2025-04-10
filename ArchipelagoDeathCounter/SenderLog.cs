using Archipelago.MultiClient.Net.Models;
using ImGuiNET;
using static ArchipelagoClient;
using static UserInterface;

namespace ArchipelagoDeathCounter;

public class SenderLog : Messenger<MessagePart[]>
{
    protected override void OnSentMessage(string message) { }

    protected override void RenderMessage(MessagePart[] message)
    {
        for (var i = 0; i < message.Length; i++)
        {
            message[i].Render(i < message.Length - 1);
        }
    }
}