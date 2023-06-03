using Modding.PublicInterfaces.Cells;
using UnityEngine;

namespace SubtickBot.ExtensionMethods;

public static class CellListExtensions
{
    public static List<BasicCell> ZoomOnCells(this IEnumerable<BasicCell> cells, ref Vector2Int[] dragSpots, int zoom, ref Vector2Int size)
    {
        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        var max = new Vector2Int(int.MinValue, int.MinValue);

        var cellsList = cells.ToList();

        //find min and max
        foreach (var cell in cellsList)
        {
            var pos = cell.Transform.Position;
            if (pos.x < min.x)
                min.x = pos.x;
            if (pos.y < min.y)
                min.y = pos.y;
            if (pos.x > max.x)
                max.x = pos.x;
            if (pos.y > max.y)
                max.y = pos.y;
        }

        min.x -= zoom;
        min.y -= zoom;
        max.x += zoom + 1;
        max.y += zoom + 1;

        min.x = Mathf.Clamp(min.x, 0, size.x);
        min.y = Mathf.Clamp(min.y, 0, size.y);
        max.x = Mathf.Clamp(max.x, 0, size.x);
        max.y = Mathf.Clamp(max.y, 0, size.y);

        size = max - min;

        //create new list with zoomed cells
        var zoomedCells = new List<BasicCell>();
        foreach (var cell in cellsList)
        {
            var pos = cell.Transform.Position;
            pos.x -= min.x;
            pos.y -= min.y;
            var newCell = cell;
            newCell.Transform = cell.Transform.SetPosition(pos);
            zoomedCells.Add(newCell);
        }

        var newDragSpots = new List<Vector2Int>();
        foreach (var spot in dragSpots)
        {
            var pos = spot;
            pos.x -= min.x;
            pos.y -= min.y;
            newDragSpots.Add(pos);
        }

        dragSpots = newDragSpots.ToArray();

        return zoomedCells;
    }
}