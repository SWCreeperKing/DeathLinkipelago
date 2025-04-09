using ImGuiNET;
using static ImGuiNET.ImGuiInputTextFlags;

namespace ArchipelagoDeathCounter;

public abstract class Messenger<TMessageType>
{
    public static int MaxScrollback = 200;
    
    public bool ScrollToBottom = true;
    private Queue<TMessageType> Scrollback = [];
    private string Input = "";
    private bool ToScroll;

    public void Render()
    {
        var wSize = ImGui.GetContentRegionAvail();
        ImGui.BeginChild("messenger", wSize with { Y = wSize.Y - 50 }, ImGuiChildFlags.Borders);
        {
            foreach (var message in Scrollback)
            {
                RenderMessage(message);
            }

            if (ScrollToBottom && ToScroll)
            {
                ImGui.SetScrollHereY();
                ToScroll = false;
            }
            else if (ToScroll)
            {
                ToScroll = false;
            }
        }

        ImGui.EndChild();

        if (!ImGui.InputText("Command", ref Input, 999, EnterReturnsTrue)) return;
        OnSentMessage(Input);
        UpdateScrollBack();
        Input = "";
    }

    public void SendMessage(TMessageType message)
    {
        Scrollback.Enqueue(message);
        UpdateScrollBack();
    }

    public void UpdateScrollBack()
    {
        while (Scrollback.Count > MaxScrollback) Scrollback.Dequeue();
        if (ScrollToBottom) ToScroll = true;
    }

    protected abstract void OnSentMessage(string message);
    protected abstract void RenderMessage(TMessageType message);
}