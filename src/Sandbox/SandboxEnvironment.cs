using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Base class for sandbox terrain demonstration environments.
/// Each environment showcases specific terrain rendering techniques.
/// </summary>
public abstract class SandboxEnvironment
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract int Width { get; }
    public abstract int Height { get; }

    protected TerrainTile[,] _tiles = null!;
    protected bool _initialized = false;

    /// <summary>
    /// Initialize the environment. Called once before first render.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _tiles = new TerrainTile[Width, Height];
        Generate();
        _initialized = true;
    }

    /// <summary>
    /// Generate the terrain for this environment.
    /// Override in derived classes to create specific terrain patterns.
    /// </summary>
    protected abstract void Generate();

    /// <summary>
    /// Update all terrain tile animations.
    /// </summary>
    public void Update(float delta)
    {
        if (!_initialized)
            return;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _tiles[x, y].Update(delta);
            }
        }
    }

    /// <summary>
    /// Render the environment to the ASCII buffer at the specified offset.
    /// </summary>
    public void Render(ASCIIBuffer buffer, int offsetX, int offsetY)
    {
        if (!_initialized)
            return;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int bufX = offsetX + x;
                int bufY = offsetY + y;

                if (bufX >= 0 && bufX < ASCIIBuffer.Width && bufY >= 0 && bufY < ASCIIBuffer.Height)
                {
                    var tile = _tiles[x, y];
                    buffer.SetCell(bufX, bufY, tile.CurrentChar, tile.CurrentForeground, tile.CurrentBackground);
                }
            }
        }
    }

    /// <summary>
    /// Reset the environment to its initial state.
    /// </summary>
    public void Reset()
    {
        _initialized = false;
        Initialize();
    }

    /// <summary>
    /// Get a tile at the specified position.
    /// </summary>
    public TerrainTile GetTile(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            return _tiles[x, y];
        return TerrainTile.Static(' ', ASCIIColors.BgDark, ASCIIColors.BgDark);
    }

    /// <summary>
    /// Set a tile at the specified position.
    /// </summary>
    protected void SetTile(int x, int y, TerrainTile tile)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _tiles[x, y] = tile;
    }

    /// <summary>
    /// Fill a rectangular area with a tile.
    /// </summary>
    protected void FillRect(int x, int y, int width, int height, TerrainTile tile)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                SetTile(x + dx, y + dy, tile);
            }
        }
    }

    /// <summary>
    /// Draw a horizontal line of tiles.
    /// </summary>
    protected void DrawHLine(int x, int y, int length, TerrainTile tile)
    {
        for (int dx = 0; dx < length; dx++)
        {
            SetTile(x + dx, y, tile);
        }
    }

    /// <summary>
    /// Draw a vertical line of tiles.
    /// </summary>
    protected void DrawVLine(int x, int y, int length, TerrainTile tile)
    {
        for (int dy = 0; dy < length; dy++)
        {
            SetTile(x, y + dy, tile);
        }
    }

    /// <summary>
    /// Fill the entire environment with a base tile.
    /// </summary>
    protected void FillAll(TerrainTile tile)
    {
        FillRect(0, 0, Width, Height, tile);
    }
}
