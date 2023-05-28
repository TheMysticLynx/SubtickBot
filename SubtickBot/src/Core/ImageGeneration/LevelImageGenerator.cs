using CellEncoding;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;

namespace SubtickBot.Core.ImageGeneration;

public class LevelImageGenerator
{
    private Image[] _cellPreviews;
    private Image _grid;
    private Image _dragSpot;
    private Image _missing;

    const int ImageSize = 1024;

    public LevelImageGenerator(string spritesPath)
    {
        //print the path
        var dir = new DirectoryInfo(spritesPath);
        Console.WriteLine($"Loading sprites from {dir.FullName}");

        //load all cell sprites
        var cellCount = 10;
        _cellPreviews = new Image[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            var cell = Image.Load($"{spritesPath}/cell{i}.png");
            _cellPreviews[i] = cell;
        }

        //load the grid
        _grid = Image.Load($"{spritesPath}/grid.png");
        _missing = Image.Load($"{spritesPath}/missing.png");
        _dragSpot = Image.Load($"{spritesPath}/dragSpot.png");
    }

    public Image GenerateImage(DecodeResult source)
    {
        //generate the image
        //make largest dimension 1024
        var aspectRatio = (float) source.Size.x / source.Size.y;
        var width = aspectRatio > 1 ? ImageSize : (int) (ImageSize * aspectRatio);
        var height = aspectRatio < 1 ? ImageSize : (int) (ImageSize / aspectRatio);

        var image = new Image<Rgba32>(width, height);

        //get cell resolution based on the biggest dimension
        var cellResolution = 1024 / Math.Max(source.Size.x, source.Size.y);

        var cellSprites = _cellPreviews.Select(x => x.CloneAs<Rgba32>()).ToArray();

        foreach (var sprite in cellSprites)
        {
            sprite.Mutate(x => x.Resize(cellResolution, cellResolution, KnownResamplers.NearestNeighbor));
        }

        var grid = _grid.CloneAs<Rgba32>();
        grid.Mutate(x => x.Resize(cellResolution * 2, cellResolution * 2, KnownResamplers.NearestNeighbor));

        var missing = _missing.CloneAs<Rgba32>();
        missing.Mutate(x => x.Resize(cellResolution, cellResolution, KnownResamplers.NearestNeighbor));

        var dragSpot = _dragSpot.CloneAs<Rgba32>();
        dragSpot.Mutate(x => x.Resize(cellResolution, cellResolution, KnownResamplers.NearestNeighbor));

        for (var x = 0; x < source.Size.x; x+=2)
        {
            for (var y = 0; y < source.Size.y; y+=2)
            {
                //draw the grid
                image.Mutate(ctx => ctx.DrawImage(grid, new Point(x * cellResolution, y * cellResolution), 1));
            }
        }

        var dragSpots = source.DragSpots;
        foreach (var spot in dragSpots)
        {
            //draw the drag spot
            image.Mutate(ctx => ctx.DrawImage(dragSpot, new Point(spot.x * cellResolution, spot.y * cellResolution), 1));
        }

        //draw the cells
        var cells = source.Cells;
        foreach (var cell in cells)
        {
            //draw the cell
            var cellSprite = missing;
            if (cell.Instance.Type < cellSprites.Length) cellSprite = cellSprites[cell.Instance.Type];

            //rotate the cell
            var rotation = cell.Transform.Direction.AsInt switch
            {
                0 => 0,
                1 => 90,
                2 => 180,
                3 => 270,
                _ => 0
            };

            cellSprite.Mutate(x => x.Rotate(rotation));

            var xPos = cell.Transform.Position.x * cellResolution;
            var yPos = (source.Size.y - cell.Transform.Position.y - 1) * cellResolution;

            //draw the cell
            image.Mutate(ctx => ctx.DrawImage(cellSprite, new Point(xPos, yPos), 1));

            //reset the rotation
            cellSprite.Mutate(x => x.Rotate(-rotation));
        }

        //bloom the image
        var bloom = image.CloneAs<Rgba32>();
        bloom.Mutate(x => x.GaussianBlur(5));

        var bloomAmount = .5f;
        var bloomMin = 128;

        //loop through all pixels of bloom copy and subtract the min
        for (var x = 0; x < bloom.Width; x++)
        {
            for (var y = 0; y < bloom.Height; y++)
            {
                var pixel = bloom[x, y];
                bloom[x, y] = new Rgba32(
                    (byte) Math.Max(pixel.R - bloomMin, 0),
                    (byte) Math.Max(pixel.G - bloomMin, 0),
                    (byte) Math.Max(pixel.B - bloomMin, 0),
                    pixel.A);
            }
        }


        //loop through all pixels and add the bloom
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                var pixel = image[x, y];
                var bloomPixel = bloom[x, y];
                image[x, y] = new Rgba32(
                    (byte) Math.Min(pixel.R + bloomPixel.R * bloomAmount, 255),
                    (byte) Math.Min(pixel.G + bloomPixel.G * bloomAmount, 255),
                    (byte) Math.Min(pixel.B + bloomPixel.B * bloomAmount, 255),
                    pixel.A);
            }
        }

        return image;
    }
}