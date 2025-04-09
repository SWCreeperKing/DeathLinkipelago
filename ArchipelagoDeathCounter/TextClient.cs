using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDeathCounter.LoggerConsole;
using Backbone;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static UserInterface;

namespace ArchipelagoDeathCounter;

public class TextClient : Messenger<MessagePacket>
{
    protected override void OnSentMessage(string message) => Client.Connection.Session.Say(message);
    protected override void RenderMessage(MessagePacket message) => message.RenderMessage();
}

public abstract class MessagePacket
{
    public abstract void RenderMessage();
}

public class ChatMessagePacket(ChatPrintJsonPacket packet) : MessagePacket
{
    public readonly ChatPrintJsonPacket Packet = packet;

    public override void RenderMessage()
    {
        Client.Connection.PrintPlayerName(Packet.Slot);
        ImGui.SameLine(0, 0);
        ImGui.Text($": {Packet.Message}");
    }
}

public class ServerMessagePacket(string message) : MessagePacket
{
    public readonly string Message = $": {message}";

    public override void RenderMessage()
    {
        ImGui.TextColored(Client.Gold, "Server");
        ImGui.SameLine(0, 0);
        ImGui.Text(Message);
    }
}

public class HintMessagePacket(JsonMessagePart[] messageParts) : MessagePacket
{
    public readonly JsonMessagePart[] MessageParts = messageParts;

    public override void RenderMessage()
    {
        var i = 0;
        ImGui.TextColored(Client.Red, MessageParts[i++].Text);
        ImGui.SameLine(0, 0);
        Client.Connection.PrintPlayerName(int.Parse(MessageParts[i++].Text));
        ImGui.SameLine(0, 0);
        ImGui.Text(MessageParts[i++].Text);
        ImGui.SameLine(0, 0);
        var itemName = Client.ItemIdToItemName(long.Parse(MessageParts[i].Text), MessageParts[i].Player!.Value);
        ImGui.TextColored(Client.GetItemColor(MessageParts[i++].Flags!.Value), itemName);
        ImGui.SameLine(0, 0);
        ImGui.Text(MessageParts[i++].Text);
        ImGui.SameLine(0, 0);
        ImGui.TextColored(Client.Green,
            Client.LocationIdToLocationName(long.Parse(MessageParts[i].Text), MessageParts[i++].Player!.Value));
        ImGui.SameLine(0, 0);
        ImGui.Text(MessageParts[i++].Text);
        ImGui.SameLine(0, 0);
        Client.Connection.PrintPlayerName(int.Parse(MessageParts[i++].Text));
        ImGui.SameLine(0, 0);
        ImGui.Text(MessageParts[i++].Text);
        ImGui.SameLine(0, 0);
        ImGui.Text(MessageParts[i++].Text);
        ImGui.SameLine(0, 0);
        Client.PrintHintStatus(MessageParts[i].HintStatus!.Value);
    }
}