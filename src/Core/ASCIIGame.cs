using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Core;

/// <summary>
/// Main game controller using pure ASCII rendering.
/// This is the new entry point for the ASCII-based visual system.
/// </summary>
public partial class ASCIIGame : Control
{
    private ASCIIRenderer? _renderer;
    private float _demoTimer = 0f;
    private int _playerX = 50;
    private int _playerY = 15;

    public override void _Ready()
    {
        // Create the ASCII renderer
        _renderer = new ASCIIRenderer
        {
            FontSize = 14 // Smaller font to fit 120x40 in reasonable window
        };
        _renderer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_renderer);

        // Hook into render events
        _renderer.OnDraw += DrawFrame;

        // Initial draw
        DrawFrame();

        GD.Print("[ASCIIGame] ASCII rendering system initialized");
        GD.Print($"[ASCIIGame] Buffer size: {ASCIIBuffer.Width}x{ASCIIBuffer.Height}");
    }

    public override void _Process(double delta)
    {
        _demoTimer += (float)delta;

        // Handle demo movement
        HandleInput();
    }

    private void HandleInput()
    {
        bool moved = false;

        if (Input.IsActionJustPressed("move_up") && _playerY > ASCIIBuffer.MapStartY)
        {
            _playerY--;
            moved = true;
        }
        if (Input.IsActionJustPressed("move_down") && _playerY < ASCIIBuffer.MapStartY + ASCIIBuffer.MapHeight - 1)
        {
            _playerY++;
            moved = true;
        }
        if (Input.IsActionJustPressed("move_left") && _playerX > ASCIIBuffer.MapStartX)
        {
            _playerX--;
            moved = true;
        }
        if (Input.IsActionJustPressed("move_right") && _playerX < ASCIIBuffer.MapStartX + ASCIIBuffer.MapWidth - 1)
        {
            _playerX++;
            moved = true;
        }

        if (moved)
        {
            _renderer?.Buffer.Invalidate();
        }
    }

    private void DrawFrame()
    {
        if (_renderer == null)
            return;

        var buffer = _renderer.Buffer;
        buffer.Clear();

        // Draw the complete UI layout
        DrawMessageArea(buffer);
        DrawMapArea(buffer);
        DrawSidebar(buffer);
        DrawStatusBar(buffer);

        // Draw demo content
        DrawDemoMap(buffer);
        DrawPlayer(buffer);
    }

    private void DrawMessageArea(ASCIIBuffer buffer)
    {
        // Message area: top 3 lines
        buffer.DrawHorizontalLine(0, 0, ASCIIBuffer.Width, ASCIIColors.Border);

        // Demo messages
        buffer.WriteString(1, 1, "> You descend deeper into the facility...", ASCIIColors.TextSecondary);
        buffer.WriteString(1, 2, "> A hostile drone emerges from the shadows!", ASCIIColors.AlertDanger);

        buffer.DrawHorizontalLine(0, 3, ASCIIBuffer.Width, ASCIIColors.Border);
    }

    private void DrawMapArea(ASCIIBuffer buffer)
    {
        // Map border
        int mapEndX = ASCIIBuffer.Width - ASCIIBuffer.SidebarWidth - 1;

        // Draw vertical separator between map and sidebar
        buffer.DrawVerticalLine(mapEndX, ASCIIBuffer.MapStartY, ASCIIBuffer.MapHeight, ASCIIColors.Border);
    }

    private void DrawSidebar(ASCIIBuffer buffer)
    {
        int x = ASCIIBuffer.SidebarX;
        int y = ASCIIBuffer.MapStartY;

        // Status header
        buffer.WriteCenteredInRegion(x, ASCIIBuffer.SidebarWidth, y, ">> STATUS <<", ASCIIColors.PrimaryBright);
        y++;
        buffer.DrawHorizontalLine(x, y, ASCIIBuffer.SidebarWidth, ASCIIColors.Border);
        y++;

        // Health bar
        float healthPercent = 0.75f;
        string healthBar = ASCIIChars.ProgressBar(healthPercent, 12);
        buffer.WriteString(x + 1, y, $"HP: {healthBar}", ASCIIColors.GetHealthColor(healthPercent));
        y++;
        buffer.WriteString(x + 1, y, "    75/100", ASCIIColors.TextSecondary);
        y++;

        // Stats
        buffer.WriteString(x + 1, y, "AT: 12  AR: 3  SR: 8", ASCIIColors.TextSecondary);
        y += 2;

        // Equipment header
        buffer.DrawHorizontalLine(x, y, ASCIIBuffer.SidebarWidth, ASCIIColors.Border);
        y++;
        buffer.WriteCenteredInRegion(x, ASCIIBuffer.SidebarWidth, y, ">> EQUIPMENT <<", ASCIIColors.PrimaryBright);
        y++;

        // Equipment slots
        buffer.WriteString(x + 1, y++, "C1: Salvaged Blaster", ASCIIColors.SlotCore);
        buffer.WriteString(x + 1, y++, "C2: [empty]", ASCIIColors.Dimmed(ASCIIColors.SlotCore));
        buffer.WriteString(x + 1, y++, "U1: Prox Scanner", ASCIIColors.SlotUtility);
        buffer.WriteString(x + 1, y++, "U2: [empty]", ASCIIColors.Dimmed(ASCIIColors.SlotUtility));
        buffer.WriteString(x + 1, y++, "B1: Scrap Plating", ASCIIColors.SlotBase);
        buffer.WriteString(x + 1, y++, "B2: Hover Treads", ASCIIColors.SlotBase);
        y++;

        // Nearby header
        buffer.DrawHorizontalLine(x, y, ASCIIBuffer.SidebarWidth, ASCIIColors.Border);
        y++;
        buffer.WriteCenteredInRegion(x, ASCIIBuffer.SidebarWidth, y, ">> NEARBY <<", ASCIIColors.PrimaryBright);
        y++;

        // Nearby entities
        buffer.WriteString(x + 1, y++, "d Drone (wounded)", ASCIIColors.Enemy);
        buffer.WriteString(x + 1, y++, "* Rare Module", ASCIIColors.Rare);
        buffer.WriteString(x + 1, y++, "> Stairs down", ASCIIColors.Stairs);
    }

    private void DrawStatusBar(ASCIIBuffer buffer)
    {
        int y = ASCIIBuffer.StatusBarLine;

        // Status bar separator
        buffer.DrawHorizontalLine(0, y - 1, ASCIIBuffer.Width, ASCIIColors.Border);

        // Key hints
        buffer.WriteString(1, y, "[?]Help", ASCIIColors.TextMuted);
        buffer.WriteString(10, y, "[I]Inventory", ASCIIColors.TextMuted);
        buffer.WriteString(24, y, "[E]Equipment", ASCIIColors.TextMuted);
        buffer.WriteString(38, y, "[>]Descend", ASCIIColors.TextMuted);

        // Turn counter (right aligned)
        string turnText = "Turn: 142";
        buffer.WriteString(ASCIIBuffer.Width - turnText.Length - 1, y, turnText, ASCIIColors.Primary);
    }

    private void DrawDemoMap(ASCIIBuffer buffer)
    {
        // Draw a simple demo map in the map area
        int startX = ASCIIBuffer.MapStartX;
        int startY = ASCIIBuffer.MapStartY;
        int mapW = ASCIIBuffer.MapWidth;
        int mapH = ASCIIBuffer.MapHeight;

        // Fill with floor
        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                int bx = startX + x;
                int by = startY + y;

                // Create some variety
                bool isWall = x == 0 || y == 0 || x == mapW - 1 || y == mapH - 1;
                bool isRoom = x > 10 && x < 25 && y > 5 && y < 15;
                bool isRoomWall = isRoom && (x == 10 || x == 25 || y == 5 || y == 15);
                bool isDoor = isRoom && x == 17 && y == 15;

                if (isWall || (isRoomWall && !isDoor))
                {
                    buffer.SetCell(bx, by, ASCIIChars.Wall, ASCIIColors.Wall);
                }
                else if (isDoor)
                {
                    buffer.SetCell(bx, by, ASCIIChars.DoorOpen, ASCIIColors.Door);
                }
                else
                {
                    buffer.SetCell(bx, by, ASCIIChars.Floor, ASCIIColors.Floor);
                }
            }
        }

        // Add some water with dancing color
        for (int x = 30; x < 40; x++)
        {
            for (int y = 10; y < 20; y++)
            {
                int bx = startX + x;
                int by = startY + y;
                var waterColor = DancingColor.Water(ASCIIColors.Water);
                buffer.SetDancingCell(bx, by, ASCIIChars.Water, waterColor, ASCIIColors.BgDark);
            }
        }

        // Add stairs
        buffer.SetCell(startX + 15, startY + 10, ASCIIChars.StairsDown, ASCIIColors.Stairs);

        // Add an enemy
        buffer.SetCell(startX + 20, startY + 12, ASCIIChars.Drone, ASCIIColors.Enemy);

        // Add an item
        buffer.SetCell(startX + 22, startY + 8, ASCIIChars.ItemModule, ASCIIColors.Rare);
    }

    private void DrawPlayer(ASCIIBuffer buffer)
    {
        // Draw player with pulsing effect
        float pulse = 0.8f + 0.2f * Mathf.Sin(_demoTimer * 3f);
        var playerColor = new Color(
            ASCIIColors.Player.R * pulse,
            ASCIIColors.Player.G * pulse,
            ASCIIColors.Player.B * pulse
        );
        buffer.SetCell(_playerX, _playerY, ASCIIChars.Player, playerColor);
    }

    public override void _ExitTree()
    {
        if (_renderer != null)
        {
            _renderer.OnDraw -= DrawFrame;
        }
    }
}
