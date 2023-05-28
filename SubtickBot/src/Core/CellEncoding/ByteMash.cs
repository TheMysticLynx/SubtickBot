using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CellEncoding;
using Modding;
using Modding.PublicInterfaces.Cells;
using UnityEngine;

namespace Indev2.Encoder
{
    public class ByteMash : ILevelFormat
    {
        public string Name { get; }

        public (byte[], string) EncodeLevel(ILevel level)
        {
            var props = level.Properties;
            var writer = new BinaryWriter(new MemoryStream());

            var mod = level.Properties.DependMod;

            //VRRRSXXX    V = Vault, R = Revision, X = Reserved, S = Short Precision Type
            var isVault = props.Vault;
            var revision = 0;
            var shortPrecision = level.CellGrid.GetCells().Select(c => c.Instance.Type).Distinct().Count() > 64 ? 1 : 0;
            //Write the header
            writer.Write((byte)((isVault ? 1 : 0) << 7 | (revision & 0x3F) << 2 | (shortPrecision & 0x3)));
            //Write the name

            writer.Write(props.Name);
            writer.Write(props.DependMod);
            writer.Write(props.Description);
            writer.Write((short)props.Width);
            writer.Write((short)props.Height);

            // 00xxxxxx break
            // 01xxPSDx blank space run, P = Precision, S = No Length, D = Drag Spot
            // 10DDPSCD cell run D = Direction, P = Precision, S = No Length, C = Cell Type Precision, Drag Spot

            var length = 0;
            var index = 0;

            BasicCell? lastCell = null;
            bool lastDragSpot = false;


            while (index < props.Width * props.Height)
            {
                //sample the cell
                var pos = FromIndex(index, props.Width);
                var cell = level.CellGrid[pos];
                var dragSpot = level.CellGrid.IsDraggable(pos.x, pos.y);

                if (CellMatches(cell, lastCell) && dragSpot == lastDragSpot && index != 0)
                {
                    length++;
                }
                else
                {
                    if (index != 0)
                    {
                        WriteRun(length, lastCell, lastDragSpot, writer);
                        length = 0;
                    }
                    lastCell = cell;
                    lastDragSpot = dragSpot;
                    length = 1;
                }

                index++;
            }

            WriteRun(length, lastCell, lastDragSpot, writer);

            //write break
            writer.Write((byte)0);
//return the compressed bytes and the base64 string
            var compressed = ((MemoryStream)writer.BaseStream).ToArray();
            //add "ByteMash" to the start of the bytes
            compressed = Encoding.UTF8.GetBytes("|ByteMash|").Concat(compressed).ToArray();
            var base64 = "|ByteMash|" + Convert.ToBase64String(compressed);


            return (compressed, base64);
        }

        private static void WriteRun(int length, BasicCell? cell, bool dragSpot, BinaryWriter writer)
        {
            while (true)
            {
                // 00xxxxxx break
                // 01xxPSxD blank space run, P = Precision, S = No Length, D = Drag Spot
                // 10DDPSCD cell run D = Direction, P = Precision, S = No Length, C = Cell Type Precision, Drag Spot
                byte header = 0;
                bool doubleLengthPrecision = false;
                if (length > 255)
                {
                    doubleLengthPrecision = true;
                    header |= 0b00001000;
                }

                if (length == 1)
                {
                    header |= 0b00000100;
                }

                if (cell != null)
                {
                    header |= 0b10000000;
                    if (cell.Value.Instance.Type > 255)
                    {
                        header |= 0b00010000;
                    }

                    header |= (byte)(cell.Value.Transform.Direction.AsInt << 4);
                }
                else
                {
                    header |= 0b01000000;
                }

                if (dragSpot)
                {
                    header |= 0b00000001;
                }

                writer.Write(header);
                if (cell != null)
                {
                    //write the cell type
                    if (cell.Value.Instance.Type > 255)
                    {
                        writer.Write(cell.Value.Instance.Type);
                    }
                    else
                    {
                        writer.Write((byte)cell.Value.Instance.Type);
                    }
                }

                if (length != 1)
                {
                    if (doubleLengthPrecision)
                    {
                        if (length > ushort.MaxValue)
                        {
                            writer.Write(ushort.MaxValue);
                            length = length - ushort.MaxValue;
                            continue;
                        }
                        else
                        {
                            writer.Write((ushort)length);
                        }
                    }
                    else
                    {
                        writer.Write((byte)length);
                    }
                }

                break;
            }
        }

        public DecodeResult Decode(byte[] data)
        {
            //uncompress the bytes using gzip
            var uncompressed = new MemoryStream(data.Skip("|ByteMash|".Length).ToArray());

            //convert the uncompressed bytes to a binary reader
            var reader = new BinaryReader(uncompressed);

            //read the header
            var flags = reader.ReadByte();

            //VRRRSXXX    V = Vault, R = Revision, X = Reserved, S = Short Precision Type
            var isVault = (flags & 0x80) != 0;
            var revision = (flags & 0x7C) >> 2;
            var shortPrecision = flags & 0x3;

            //read the name
            var name = reader.ReadString();
            var dependMod = reader.ReadString();
            var description = reader.ReadString();
            var width = reader.ReadInt16();
            var height = reader.ReadInt16();

            var dragSpots = new List<Vector2Int>();
            var cells = new List<BasicCell>();

            var index = 0;
            while (true)
            {
                var header = reader.ReadByte();
                if (header == 0)
                {
                    break;
                }

                if ((header & 0b10000000) != 0)
                {
                    //cell run
                    var direction = Direction.FromInt((header & 0b01110000) >> 4);
                    var cellTypePrecision = (header & 0b00001000) != 0;
                    var noLength = (header & 0b00000100) != 0;
                    var precision = (header & 0b00000010) != 0;
                    var dragSpot = (header & 0b00000001) != 0;

                    var cellType = cellTypePrecision ? reader.ReadUInt16() : reader.ReadByte();
                    var length = noLength ? 1 : (precision ? reader.ReadUInt16() : reader.ReadByte());


                    for (var i = 0; i < length; i++)
                    {
                        var pos = FromIndex(index, width);
                        var cell = new BasicCell(new Instance((short)cellType), new CellTransform(pos, direction));
                        cells.Add(cell);
                        if (dragSpot)
                        {
                            dragSpots.Add(pos);
                        }

                        index++;
                    }
                }
                else
                {
                    //blank space run
                    var precision = (header & 0b00001000) != 0;
                    var noLength = (header & 0b00000100) != 0;
                    var dragSpot = (header & 0b00000001) != 0;

                    var length = noLength ? 1 : (precision ? reader.ReadUInt16() : reader.ReadByte());
                    for (var i = 0; i < length; i++)
                    {
                        if (dragSpot)
                        {
                            var pos = FromIndex(index, width);
                            dragSpots.Add(pos);
                        }

                        index++;
                    }
                }
            }


            //return result
            return new DecodeResult()
            {
                Name = name,
                DependMod = dependMod,
                Description = description,
                Size = new Vector2Int(width, height),
                Vault = isVault,
                DragSpots = dragSpots.ToArray(),
                Cells = cells.ToArray()
            };
        }

        public DecodeResult Decode(string level)
        {
            //convert the base64 string to bytes
            var bytes = Convert.FromBase64String(level.Substring("|ByteMash|".Length));
            return Decode(bytes);
        }

        public bool Matches(byte[] data)
        {
            var toMatch = Encoding.ASCII.GetBytes("|ByteMash|");
            if (data.Length < toMatch.Length)
            {
                return false;
            }

            for (var i = 0; i < toMatch.Length; i++)
            {
                if (data[i] != toMatch[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool Matches(string level)
        {
            //convert the base64 string to bytes
            return level.StartsWith("|ByteMash|");
        }

        private Vector2Int FromIndex(int index, int width)
        {
            return new Vector2Int(index % width, index / width);
        }

        void WriteState(ref byte type,ref int length, BinaryWriter writer,BasicCell? lastCell = null)
        {
            var newByte = (byte)(type | 0b11000000);
            bool isCellRun = (newByte | 0b10000000) == 1;

            if (isCellRun)
            {
                var direction = (byte)lastCell.Value.Transform.Direction.AsInt;
                newByte |= (byte)(direction << 4);
            }
            var doublePrecision = false;

            switch (length)
            {
                case 1:
                    newByte |= 0b00000001;
                    break;
                case > 255:
                    newByte |= 0b00000010;
                    doublePrecision = true;
                    break;
            }

            if (isCellRun)
            {
                if (lastCell.Value.Instance.Type > 255)
                {
                    newByte |= 0b00000100;
                    writer.Write(newByte);
                    writer.Write((ushort)lastCell.Value.Instance.Type);
                }
                else
                {
                    writer.Write(newByte);
                    writer.Write((byte)lastCell.Value.Instance.Type);
                }
            }
            else
            {
                writer.Write(newByte);
            }

            //write the byte or short
            if (doublePrecision)
            {
                if (length > ushort.MaxValue)
                    writer.Write(ushort.MaxValue);
                else
                    writer.Write((ushort)length);
            }
            else
            {
                writer.Write((byte)length);
            }

            //if above short precision call again after decrementing length
            if (length <= ushort.MaxValue) return;
            length -= ushort.MaxValue;
            WriteState(ref type, ref length, writer, lastCell);
        }

        private bool CellMatches(BasicCell? a, BasicCell? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Value.Instance.Type == b.Value.Instance.Type && a.Value.Transform.Direction == b.Value.Transform.Direction;
        }
    }
}