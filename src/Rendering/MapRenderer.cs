using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Systems;
using NullAndVoid.World;

namespace NullAndVoid.Rendering;

/// <summary>
/// Renders the game map (terrain) to the ASCII buffer or MapViewport.
/// Bridges between TileMapManager and the ASCII rendering system.
/// Supports zoom through the MapViewport.
/// </summary>
public class MapRenderer
{
    private readonly ASCIIBuffer _buffer;
    private MapViewport? _mapViewport;
    private readonly int _offsetX;
    private readonly int _offsetY;
    private int _viewWidth;
    private int _viewHeight;

    // Camera position for scrolling (center of view in map coordinates)
    public Vector2I CameraPosition { get; set; } = Vector2I.Zero;

    // Whether to use the MapViewport for rendering (enables zoom)
    public bool UseMapViewport { get; set; } = true;

    public MapRenderer(ASCIIBuffer buffer)
    {
        _buffer = buffer;
        _offsetX = ASCIIBuffer.MapStartX;
        _offsetY = ASCIIBuffer.MapStartY;
        _viewWidth = ASCIIBuffer.MapWidth;
        _viewHeight = ASCIIBuffer.MapHeight;
    }

    /// <summary>
    /// Set the MapViewport for zoomed rendering.
    /// </summary>
    public void SetMapViewport(MapViewport? viewport)
    {
        _mapViewport = viewport;
    }

    /// <summary>
    /// Get current view dimensions (may be smaller when zoomed).
    /// </summary>
    public (int width, int height) GetViewDimensions()
    {
        if (UseMapViewport && _mapViewport != null)
        {
            return (_mapViewport.ViewWidth, _mapViewport.ViewHeight);
        }
        return (_viewWidth, _viewHeight);
    }

    /// <summary>
    /// Render the map to the ASCII buffer or MapViewport, centered on the camera position.
    /// </summary>
    public void Render(TileMapManager tileMap)
    {
        if (tileMap == null)
            return;

        // Get current view dimensions (may be smaller when zoomed)
        var (viewW, viewH) = GetViewDimensions();

        // Calculate view bounds (what portion of the map is visible)
        int viewLeft = CameraPosition.X - viewW / 2;
        int viewTop = CameraPosition.Y - viewH / 2;

        // Clear viewport if using it
        if (UseMapViewport && _mapViewport != null)
        {
            _mapViewport.ClearBuffer();
        }

        for (int screenY = 0; screenY < viewH; screenY++)
        {
            for (int screenX = 0; screenX < viewW; screenX++)
            {
                int mapX = viewLeft + screenX;
                int mapY = viewTop + screenY;

                RenderTileToTarget(tileMap, mapX, mapY, screenX, screenY);
            }
        }

        // Note: Don't call _mapViewport.Render() here - entities need to be added first.
        // Call FinalizeViewport() after all map content (tiles, entities, effects) is rendered.
    }

    private void RenderTileToTarget(TileMapManager tileMap, int mapX, int mapY, int screenX, int screenY)
    {
        var mapPos = new Vector2I(mapX, mapY);

        char tileChar;
        Color tileColor;

        // Check bounds
        if (!tileMap.IsInBounds(mapPos))
        {
            tileChar = ASCIIChars.Void;
            tileColor = ASCIIColors.BgDark;
        }
        else
        {
            // Get visibility
            bool isVisible = tileMap.FOV.IsVisible(mapPos);
            bool isExplored = tileMap.FOV.IsExplored(mapPos);

            if (!isVisible && !isExplored)
            {
                tileChar = ASCIIChars.Fog;
                tileColor = ASCIIColors.TextDisabled;
            }
            else
            {
                // Get tile type
                var tileType = tileMap.GetTileAt(mapPos);
                switch (tileType)
                {
                    case TileMapManager.TileType.Floor:
                        tileChar = ASCIIChars.Floor;
                        tileColor = isVisible ? ASCIIColors.FloorLit : ASCIIColors.Floor;
                        break;
                    case TileMapManager.TileType.Wall:
                        tileChar = ASCIIChars.Wall;
                        tileColor = isVisible ? ASCIIColors.WallLit : ASCIIColors.Wall;
                        break;
                    default:
                        tileChar = ASCIIChars.Void;
                        tileColor = ASCIIColors.BgDark;
                        break;
                }

                if (!isVisible)
                {
                    tileColor = ASCIIColors.Dimmed(tileColor, 0.4f);
                }
            }
        }

        // Render to appropriate target
        if (UseMapViewport && _mapViewport != null)
        {
            _mapViewport.SetCell(screenX, screenY, tileChar, tileColor);
        }
        else
        {
            int bufferX = _offsetX + screenX;
            int bufferY = _offsetY + screenY;
            _buffer.SetCell(bufferX, bufferY, tileChar, tileColor, ASCIIColors.BgDark);
        }
    }

    private void RenderTile(TileMapManager tileMap, int mapX, int mapY, int bufferX, int bufferY)
    {
        var mapPos = new Vector2I(mapX, mapY);

        // Check bounds
        if (!tileMap.IsInBounds(mapPos))
        {
            // Out of bounds - render as void
            _buffer.SetCell(bufferX, bufferY, ASCIIChars.Void, ASCIIColors.BgDark, ASCIIColors.BgDark);
            return;
        }

        // Get visibility
        bool isVisible = tileMap.FOV.IsVisible(mapPos);
        bool isExplored = tileMap.FOV.IsExplored(mapPos);

        if (!isVisible && !isExplored)
        {
            // Unexplored - fog
            _buffer.SetCell(bufferX, bufferY, ASCIIChars.Fog, ASCIIColors.TextDisabled, ASCIIColors.BgDark);
            return;
        }

        // Get tile type and render accordingly
        var tileType = tileMap.GetTileAt(mapPos);
        char tileChar;
        Color tileColor;

        switch (tileType)
        {
            case TileMapManager.TileType.Floor:
                tileChar = ASCIIChars.Floor;
                tileColor = isVisible ? ASCIIColors.FloorLit : ASCIIColors.Floor;
                break;

            case TileMapManager.TileType.Wall:
                tileChar = ASCIIChars.Wall;
                tileColor = isVisible ? ASCIIColors.WallLit : ASCIIColors.Wall;
                break;

            case TileMapManager.TileType.Empty:
            default:
                tileChar = ASCIIChars.Void;
                tileColor = ASCIIColors.BgDark;
                break;
        }

        // Apply visibility dimming for explored but not visible
        if (!isVisible)
        {
            tileColor = ASCIIColors.Dimmed(tileColor, 0.4f);
        }

        _buffer.SetCell(bufferX, bufferY, tileChar, tileColor, ASCIIColors.BgDark);
    }

    /// <summary>
    /// Render a GameMap with destructible tiles to the ASCII buffer or MapViewport.
    /// </summary>
    public void Render(GameMap gameMap)
    {
        if (gameMap == null)
            return;

        // Get current view dimensions (may be smaller when zoomed)
        var (viewW, viewH) = GetViewDimensions();

        // Calculate view bounds (what portion of the map is visible)
        int viewLeft = CameraPosition.X - viewW / 2;
        int viewTop = CameraPosition.Y - viewH / 2;

        // Clear viewport if using it
        if (UseMapViewport && _mapViewport != null)
        {
            _mapViewport.ClearBuffer();
        }

        for (int screenY = 0; screenY < viewH; screenY++)
        {
            for (int screenX = 0; screenX < viewW; screenX++)
            {
                int mapX = viewLeft + screenX;
                int mapY = viewTop + screenY;

                RenderDestructibleTileToTarget(gameMap, mapX, mapY, screenX, screenY);
            }
        }

        // Note: Don't call _mapViewport.Render() here - entities need to be added first.
        // Call FinalizeViewport() after all map content (tiles, entities, effects) is rendered.
    }

    private void RenderDestructibleTileToTarget(GameMap gameMap, int mapX, int mapY, int screenX, int screenY)
    {
        var mapPos = new Vector2I(mapX, mapY);

        char displayChar;
        Color fgColor;

        // Check bounds
        if (!gameMap.IsInBounds(mapPos))
        {
            displayChar = ASCIIChars.Void;
            fgColor = ASCIIColors.BgDark;
        }
        else
        {
            // Get visibility from GameMap's FOV
            bool isVisible = gameMap.FOV.IsVisible(mapPos);
            bool isExplored = gameMap.FOV.IsExplored(mapPos);

            if (!isVisible && !isExplored)
            {
                displayChar = ASCIIChars.Fog;
                fgColor = ASCIIColors.TextDisabled;
            }
            else
            {
                // Get the destructible tile
                var tile = gameMap.GetTileSafe(mapX, mapY);

                // Get display properties from tile
                displayChar = tile.GetCurrentChar();
                fgColor = tile.GetCurrentForeground();

                // Apply visibility dimming for explored but not visible
                if (!isVisible)
                {
                    fgColor = ASCIIColors.Dimmed(fgColor, 0.4f);
                }
                else if (tile.Fire.IsActive)
                {
                    fgColor = fgColor.Lerp(ASCIIColors.FireBright, 0.2f);
                }
            }
        }

        // Render to appropriate target
        if (UseMapViewport && _mapViewport != null)
        {
            _mapViewport.SetCell(screenX, screenY, displayChar, fgColor);
        }
        else
        {
            int bufferX = _offsetX + screenX;
            int bufferY = _offsetY + screenY;
            _buffer.SetCell(bufferX, bufferY, displayChar, fgColor, ASCIIColors.BgDark);
        }
    }

    /// <summary>
    /// Render explosion effects on top of the map.
    /// Should be called after Render() to draw over tiles.
    /// </summary>
    public void RenderExplosions(GameMap gameMap)
    {
        if (gameMap == null)
            return;

        foreach (var visual in gameMap.ExplosionSys.ActiveVisuals)
        {
            float progress = visual.Progress;
            int currentRadius = (int)(visual.Radius * progress);

            for (int dy = -currentRadius; dy <= currentRadius; dy++)
            {
                for (int dx = -currentRadius; dx <= currentRadius; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Draw expanding ring
                    if (dist >= currentRadius - 1 && dist <= currentRadius)
                    {
                        int mapX = visual.Center.X + dx;
                        int mapY = visual.Center.Y + dy;

                        var bufferPos = MapToBuffer(new Vector2I(mapX, mapY));
                        if (bufferPos != null)
                        {
                            char explosionChar = progress < 0.3f ? '*' : progress < 0.6f ? '○' : '·';
                            Color color = visual.Color.Lerp(ASCIIColors.BgDark, progress);
                            _buffer.SetCell(bufferPos.Value.x, bufferPos.Value.y, explosionChar, color);
                        }
                    }
                }
            }

            // Center flash
            if (progress < 0.3f)
            {
                var centerBuffer = MapToBuffer(visual.Center);
                if (centerBuffer != null)
                {
                    _buffer.SetCell(centerBuffer.Value.x, centerBuffer.Value.y, '●', visual.Color);
                }
            }
        }
    }

    /// <summary>
    /// Convert screen buffer coordinates to map coordinates.
    /// </summary>
    public Vector2I BufferToMap(int bufferX, int bufferY)
    {
        var (viewW, viewH) = GetViewDimensions();
        int viewLeft = CameraPosition.X - viewW / 2;
        int viewTop = CameraPosition.Y - viewH / 2;

        return new Vector2I(
            viewLeft + (bufferX - _offsetX),
            viewTop + (bufferY - _offsetY)
        );
    }

    /// <summary>
    /// Convert map coordinates to screen buffer coordinates.
    /// Returns null if the position is not visible on screen.
    /// </summary>
    public (int x, int y)? MapToBuffer(Vector2I mapPos)
    {
        var (viewW, viewH) = GetViewDimensions();
        int viewLeft = CameraPosition.X - viewW / 2;
        int viewTop = CameraPosition.Y - viewH / 2;

        int screenX = mapPos.X - viewLeft;
        int screenY = mapPos.Y - viewTop;

        if (screenX < 0 || screenX >= viewW || screenY < 0 || screenY >= viewH)
            return null;

        // When using viewport, return screen coords; otherwise return buffer coords
        if (UseMapViewport && _mapViewport != null)
        {
            return (screenX, screenY);
        }
        return (_offsetX + screenX, _offsetY + screenY);
    }

    /// <summary>
    /// Check if a map position is visible on screen.
    /// </summary>
    public bool IsOnScreen(Vector2I mapPos)
    {
        var (viewW, viewH) = GetViewDimensions();
        int viewLeft = CameraPosition.X - viewW / 2;
        int viewTop = CameraPosition.Y - viewH / 2;

        int screenX = mapPos.X - viewLeft;
        int screenY = mapPos.Y - viewTop;

        return screenX >= 0 && screenX < viewW && screenY >= 0 && screenY < viewH;
    }

    /// <summary>
    /// Finalize the MapViewport rendering after all content is added.
    /// Call this after rendering map tiles, entities, and effects.
    /// </summary>
    public void FinalizeViewport()
    {
        if (UseMapViewport && _mapViewport != null)
        {
            _mapViewport.Render();
        }
    }

    /// <summary>
    /// Center the camera on a position.
    /// </summary>
    public void CenterOn(Vector2I position)
    {
        CameraPosition = position;
    }
}
