using System;
using CreepyUtil.Archipelago;
using Godot;

namespace DeathLinkipelago.Scripts;

public partial class DeathTracker : Control
{
    [Export] public Label Info;
    // [Export] public Node Bridge;
    public static string LastToDie;
    public static double TimeSinceLastDeath;
    public static double MaxTimeSinceLastDeath;
    public static bool ResetVals = false;
    // public static Queue<(double, string)> DeathQueue = [];
    public static int Counter = 2;

    public static void Reset()
    {
        ResetVals = true;
        LastToDie = null;
        TimeSinceLastDeath = 0;
        MaxTimeSinceLastDeath = 0;
        // DeathQueue.Clear();
        Counter = 2;
    }

    public override void _Process(double delta)
    {
        // if (ResetVals)
        // {
        //     ResetVals = false;
        //     Bridge.Call("clear_points", 0);
        // }

        if (LastToDie is null)
        {
            Info.Text = "No Deaths since after startup";
            return;
        }

        // while (DeathQueue.Count != 0)
        // {
        //     var item = DeathQueue.Dequeue();
        //     AddDeath((float) item.Item1);
        // }

        TimeSinceLastDeath += delta;
        Info.Text =
            $"Last Death: [{LastToDie}]\nSeconds since last death: [{TimeSinceLastDeath.GetAsTime()}]\nLongest Time since last death: [{MaxTimeSinceLastDeath.GetAsTime()}]";
    }

    public static void PlayerDied(string name)
    {
        // DeathQueue.Enqueue((TimeSinceLastDeath, name));
        MaxTimeSinceLastDeath = Math.Max(MaxTimeSinceLastDeath, TimeSinceLastDeath);
        TimeSinceLastDeath = 0;
        LastToDie = name;
    }

    // public void AddDeath(float y) => Bridge.Call("add_point", Counter++, y);
}