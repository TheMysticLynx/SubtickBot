using System.ComponentModel.DataAnnotations;

namespace SubtickBot.Data;

public class User
{
    public ulong Id { get; set; }
    public int LevelsCreated { get; set; }
    public int LevelsPreviewed { get; set; }
    public long TotalPreviewTime { get; set; }
    public int ErrorsThrown { get; set; }
    public int CommandsExecuted { get; set; }
    public List<Level> Levels { get; set; }
}

public class Level
{
    public ulong Id { get; set; }
    public ulong Author { get; set; }
    public byte[] Data { get; set; }
}