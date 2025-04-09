using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDeathCounter.LoggerConsole;
using Backbone;
using ImGuiNET;
using static UserInterface;

namespace ArchipelagoDeathCounter;

public class TextClient : Messenger<ChatPrintJsonPacket>
{
    protected override void OnSentMessage(string message)
    {
        Client.Connection.Session.Say(message);
    }

    protected override void RenderMessage(ChatPrintJsonPacket message)
    {
        Client.Connection.PrintPlayerName(message.Slot);
        ImGui.SameLine(0, 0);
        ImGui.Text($": {message.Message}");
    }
}