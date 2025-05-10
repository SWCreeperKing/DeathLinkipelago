using System;
using System.Collections.Concurrent;
using CreepyUtil.Archipelago;
using DeathLinkipelago.Scripts.Charts;
using Godot;

namespace DeathLinkipelago.Scripts;

public partial class DeathTracker : Control
{
    [Export] public Label Info;
    [Export] private DeathChart Chart;
    [Export] private BlameChart BlamePi;
    public static string LastToDie;
    public static double TimeSinceLastDeath;
    public static double MaxTimeSinceLastDeath;
    private readonly static ConcurrentQueue<PointData> DeathQueue = [];

    public static void Reset()
    {
        LastToDie = null;
        TimeSinceLastDeath = 0;
        MaxTimeSinceLastDeath = 0;
    }

    public override void _Process(double delta)
    {
        if (!DeathQueue.IsEmpty)
        {
            DeathQueue.TryDequeue(out var pointData);
            Chart.AddDeath(pointData);
        }
        
        if (LastToDie is null)
        {
            Info.Text = "No Deaths since after startup";
            return;
        }

        TimeSinceLastDeath += delta;
        Info.Text =
            $"Last Death: [{LastToDie}]\nSeconds since last death: [{TimeSinceLastDeath.GetAsTime()}]\nLongest Time since last death: [{MaxTimeSinceLastDeath.GetAsTime()}]";
    }

    public static void PlayerDied(string name)
    {
        DeathQueue.Enqueue(new PointData(name, TimeSinceLastDeath));
        MaxTimeSinceLastDeath = Math.Max(MaxTimeSinceLastDeath, TimeSinceLastDeath);
        TimeSinceLastDeath = 0;
        LastToDie = name;
        BlameChart.RefreshUi = true;
    }
}