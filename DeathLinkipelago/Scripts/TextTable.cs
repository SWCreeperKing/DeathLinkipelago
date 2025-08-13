using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

namespace DeathLinkipelago.Scripts;

public abstract partial class TextTable : RichTextLabel
{
    [Export] private Array<string> _Columns = [];

    public void UpdateData(List<string[]> data)
    {
        StringBuilder sb = new();
        sb.Append("[table=").Append(_Columns.Count).Append(']');

        for (var i = 0; i < _Columns.Count; i++)
        {
            sb.Append("[cell bg=00000069] ").Append(GetColumnText(_Columns[i], i)).Append(" [/cell]");
        }

        for (var i = 0; i < data.Count; i++)
        {
            foreach (var item in data[i])
            {
                if (i % 2 == 0)
                {
                    sb.Append("[cell padding=0,2,0,2] ").Append(item).Append(" [/cell]");
                }
                else
                {
                    sb.Append("[cell bg=00000044 padding=0,2,0,0] ").Append(item).Append(" [/cell]");
                }
            }
        }

        sb.Append("[/table]");

        Text = sb.ToString();
    }

    public virtual string GetColumnText(string columnText, int columnNum) => columnText;
}