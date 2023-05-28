using CellEncoding;

namespace Core
{
    public struct LevelProperties : ILevelProperties
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public long Time { get; set; }
        public string DependMod { get; set; }
        public bool Vault { get; set; }


        [Obsolete]
        public byte[] ToBytes()
        {
            Name ??= "Default";
            Description ??= "";
            Author ??= "Unknown";
            Version ??= "1.0";
            DependMod ??= "";

            var writer = new BinaryWriter(new MemoryStream());
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Name);
            writer.Write(Description);
            writer.Write(Author);
            writer.Write(Version);
            writer.Write(DateTime.Now.Ticks);
            writer.Write(DependMod);
            writer.Write(Vault);
            return ((MemoryStream)writer.BaseStream).ToArray();
        }

        [Obsolete]
        public static LevelProperties FromStream(Stream bytes, string format)
        {
            var reader = new BinaryReader(bytes);
            var props = new LevelProperties
            {
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                Name = reader.ReadString(),
                Description = reader.ReadString(),
                Author = reader.ReadString(),
                Version = reader.ReadString(),
                Time = reader.ReadInt64(),
                DependMod = reader.ReadString(),
                Vault = format == "B2" && reader.ReadBoolean()
            };

            return props;
        }
    }

    public class LevelPropertiesChangeEventArgs : EventArgs
    {
        public LevelPropertiesChangeEventArgs(LevelProperties properties, LevelProperties oldProperties)
        {
            Properties = properties;
            OldProperties = oldProperties;
        }

        public LevelProperties Properties { get; private set; }
        public LevelProperties OldProperties { get; private set; }
    }
}