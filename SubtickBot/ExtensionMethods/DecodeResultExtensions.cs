using CellEncoding;
using Core;
using Modding;
using Modding.PublicInterfaces.Cells;
using UnityEngine;

namespace SubtickBot.ExtensionMethods;

public static class DecodeResultExtensions
{
    public class CellGridFascade : ICellGrid
    {
        private Dictionary<Vector2Int, BasicCell> _cells = new();
        private List<Vector2Int> _dragSpots = new();

        public void RemoveCell(BasicCell cell)
        {
            _cells.Remove(cell.Transform.Position);
        }

        public void RemoveCell(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            _cells.Remove(pos);
        }

        public void RemoveCell(Vector2Int position)
        {
            _cells.Remove(position);
        }

        public BasicCell? AddCell(Vector2Int position, Direction direction, int cellType, CellTransform? previousPosition = null)
        {
            var cell = new BasicCell(new Instance((short)cellType), new CellTransform(position, direction));
            _cells.Add(position, cell);
            return cell;
        }

        public void AddCell(BasicCell cell)
        {
            _cells.Add(cell.Transform.Position, cell);
        }

        public bool InBounds(int x, int y)
        {
            if (x < 0 || x >= Width)
                return false;

            if (y < 0 || y >= Height)
                return false;

            return true;
        }

        public bool InBounds(Vector2Int pos)
        {
            return InBounds(pos.x, pos.y);
        }

        public BasicCell? MoveCell(BasicCell cell, Vector2Int newPos)
        {
            if (!InBounds(newPos))
                return null;

            RemoveCell(cell);
            cell.Transform = cell.Transform.SetPosition(newPos);
            AddCell(cell);
            return cell;
        }

        public BasicCell RotateCell(BasicCell cell, Direction newDirection)
        {
            RemoveCell(cell);
            cell.Transform = cell.Transform.SetDirection(newDirection);
            AddCell(cell);
            return cell;
        }

        public bool IsDraggable(int x, int y)
        {
            return _dragSpots.Contains(new Vector2Int(x, y));
        }

        public void AddDragSpot(int x, int y)
        {
            _dragSpots.Add(new Vector2Int(x, y));
        }

        public Vector2Int[] GetDragSpots()
        {
            return _dragSpots.ToArray();
        }

        public BasicCell? GetCell(int x, int y)
        {
            if (!InBounds(x, y))
                return null;

            if(!_cells.TryGetValue(new Vector2Int(x, y), out var cell))
                return null;

            return cell;
        }

        public BasicCell? GetCell(Vector2Int pos)
        {
            return GetCell(pos.x, pos.y);
        }

        public bool PushCell(BasicCell cell, Direction direction, int force)
        {
            throw new NotImplementedException();
        }

        public CellProcessor GetCellProcessor(int type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BasicCell> GetCells()
        {
            return _cells.Values;
        }

        public IEnumerable<IEnumerable<BasicCell>> GetRows()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<BasicCell>> GetColumns()
        {
            throw new NotImplementedException();
        }

        public int Width { get; }
        public int Height { get; }
        public int Step { get; }

        public BasicCell? this[int x, int y] => GetCell(x, y);

        public BasicCell? this[Vector2Int pos] => GetCell(pos);

        public event EventHandler? OnStep;
        public event Action<BasicCell>? OnCellAdded;
        public event Action<BasicCell>? OnCellRemoved;
        public event Action<BasicCell, CellTransform>? OnCellChanged;
    }

    public class LevelFascade : ILevel
    {
        public int Width { get; }
        public int Height { get; }
        public ICellGrid CellGrid { get; }
        public ILevelProperties Properties { get; }

        public LevelFascade(int width, int height, ICellGrid cellGrid, ILevelProperties properties)
        {
            Width = width;
            Height = height;
            CellGrid = cellGrid;
            Properties = properties;
        }
    }

    public static ILevel ToLevel(this DecodeResult result)
    {
        var grid = new CellGridFascade();
        var props = new LevelProperties
        {
            Author = "",
            Description = result.DependMod,
            Name = result.Name,
            DependMod = result.DependMod,
            Height = result.Size.y,
            Width = result.Size.x,
            Time = 0,
            Vault = result.Vault,
            Version = ""
        };
        var newLevel = new LevelFascade(result.Size.x, result.Size.y, grid, props);
        foreach (var cell in result.Cells)
        {
            newLevel.CellGrid.AddCell(cell);
        }

        foreach (var spot in result.DragSpots)
        {
            grid.AddDragSpot(spot.x, spot.y);
        }

        return newLevel;
    }
}