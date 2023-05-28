using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CellEncoding;
using Core;
using Modding;
using Modding.PublicInterfaces.Cells;
using UnityEngine;

namespace Indev2.Encoder
{
    [Obsolete]
    public class LevelFormatB : ILevelFormat
    {
        public string Identifier => "B2";
        public string CurrentVersion => "1.0.0";
        public string Name => "Legacy Beta Format";

        public (byte[], string) EncodeLevel(ILevel level)
        {
            var writer = new BinaryWriter(new MemoryStream());
            //Currently loaded mod
            writer.Write(level.Properties.DependMod);
            //Version of the mod
            writer.Write("N/A");
            //This is always the same
            writer.Write(Identifier);
            //write level properties
            writer.Write(new LevelProperties
            {
                Name = level.Properties.Name,
                Author = "",
                DependMod = level.Properties.DependMod,
                Description = level.Properties.Description,
                Height = level.Properties.Height,
                Width = level.Properties.Width,
                Time = 0L,
                Vault = level.Properties.Vault,
                Version = level.Properties.Version
            }.ToBytes());

            //Draggable objects
            var draggablePositions = level.CellGrid.GetDragSpots();
            var dragSpots = EncodeDragSpots(draggablePositions);
            writer.Write(dragSpots);

            //Write cells
            var cells = EncodeCells(level.CellGrid.GetCells().ToArray());
            writer.Write(cells);

            var bytesResult = ((MemoryStream)writer.BaseStream).ToArray();
            var base64Result = Convert.ToBase64String(bytesResult);

            return (bytesResult, base64Result);
        }

        public DecodeResult Decode(byte[] data)
        {
            var result = new DecodeResult();
            var reader = new BinaryReader(new MemoryStream(data));
            result.DependMod = reader.ReadString();
            reader.ReadString();
            result.Format = reader.ReadString();

            var props = LevelProperties.FromStream(reader.BaseStream, result.Format);
            result.Name = props.Name;
            result.Description = props.Description;
            result.Size = new Vector2Int(props.Width, props.Height);
            result.Vault = props.Vault;

            var dragSpots = DecodeDragSpots(reader.BaseStream);
            result.Cells = DecodeCells(reader.BaseStream);
            result.DragSpots = dragSpots;

            return result;
        }

        public DecodeResult Decode(string level)
        {
            return Decode(Convert.FromBase64String(level));
        }

        public bool Matches(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            try
            {
                reader.ReadString();
                reader.ReadString();
                return reader.ReadString().Equals(Identifier); //Legacy support
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Matches(string level)
        {
            try
            {
                return Matches(Convert.FromBase64String(level));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public byte[] EncodeCells(BasicCell[] cells)
        {
            var writer = new BinaryWriter(new MemoryStream());

            var typeSortedCells = new Dictionary<int, List<BasicCell>>();
            foreach (var cell in cells)
                if (!typeSortedCells.ContainsKey(cell.Instance.Type))
                    typeSortedCells.Add(cell.Instance.Type, new List<BasicCell> { cell });
                else
                    typeSortedCells[cell.Instance.Type].Add(cell);

            writer.Write((short)typeSortedCells.Count);
            foreach (var type in typeSortedCells)
            {
                var dirCount = type.Value.Select(a => a.Transform.Direction).Distinct().Count();
                writer.Write((short)type.Key);
                writer.Write((byte)dirCount);
                foreach (var direction in Direction.All)
                {
                    var writeCells = type.Value.Where(a => a.Transform.Direction == direction).ToArray();
                    if (!writeCells.Any())
                        continue;
                    writer.Write((byte)direction.AsInt);
                    writer.Write(writeCells.Count());
                    foreach (var cell in writeCells)
                    {
                        writer.Write((short)cell.Transform.Position.x);
                        writer.Write((short)cell.Transform.Position.y);
                    }
                }
            }

            var bytes = ((MemoryStream)writer.BaseStream).ToArray();
            return bytes;
        }

        public byte[] EncodeDragSpots(Vector2Int[] spots)
        {
            var bytes = new BinaryWriter(new MemoryStream());

            bytes.Write(spots.Length);

            foreach (var variable in spots)
            {
                bytes.Write(variable.x);
                bytes.Write(variable.y);
            }

            return ((MemoryStream)bytes.BaseStream).ToArray();
        }

        public BasicCell[] DecodeCells(Stream bytes)
        {
            var reader = new BinaryReader(bytes);
            var typeCount = reader.ReadInt16();
            var cells = new List<BasicCell>();
            for (var i = 0; i < typeCount; i++) cells.AddRange(DecodeType(bytes));

            return cells.ToArray();
        }

        public Vector2Int[] DecodeDragSpots(Stream bytes)
        {
            var reader = new BinaryReader(bytes);
            var count = reader.ReadInt32();
            var spots = new Vector2Int[count];
            for (var i = 0; i < count; i++) spots[i] = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());

            return spots;
        }

        public BasicCell[] DecodeType(Stream bytes)
        {
            var type = new BinaryReader(bytes).ReadInt16();
            var dirCount = new BinaryReader(bytes).ReadByte();

            var cells = new List<BasicCell>();
            for (var i = 0; i < dirCount; i++) cells.AddRange(DecodeDirection(bytes, type));

            return cells.ToArray();
        }

        public BasicCell[] DecodeDirection(Stream bytes, int type)
        {
            var reader = new BinaryReader(bytes);
            var direction = reader.ReadByte();
            var count = reader.ReadInt32();
            var cells = new BasicCell[count];
            for (var i = 0; i < count; i++)
            {
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var transform = new CellTransform(new Vector2Int(x, y), Direction.FromInt(direction));
                cells[i] = new BasicCell(type, transform);
            }

            return cells;
        }
    }
}