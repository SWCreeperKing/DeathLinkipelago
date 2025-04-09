using Archipelago.MultiClient.Net.Models;
using ImGuiNET;
using static UserInterface;

namespace ArchipelagoDeathCounter;

public class SenderLog : Messenger<JsonMessagePart[]>
{
    protected override void OnSentMessage(string message) { }

    protected override void RenderMessage(JsonMessagePart[] message)
    {
        var i = 0;
        if (message.Length == 8)
        {
            Client.Connection.PrintPlayerName(int.Parse(message[i++].Text));
            ImGui.SameLine(0, 0);
            ImGui.Text(message[i++].Text);
            ImGui.SameLine(0, 0);
            var itemName = Client.ItemIdToItemName(long.Parse(message[i].Text), message[i].Player!.Value);
            ImGui.TextColored(Client.GetItemColor(message[i++].Flags!.Value), itemName);
            ImGui.SameLine(0, 0);
            ImGui.Text(message[i++].Text);
            ImGui.SameLine(0, 0);
            Client.Connection.PrintPlayerName(int.Parse(message[i++].Text));
        }
        else
        {
            Client.Connection.PrintPlayerName(int.Parse(message[i++].Text));
            ImGui.SameLine(0, 0);
            ImGui.Text(message[i++].Text);
            ImGui.SameLine(0, 0);
            var itemName = Client.ItemIdToItemName(long.Parse(message[i].Text), message[i].Player!.Value);
            ImGui.TextColored(Client.GetItemColor(message[i++].Flags!.Value), itemName);
        }

        ImGui.SameLine(0, 0);
        ImGui.Text(message[i++].Text);
        ImGui.SameLine(0, 0);
        ImGui.TextColored(Client.Green,
            Client.LocationIdToLocationName(long.Parse(message[i].Text), message[i++].Player!.Value));
        ImGui.SameLine(0, 0);
        ImGui.Text(message[i++].Text);
    }
}