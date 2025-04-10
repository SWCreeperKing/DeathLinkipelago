using System.Diagnostics;
using System.Reflection;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDeathCounter;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.UIBackbone.LoggerConsole;
using CreepyUtil.Archipelago.UIBackbone.LoggerConsole.Commands;
using ImGuiNET;
using Raylib_cs;
using static CreepyUtil.Archipelago.UIBackbone.ApUIClient;

UserInterface ui = new();
ui.Loop();

class UserInterface() : Backbone.Backbone("DeathLinkipelago", 650, 700)
{
    public static ArchipelagoClient Client = null;
    public long LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public string Address = "archipelago.gg";
    public string Password = "";
    public string Slot = "Slot";
    public int Port = 12345;
    public string[]? Error;

    public override void Init()
    {
        if (File.Exists("init.txt"))
        {
            var file = File.ReadAllText("init.txt").Replace("\r", "").Split('\n');
            Slot = file[0];
            Port = int.TryParse(file[1], out var port) ? port : Port;
            if (file.Length >= 3)
            {
                GameConsole.MaxScrollback = int.TryParse(file[2], out var amt) ? amt : 200;
            }
        }

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var stream =
                asm.GetManifestResourceStream("ArchipelagoDeathCounter.Resources.Icon.png");

            var byteArr = new byte[stream!.Length];
            stream.ReadExactly(byteArr, 0, (int)stream.Length);
            stream.Close();


            Raylib.SetWindowIcon(Raylib.LoadImageFromMemory(".png", byteArr));
        }
        catch
        {
            //ignored
        }

        Logger.Initialize();
        Logger.Log("init");
        CommandRegister.RegisterCommandFile(typeof(ProgramCommands));
        Raylib.SetTargetFPS(30);
    }

    public override void Update()
    {
        var nextUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var delta = nextUpdate - LastUpdate;
        if (Client is not null)
        {
            Client.Update(delta / 1000f);
        }

        LastUpdate = nextUpdate;
    }

    public override void Render()
    {
        if (!ImGui.BeginTabBar("tabs")) return;
        if (ImGui.BeginTabItem("Deathlinkipelago"))
        {
            if (Client is null)
            {
                ImGui.InputText("Address", ref Address, 255);
                ImGui.InputInt("Port", ref Port);
                ImGui.InputText("Password", ref Password, 255);
                ImGui.InputText("Slot", ref Slot, 255);

                if (ImGui.Button("Connect"))
                {
                    Client = new ArchipelagoClient(new LoginInfo(Port, Slot, Address, Password), 0x0AF5F0AC);
                    Client.OnConnectionEvent += (_, rawClient) =>
                    {
                        var client = (ArchipelagoClient)rawClient;
                        File.WriteAllLines("init.txt", [Slot, $"{Port}", $"{GameConsole.MaxScrollback}"]);
                        client.HasDeathButton = (bool)client.SlotData["has_funny_button"];

                        try
                        {
                            client.SendTrapsAfterGoal = (bool)client.SlotData["send_traps_after_goal"];
                        }
                        catch (Exception e)
                        {
                            client.SendTrapsAfterGoal = false;
                            Logger.Log(e);
                        }

                        try
                        {
                            var deathCount = client["Deaths"];
                            if (deathCount is not null && deathCount != "" && deathCount != "()")
                            {
                                client.Deaths = deathCount.To<Dictionary<string, int>>() ?? [];
                                client.HeldItems["Death Coin"] = client.Deaths.Values.Sum();
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log(e);
                            client.Deaths = [];
                        }

                        var used = client["Used"]!.To<string>();
                        if (used == "") return;
                        var usedSplit = used.Split('|').Select(int.Parse).ToArray();
                        client.UsedItems["Death Trap"] = usedSplit[0];
                        client.UsedItems["Death Shield"] = usedSplit[1];
                        client.UsedItems["Death Coin"] = usedSplit[2];
                    };

                    Error = Client.TryConnect("DeathLinkipelago",
                        ItemsHandlingFlags.RemoteItems | ItemsHandlingFlags.IncludeOwnItems, tags: ["DeathLink"]);

                    if (Error is not null)
                    {
                        Client = null!;
                    }
                }

                if (Error is not null)
                {
                    foreach (var line in Error)
                    {
                        ImGui.TextColored(Red, line);
                    }
                }
            }
            else
            {
                Client.Render();
            }

            ImGui.EndTabItem();
        }

        if (Client is not null && Client.IsConnected)
        {
            if (ImGui.BeginTabItem("Text Client"))
            {
                Client.TextClient.Render();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Hints"))
            {
                Client.RenderHintsTable();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Item Log"))
            {
                Client.ItemLog.Render();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Player List"))
            {
                Client.RenderPlayerTable();
                ImGui.EndTabItem();
            }
        }

        if (ImGui.BeginTabItem("Console"))
        {
            Logger.GameConsole.Render();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Credits"))
        {
            ImGui.Text("SW_CreeperKing");
            ImGui.Text("- Developer");
            if (ImGui.Button("Open Link Tree"))
            {
                Process.Start("explorer", "https://linktr.ee/swcreeperking");
            }

            ImGui.NewLine();
            ImGui.Text("Raphael2512");
            ImGui.Text("- Application Icon");
            ImGui.PushID("linktree2");
            if (ImGui.Button("Open Link Tree"))
            {
                Process.Start("explorer", "https://linktr.ee/raphael2512");
            }

            ImGui.PopID();

            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}