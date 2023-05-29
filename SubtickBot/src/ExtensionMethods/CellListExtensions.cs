using Modding;
using Modding.PublicInterfaces.Cells;
using UnityEngine;

namespace SubtickBot.ExtensionMethods;

public static class CellListExtensions
{
    public static List<BasicCell> ZoomOnCells(this IEnumerable<BasicCell> cells, int zoom, out Vector2Int size)
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

        //subtract min from all cells
        for (var index = 0; index < cellsList.Count; index++)
        {
            var cell = cellsList[index];
            var pos = cell.Transform.Position;
            var newTransform = cell.Transform.SetPosition(new Vector2Int(pos.x - min.x + zoom, pos.y - min.y + zoom));
            cell.Transform = newTransform;
            cellsList[index] = cell;
        }

        //set size
        size = new Vector2Int(max.x - min.x + zoom * 2 + 1, max.y - min.y + zoom * 2 + 1);

        return cellsList;
    }
}