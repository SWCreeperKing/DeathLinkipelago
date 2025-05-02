using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CreepyUtil.Archipelago;
using Godot;
using static Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags;
using static DeathLinkipelago.Scripts.DeathTracker;
using static DeathLinkipelago.Scripts.MainController;

namespace DeathLinkipelago.Scripts;

public partial class Login : Control
{
    [Export] private MainController _Main;
    [Export] private LineEdit _AddressEdit;
    [Export] private LineEdit _PasswordEdit;
    [Export] private LineEdit _PortEdit;
    [Export] private LineEdit _SlotEdit;
    [Export] private Button _Login;
    [Export] private Label _ErrorLabel;
    private string _LastText;

    public string Address
    {
        get => _AddressEdit.Text;
        set => _AddressEdit.Text = value;
    }

    public string Password
    {
        get => _PasswordEdit.Text;
        set => _PasswordEdit.Text = value;
    }

    public string Slot
    {
        get => _SlotEdit.Text;
        set => _SlotEdit.Text = value;
    }

    public int Port { get; private set; } = 12345;

    public int PortField
    {
        get => Port;
        set => _PortEdit.Text = $"{Port = value}";
    }

    public override void _Ready()
    {
        _PortEdit.TextChanged += s =>
        {
            if (s.Trim() == "" || s.IsValidInt())
            {
                Port = int.TryParse(s.Trim(), out var port) ? port : 12345;
                _LastText = $"{Port}";
                return;
            }

            _PortEdit.Text = _LastText;
        };

        _Login.Pressed += TryConnection;
    }

    public void TryConnection()
    {
        Lock(true);
        Client = new ApClient();
        Client.OnConnectionLost += (_, _) => ConnectionFailed(["Connection Lost"]);
        var info = GetLoginInfo();

        Task.Run(() =>
        {
            try
            {
                string[] error;
                lock (Client)
                {
                    error = Client.TryConnect(info, 0x0AF5F0AC,
                        "DeathLinkipelago", AllItems,
                        tags: ["DeathLink"]);
                }

                if (error is not null && error.Length > 0)
                {
                    CallDeferred("ConnectionFailed", error);
                }
                else
                {
                    CallDeferred("Connected");
                }
            }
            catch (Exception e)
            {
                CallDeferred("ConnectionFailed", [e.Message, e.StackTrace]);
            }

            CallDeferred("Lock", false);
        });
    }

    public void TryDisconnection()
    {
        Client?.TryDisconnect();
        Disconnected();
    }

    public void Lock(bool toggle)
    {
        _Login.Disabled = toggle;
        _AddressEdit.Editable = !toggle;
        _PasswordEdit.Editable = !toggle;
        _PortEdit.Editable = !toggle;
        _SlotEdit.Editable = !toggle;
    }

    public void Connected()
    {
        var version = Client.SlotData.GetValueOrDefault("compatibility_version", "None");
        if (version is null or "None" or not ApWorldCompatibilityVersion)
        {
            ConnectionFailed([
                $"Incorrect version compatability.\nClient: [{ApWorldCompatibilityVersion}], ApWorld: [{version}]"
            ]);
            return;
        }

        Client.OnConnectionEvent += (_, _) => TryConnection();
        _Main.Connected();
    }

    public void Disconnected()
    {
        if (Client is not null)
        {
            Client = null;
        }

        MainController.Config = null;
        DeathCounter = [];
        LastSave = 0;
        LastDeathTrap = 0;
        NextLifeCoin = 0;
        ShopLevel = 0;
        HasChangedSinceLastSave = false;
        Reset();
        HintedItems = [];

        Inventory = new()
        {
            ["Death Trap"] = 0,
            ["Death Shield"] = 0,
            ["Death Coin"] = 0,
            ["Life Coin"] = 0
        };

        InventoryUsed = new(Inventory);

        Lock(false);
        _Main.SwitchScene(0);
    }

    public void ConnectionFailed(string[] error)
    {
        _ErrorLabel.Visible = true;
        _ErrorLabel.Text = string.Join("\n", error);
        TryDisconnection();
    }

    public LoginInfo GetLoginInfo() => new(Port, Slot, Address, Password);
}