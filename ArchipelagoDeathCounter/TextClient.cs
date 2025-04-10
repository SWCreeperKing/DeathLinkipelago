using System.Numerics;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ImGuiNET;
using static ArchipelagoClient;
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
        ImGui.TextColored(Gold, "Server");
        ImGui.SameLine(0, 0);
        ImGui.Text(Message);
    }
}

public class HintMessagePacket(JsonMessagePart[] messageParts) : MessagePacket
{
    public readonly MessagePart[] MessageParts = messageParts.Select(mp => new MessagePart(mp)).ToArray();

    public override void RenderMessage()
    {
        for (var i = 0; i < MessageParts.Length; i++)
        {
            MessageParts[i].Render(i < MessageParts.Length - 1);
        }
    }
}

public readonly struct MessagePart
{
    public readonly Vector4 Color = White;
    public readonly string Text = "";

    public MessagePart(JsonMessagePart jsonMessagePart, Vector4? colorOverride = null)
    {
        switch (jsonMessagePart.Type)
        {
            case JsonMessagePartType.PlayerId:
                var slot = int.Parse(jsonMessagePart.Text);
                Color = slot == Client.Connection.PlayerSlot ? DarkPurple : slot == 0 ? Gold : DirtyWhite;
                Text = Client.PlayerNames[slot];
                break;
            case JsonMessagePartType.ItemId:
                Color = Client.GetItemColor(jsonMessagePart.Flags!.Value);
                Text = Client.ItemIdToItemName(long.Parse(jsonMessagePart.Text), jsonMessagePart.Player!.Value);
                break;
            case JsonMessagePartType.LocationId:
                Color = Green;
                Text = Client.LocationIdToLocationName(long.Parse(jsonMessagePart.Text), jsonMessagePart.Player!.Value);
                break;
            case JsonMessagePartType.EntranceName:
                var entranceName = jsonMessagePart.Text.Trim();
                Color = entranceName == "" ? White : ChillBlue;
                Text = entranceName == "" ? "Vanilla" : entranceName;
                break;
            case JsonMessagePartType.HintStatus:
                Color = Client.HintStatusColor[(HintStatus)jsonMessagePart.HintStatus!];
                Text = Client.HintStatusText[(HintStatus)jsonMessagePart.HintStatus!];
                break;
            default:
                Text = jsonMessagePart.Text ?? "";
                break;
        }

        if (colorOverride is null) return;
        Color = colorOverride.Value;
    }

    public void Render(bool appendSameLine = true)
    {
        ImGui.TextColored(Color, Text);
        if (!appendSameLine) return;
        ImGui.SameLine(0, 0);
    }
}