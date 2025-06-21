using System;
using System.IO;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using static DeathLinkipelago.Scripts.MainController;
using Range = Godot.Range;

namespace DeathLinkipelago.Scripts;

public partial class Settings : VBoxContainer
{
    public static SettingsData Data = new();
    [Export] private Theme _Theme;
    [Export] private Dictionary<string, Control> DataControls = [];
    [Export] private Label _AnonymizePercentLabel;
    private event EventHandler<string> ValueChanged;

    public override void _EnterTree()
    {
        if (!File.Exists($"{SaveDir}/Settings.txt")) return;
        Data = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText($"{SaveDir}/Settings.txt"));
    }

    public override void _Ready()
    {
        ValueChanged += (control, id) =>
        {
            switch (id)
            {
                case "anonymize_n%":
                    _AnonymizePercentLabel.Text = $"{((Range)control)!.Value * 10}%";
                    break;
                case "global_font":
                    _Theme.DefaultFontSize = (int)((Range)control)!.Value;
                    break;
                case "hide_traps" or "hide_shields" or "separated_hinted":
                    RefreshUI = true;
                    break;
            }
        };

        foreach (var (id, control) in DataControls)
        {
            switch (control)
            {
                case CheckBox checkBox:
                    checkBox.ButtonPressed = Data.GetBool(id, checkBox.ButtonPressed);
                    checkBox.Pressed += () =>
                    {
                        Data.SetBool(id, checkBox.ButtonPressed);
                        ValueChanged?.Invoke(checkBox, id);
                    };
                    ValueChanged?.Invoke(checkBox, id);
                    break;
                case Range range:
                    range.Value = Data.GetFloat(id, (float)range.Value);
                    range.ValueChanged += d =>
                    {
                        Data.SetFloat(id, (float)d);
                        ValueChanged?.Invoke(range, id);
                    };
                    ValueChanged?.Invoke(range, id);
                    break;
            }
        }
    }

    public static void OpenSaveDir() => OS.ShellOpen(SaveDir);
    public static void Save() => File.WriteAllText($"{SaveDir}/Settings.txt", JsonConvert.SerializeObject(Data));
}