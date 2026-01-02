using System;
using System.Text;
using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Character grid buffer for ASCII rendering.
/// Optimized for 2560x1440 display at font size 24.
///
/// Layout (180 wide x 50 tall):
/// ┌────────────────────────────────────────────────────────────────────────┬────────────────────────────┐
/// │ Row 0: Top border                                                      │                            │
/// │ Row 1-2: Message log (2 lines)                                         │                            │
/// │ Row 3: Separator                                                       │                            │
/// ├────────────────────────────────────────────────────────────────────────┤ Sidebar                    │
/// │                                                                        │ (30 chars)                 │
/// │ Rows 4-45: Map area                                                    │ >> STATUS <<               │
/// │ (149 chars wide x 42 tall)                                             │ HP, Equipment, Nearby      │
/// │                                                                        │                            │
/// ├────────────────────────────────────────────────────────────────────────┴────────────────────────────┤
/// │ Row 46: Separator                                                                                   │
/// │ Rows 47-48: Weapon bar (2 lines)                                                                    │
/// │ Row 49: Status bar / key hints                                                                      │
/// └─────────────────────────────────────────────────────────────────────────────────────────────────────┘
/// </summary>
public class ASCIIBuffer
{
    // Target resolution: 2560x1440 at font size 24
    // Character dimensions at font 24: approximately 14px wide x 28px tall
    // 2560 / 14 ≈ 183, 1440 / 28 ≈ 51
    // Using 180x50 for clean layout

    // Screen dimensions
    public const int Width = 180;
    public const int Height = 50;

    // Layout regions - vertical
    public const int MessageStartY = 0;
    public const int MessageLines = 3;          // Rows 0-2 (border + 2 message lines)
    public const int MessageSeparatorY = 3;     // Row 3: separator line

    public const int MapStartY = 4;             // Rows 4-45: main map area
    public const int MapHeight = 42;            // 42 rows of map

    public const int WeaponBarSeparatorY = 46;  // Row 46: separator above weapon bar
    public const int WeaponBarY = 47;           // Rows 47-48: weapon bar (2 lines)
    public const int WeaponBarHeight = 2;

    public const int StatusBarLine = 49;        // Row 49: status bar

    // Layout regions - horizontal
    public const int SidebarWidth = 30;
    public const int SidebarX = Width - SidebarWidth;  // Column 150
    public const int MapStartX = 0;
    public const int MapWidth = Width - SidebarWidth - 1;  // 149 chars (with 1 for separator)
    public const int MapSeparatorX = MapWidth;  // Column 149: vertical separator

    private readonly ASCIICell[,] _cells;
    private readonly ASCIICell[,] _previousCells;
    private bool _fullRedrawNeeded = true;

    public ASCIIBuffer()
    {
        _cells = new ASCIICell[Width, Height];
        _previousCells = new ASCIICell[Width, Height];
        Clear();
    }

    /// <summary>
    /// Clear the entire buffer to default state.
    /// </summary>
    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[x, y] = ASCIICell.Empty();
            }
        }
        _fullRedrawNeeded = true;
    }

    /// <summary>
    /// Clear a specific region of the buffer.
    /// </summary>
    public void ClearRegion(int startX, int startY, int width, int height)
    {
        for (int y = startY; y < startY + height && y < Height; y++)
        {
            for (int x = startX; x < startX + width && x < Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    _cells[x, y] = ASCIICell.Empty();
                }
            }
        }
    }

    /// <summary>
    /// Set a cell with static colors.
    /// </summary>
    public void SetCell(int x, int y, char character, Color foreground, Color background)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _cells[x, y] = ASCIICell.Create(character, foreground, background);
    }

    /// <summary>
    /// Set a cell with just character and foreground (transparent background).
    /// </summary>
    public void SetCell(int x, int y, char character, Color foreground)
    {
        SetCell(x, y, character, foreground, ASCIIColors.BgDark);
    }

    /// <summary>
    /// Set a cell with a dancing foreground color.
    /// </summary>
    public void SetDancingCell(int x, int y, char character, DancingColor foreground, Color background)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _cells[x, y] = ASCIICell.CreateDancing(character, foreground, background);
    }

    /// <summary>
    /// Set a cell with both dancing colors.
    /// </summary>
    public void SetFullDancingCell(int x, int y, char character, DancingColor foreground, DancingColor background)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _cells[x, y] = ASCIICell.CreateFullDancing(character, foreground, background);
    }

    /// <summary>
    /// Get a cell (for reading).
    /// </summary>
    public ASCIICell GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return ASCIICell.Empty();

        return _cells[x, y];
    }

    /// <summary>
    /// Set just the background color of a cell, preserving character and foreground.
    /// </summary>
    public void SetCellBackground(int x, int y, Color background)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        var existing = _cells[x, y];
        _cells[x, y] = ASCIICell.Create(existing.Character, existing.GetEffectiveForeground(), background);
    }

    /// <summary>
    /// Alias for WriteString for compatibility.
    /// </summary>
    public void DrawString(int x, int y, string text, Color foreground)
    {
        WriteString(x, y, text, foreground);
    }

    /// <summary>
    /// Set cell visibility state.
    /// </summary>
    public void SetCellVisibility(int x, int y, bool isVisible, bool wasSeen)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _cells[x, y].SetVisibility(isVisible, wasSeen);
    }

    /// <summary>
    /// Set cell light level.
    /// </summary>
    public void SetCellLighting(int x, int y, float lightLevel)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _cells[x, y].LightLevel = lightLevel;
        _cells[x, y].NeedsUpdate = true;
    }

    /// <summary>
    /// Write a string horizontally starting at position.
    /// </summary>
    public void WriteString(int x, int y, string text, Color foreground, Color background)
    {
        for (int i = 0; i < text.Length && x + i < Width; i++)
        {
            if (x + i >= 0)
            {
                SetCell(x + i, y, text[i], foreground, background);
            }
        }
    }

    /// <summary>
    /// Write a string with just foreground color.
    /// </summary>
    public void WriteString(int x, int y, string text, Color foreground)
    {
        WriteString(x, y, text, foreground, ASCIIColors.BgDark);
    }

    /// <summary>
    /// Write a centered string on a line.
    /// </summary>
    public void WriteCentered(int y, string text, Color foreground)
    {
        int x = (Width - text.Length) / 2;
        WriteString(x, y, text, foreground);
    }

    /// <summary>
    /// Write a centered string within a region.
    /// </summary>
    public void WriteCenteredInRegion(int startX, int width, int y, string text, Color foreground)
    {
        int x = startX + (width - text.Length) / 2;
        WriteString(x, y, text, foreground);
    }

    /// <summary>
    /// Draw a horizontal line.
    /// </summary>
    public void DrawHorizontalLine(int x, int y, int length, Color color, char character = ASCIIChars.BoxH)
    {
        for (int i = 0; i < length && x + i < Width; i++)
        {
            if (x + i >= 0)
            {
                SetCell(x + i, y, character, color);
            }
        }
    }

    /// <summary>
    /// Draw a vertical line.
    /// </summary>
    public void DrawVerticalLine(int x, int y, int length, Color color, char character = ASCIIChars.BoxV)
    {
        for (int i = 0; i < length && y + i < Height; i++)
        {
            if (y + i >= 0)
            {
                SetCell(x, y + i, character, color);
            }
        }
    }

    /// <summary>
    /// Draw a box outline.
    /// </summary>
    public void DrawBox(int x, int y, int width, int height, Color color, bool doubleLines = false)
    {
        if (width < 2 || height < 2)
            return;

        char h = doubleLines ? ASCIIChars.BoxHD : ASCIIChars.BoxH;
        char v = doubleLines ? ASCIIChars.BoxVD : ASCIIChars.BoxV;
        char tl = doubleLines ? ASCIIChars.BoxTLD : ASCIIChars.BoxTL;
        char tr = doubleLines ? ASCIIChars.BoxTRD : ASCIIChars.BoxTR;
        char bl = doubleLines ? ASCIIChars.BoxBLD : ASCIIChars.BoxBL;
        char br = doubleLines ? ASCIIChars.BoxBRD : ASCIIChars.BoxBR;

        // Corners
        SetCell(x, y, tl, color);
        SetCell(x + width - 1, y, tr, color);
        SetCell(x, y + height - 1, bl, color);
        SetCell(x + width - 1, y + height - 1, br, color);

        // Horizontal lines
        DrawHorizontalLine(x + 1, y, width - 2, color, h);
        DrawHorizontalLine(x + 1, y + height - 1, width - 2, color, h);

        // Vertical lines
        DrawVerticalLine(x, y + 1, height - 2, color, v);
        DrawVerticalLine(x + width - 1, y + 1, height - 2, color, v);
    }

    /// <summary>
    /// Fill a rectangular region with a character and color.
    /// </summary>
    public void FillRegion(int x, int y, int width, int height, char character, Color foreground, Color background)
    {
        for (int py = y; py < y + height && py < Height; py++)
        {
            for (int px = x; px < x + width && px < Width; px++)
            {
                if (px >= 0 && py >= 0)
                {
                    SetCell(px, py, character, foreground, background);
                }
            }
        }
    }

    /// <summary>
    /// Update all dancing colors. Call each frame.
    /// </summary>
    public void Update(float delta)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[x, y].Update(delta);
            }
        }
    }

    /// <summary>
    /// Check if a full redraw is needed.
    /// </summary>
    public bool NeedsFullRedraw => _fullRedrawNeeded;

    /// <summary>
    /// Mark that a full redraw has been completed.
    /// </summary>
    public void MarkRedrawn()
    {
        _fullRedrawNeeded = false;
        Array.Copy(_cells, _previousCells, _cells.Length);
    }

    /// <summary>
    /// Force a full redraw next frame.
    /// </summary>
    public void Invalidate()
    {
        _fullRedrawNeeded = true;
    }

    /// <summary>
    /// Render buffer to BBCode string for RichTextLabel.
    /// </summary>
    public string ToBBCode()
    {
        var sb = new StringBuilder(Width * Height * 30);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var cell = _cells[x, y];
                var fgHex = cell.GetForegroundHex();
                var character = cell.GetEffectiveCharacter();

                string charStr = character switch
                {
                    '[' => "[lb]",
                    ']' => "[rb]",
                    _ => character.ToString()
                };

                sb.Append($"[color=#{fgHex}]{charStr}[/color]");
            }
            if (y < Height - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Render buffer to plain text (for debugging).
    /// </summary>
    public string ToPlainText()
    {
        var sb = new StringBuilder(Width * Height + Height);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                sb.Append(_cells[x, y].GetEffectiveCharacter());
            }
            if (y < Height - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convert world/map coordinates to buffer coordinates.
    /// </summary>
    public (int x, int y) MapToBuffer(int mapX, int mapY)
    {
        return (MapStartX + mapX, MapStartY + mapY);
    }

    /// <summary>
    /// Check if map coordinates are within visible map area.
    /// </summary>
    public bool IsInMapBounds(int mapX, int mapY)
    {
        return mapX >= 0 && mapX < MapWidth && mapY >= 0 && mapY < MapHeight;
    }

    /// <summary>
    /// Get the map area dimensions.
    /// </summary>
    public static (int width, int height) GetMapDimensions()
    {
        return (MapWidth, MapHeight);
    }
}
