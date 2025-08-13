using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace DeathLinkipelago.Scripts.Charts;

public partial class BlameChart : ColorRect
{
    private static readonly Dictionary<string, Color> _ColorCache = [];
    public static bool RefreshUi = false;

    [Export] private Label _Tooltip;

    private PiPolygon[] PiPolygons = [];
    private int _SelectedPoint = -1;

    public override void _Process(double delta)
    {
        if (!RefreshUi) return;
        QueueRedraw();
        RefreshUi = false;
    }

    public override void _Draw()
    {
        if (MainController.DeathCounter.IsEmpty) return;
        var size = Size;
        var total = MainController.DeathCounter.Values.Sum();
        var deathCounter = MainController.DeathCounter
                                         .Select(kv => new PiPolygonRawData(kv.Key, kv.Value, (float)kv.Value / total))
                                         .ToArray();

        var otherRaw = deathCounter.Where(t => t.Percent < .03).ToArray();
        var deathCounterList = deathCounter.Where(t => t.Percent >= .03).ToList();
        if (otherRaw.Length > 0)
        {
            var otherSum = otherRaw.Sum(p => p.Percent);
            var otherDeaths = otherRaw.Sum(p => p.Deaths);
            while (otherSum < .03 && deathCounterList.Count > 1)
            {
                var min = deathCounterList.MinBy(t => t.Percent);
                deathCounterList.Remove(min);
                otherSum += min.Percent;
            }

            if (otherSum < .03 && deathCounterList.Count == 1)
            {
                var last = deathCounterList[0];
                deathCounterList = [new PiPolygonRawData(last.Blame, last.Deaths, .97f)];
            }

            deathCounterList.Add(new PiPolygonRawData("Other", otherDeaths, Math.Max(.03f, otherSum)));
        }

        var angle = 0f;
        var center = size / 2f;
        var radius = Math.Min(size.X, size.Y) / 2f - 15;
        PiPolygons = deathCounterList
                    .OrderBy(t => t.Deaths)
                    .Select(t
                         => new PiPolygon(t.Blame, center, radius, angle, angle += t.Percent * 360, t.Deaths))
                    .ToArray();


        foreach (var poly in PiPolygons)
        {
            DrawColoredPolygon(poly.Polygon, poly.Color);
        }

        if (_SelectedPoint == -1) return;
        if (PiPolygons.Length == 1)
        {
            DrawArc(center, radius, 0, 360, 32, Colors.Black, 3);
        }
        else
        {
            DrawPolyline(PiPolygons[_SelectedPoint].PolygonLines, Colors.Black, 3);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouse mouse) return;
        var beforePoint = _SelectedPoint;
        _SelectedPoint = -1;
        var relativeMouse = mouse.Position - GlobalPosition;
        for (var i = 0; i < PiPolygons.Length; i++)
        {
            if (!Geometry2D.IsPointInPolygon(relativeMouse, PiPolygons[i].Polygon)) continue;
            _SelectedPoint = i;
        }

        if (beforePoint == _SelectedPoint) return;
        _Tooltip.Text = _SelectedPoint == -1 ? " \n " : PiPolygons[_SelectedPoint].ToString();
        QueueRedraw();
    }

    public static Vector2[] GetCircleArcPoly(Vector2 center, float radius, float angleFrom, float angleTo,
        int points = 32)
    {
        var pointsArc = new Vector2[points + 2];
        pointsArc[0] = center;

        for (var i = 0; i <= points; i++)
        {
            var anglePoint = Mathf.DegToRad(angleFrom + i * (angleTo - angleFrom) / points - 90);
            pointsArc[i + 1] = center + new Vector2(Mathf.Cos(anglePoint), Mathf.Sin(anglePoint)) * radius;
        }

        return pointsArc;
    }

    public static Color MakeRandomColor(string seed)
    {
        if (_ColorCache.TryGetValue(seed, out var color)) return color;

        var rand = new Random(BitConverter.ToInt32(SHA1.HashData(Encoding.UTF8.GetBytes(seed))));
        return _ColorCache[seed] =
            new Color(rand.Next(50, 256) / 255f, rand.Next(50, 256) / 255f, rand.Next(50, 256) / 255f);
    }
}

public readonly struct PiPolygonRawData(string blame, int deaths, float percent) : IEquatable<PiPolygonRawData>
{
    public readonly string Blame = blame;
    public readonly int Deaths = deaths;
    public readonly float Percent = percent;

    public bool Equals(PiPolygonRawData other)
        => Blame == other.Blame && Deaths == other.Deaths && Percent.Equals(other.Percent);

    public override bool Equals(object obj) => obj is PiPolygonRawData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Blame, Deaths, Percent);
}

public readonly struct PiPolygon
{
    public readonly string Blame;
    public readonly Color Color;
    public readonly Vector2[] Polygon;
    public readonly Vector2[] PolygonLines;
    public readonly int Deaths;

    public PiPolygon(string blame, Vector2 center, float radius, float start, float end, int deaths)
    {
        Blame = blame;
        Color = BlameChart.MakeRandomColor(blame);
        Polygon = BlameChart.GetCircleArcPoly(center, radius, start, end);
        PolygonLines = Polygon.Append(Polygon[0]).ToArray();
        Deaths = deaths;
    }

    public override string ToString() => $"To Blame: [{Blame}]\nDeaths: [{Deaths}]";
}