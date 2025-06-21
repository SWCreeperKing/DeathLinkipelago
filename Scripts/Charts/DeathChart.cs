using System;
using System.Collections.Generic;
using System.Linq;
using CreepyUtil.Archipelago;
using Godot;

namespace DeathLinkipelago.Scripts.Charts;

public partial class DeathChart : ColorRect
{
    [Export] private Font _Font;
    [Export] private Label _Tooltip;
    private readonly LimitedList<PointData> _DataPoints = new(50);
    private Vector2 _CharSize;
    private Vector2[] _CachedPoints = [];
    private int _SelectedPoint = -1;

    public override void _Ready() => _CharSize = _Font.GetStringSize(" ", HorizontalAlignment.Left, -1, 14);
    
    public override void _Draw()
    {
        if (_DataPoints.Count == 0) return;
        // initial calculations for high, mid, and low points
        var size = Size;
        var list = _DataPoints.GetList;
        var highRaw = list.Max(point => point.TimeSinceLastDeath);
        var lowRaw = list.Min(point => point.TimeSinceLastDeath);
        var mid = Math.Sqrt(highRaw * lowRaw);
        var highY = (float)Math.Ceiling(_CharSize.Y / 2f);
        var midY = size.Y / 2f;
        var lowY = size.Y - _CharSize.Y / 2f;

        // text
        var highText = highRaw.GetAsTime(false);
        var midText = mid.GetAsTime(false);
        var lowText = lowRaw.GetAsTime(false);

        // text sizes
        var highTextSize = highText.Length * _CharSize.X;
        var midTextSize = midText.Length * _CharSize.X;
        var lowTextSize = lowText.Length * _CharSize.X;
        var longest = Math.Max(highTextSize, Math.Max(midTextSize, lowTextSize)) + 2.5f + 7;

        // draw chart lines
        DrawLine(new Vector2(longest, highY), new Vector2(size.X, highY), Colors.DarkRed);
        DrawLine(new Vector2(longest, midY), new Vector2(size.X, midY), Colors.DarkRed);
        DrawLine(new Vector2(longest, lowY), new Vector2(size.X, lowY), Colors.DarkRed);
        DrawLine(new Vector2(longest, 0), new Vector2(longest, size.Y), Colors.LightSlateGray, 2);
        longest -= 7;

        // chart text
        DrawText(highText, new Vector2(2.5f, _CharSize.Y - 1), HorizontalAlignment.Right, longest);
        DrawText(midText, new Vector2(2.5f, size.Y / 2f + _CharSize.Y / 2f - 3), HorizontalAlignment.Right, longest);
        DrawText(lowText, new Vector2(2.5f, size.Y - 3), HorizontalAlignment.Right, longest);
        longest += 7;

        // calculate points and their locations
        var left = longest + 9;
        var right = size.X - 9;
        var width = right - left;
        var step = width / (_DataPoints.Count - 1);
        step = float.IsInfinity(step) ? width / 2 : step;
        var normalizedHeight = lowY - highY;
        
        var convertedPoints = list.All(point => point.TimeSinceLastDeath is >= 0 and <= 1)
            ? list.Select(point => point.TimeSinceLastDeath)
            : list.Select(point => Math.Log(point.TimeSinceLastDeath, highRaw)).ToArray();
        var lowestPoint = convertedPoints.Min();

        _CachedPoints = convertedPoints // math support: @unpingabot
                       .Select(point => (point - lowestPoint) / (1 - lowestPoint))
                       .Select((point, i)
                            => new Vector2(i * step + left, (float)(1 - point) * normalizedHeight + highY))
                       .ToArray();

        // draw points and line
        if (_CachedPoints.Length > 1)
        {
            DrawPolyline(_CachedPoints, Colors.CadetBlue, 2);
        }

        for (var i = 0; i < _CachedPoints.Length; i++)
        {
            DrawCircle(_CachedPoints[i], 5, _DataPoints[i].Color);
        }

        if (_SelectedPoint == -1) return;
        DrawArc(_CachedPoints[_SelectedPoint], 5, 0, 360, 16, Colors.Black, 3);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouse mouse) return;
        var beforePoint = _SelectedPoint;
        _SelectedPoint = -1;
        var relativeMouse = mouse.Position - GlobalPosition;
        for (var i = 0; i < _CachedPoints.Length; i++)
        {
            if (!Geometry2D.IsPointInCircle(relativeMouse, _CachedPoints[i], 7)) continue;
            _SelectedPoint = i;
        }

        if (beforePoint == _SelectedPoint) return;
        _Tooltip.Text = _SelectedPoint == -1
            ? " \n "
            : $"To Blame: [{_DataPoints[_SelectedPoint].ToBlame}]\nTime since last death: [{_DataPoints[_SelectedPoint].TimeSinceLastDeath.GetAsTime(false)}]";
        QueueRedraw();
    }

    public void AddDeath(string blame, double timeSinceLastDeath) => AddDeath(new PointData(blame, timeSinceLastDeath));

    public void AddDeath(PointData point)
    {
        _DataPoints.Add(point);
        QueueRedraw();
    }

    public void DrawText(string text, Vector2 pos, HorizontalAlignment align = HorizontalAlignment.Left,
        float width = -1)
        => DrawString(_Font, pos, text, align, width, 14, Colors.WebGray);
}

public readonly struct PointData(string toBlame, double timeSinceLastDeath)
{
    public readonly string ToBlame = toBlame;
    public readonly double TimeSinceLastDeath = Math.Max(0.001, timeSinceLastDeath);
    public readonly Color Color = BlameChart.MakeRandomColor(toBlame);
}