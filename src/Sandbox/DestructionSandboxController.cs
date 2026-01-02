using System.Collections.Generic;
using Godot;
using NullAndVoid.Destruction;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Controller for the destruction and fire testing sandbox.
/// Allows testing explosions, fire spread, and terrain destruction.
/// </summary>
public partial class DestructionSandboxController : Control
{
    private ASCIIRenderer? _renderer;
    private DestructionSandbox? _sandbox;

    // Cursor for targeting
    private Vector2I _cursorPos = new(30, 17);

    // Selected weapon
    private int _selectedWeapon = 0;
    private readonly ExplosionData[] _weapons = {
        ExplosionData.SmallBlast,
        ExplosionData.Grenade,
        ExplosionData.Incendiary,
        ExplosionData.Plasma,
        ExplosionData.Missile
    };

    // Simulation state
    private bool _fireSimEnabled = true;
    private bool _paused = false;
    private float _turnTimer = 0f;
    private float _turnInterval = 0.5f; // Seconds between turns
    private bool _showDebug = false;

    // Layout constants
    private const int MapOffsetX = 2;
    private const int MapOffsetY = 4;
    private const int InfoPanelX = 65;

    public override void _Ready()
    {
        // Create the ASCII renderer
        _renderer = new ASCIIRenderer
        {
            FontSize = 24
        };
        _renderer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_renderer);

        // Create sandbox
        _sandbox = new DestructionSandbox();
        _sandbox.Generate();

        // Hook up rendering
        _renderer.OnDraw += DrawFrame;

        GD.Print("[DestructionSandbox] Initialized");
    }

    public override void _ExitTree()
    {
        if (_renderer != null)
            _renderer.OnDraw -= DrawFrame;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Update animations
        _sandbox?.Update(dt);

        // Process fire simulation on timer
        if (_fireSimEnabled && !_paused)
        {
            _turnTimer += dt;
            if (_turnTimer >= _turnInterval)
            {
                _turnTimer = 0;
                _sandbox?.ProcessFireTurn();

                // Process chain reactions
                while (_sandbox?.ExplosionSys.ProcessChainReactions() ?? false)
                {
                    // Keep processing until done
                }
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                // Cursor movement
                case Key.Up:
                case Key.W:
                    MoveCursor(0, -1);
                    break;
                case Key.Down:
                case Key.S:
                    MoveCursor(0, 1);
                    break;
                case Key.Left:
                case Key.A:
                    MoveCursor(-1, 0);
                    break;
                case Key.Right:
                case Key.D:
                    MoveCursor(1, 0);
                    break;

                // Fire weapon
                case Key.Enter:
                case Key.Space:
                    FireWeapon();
                    break;

                // Ignite (F key)
                case Key.I:
                    IgniteAtCursor();
                    break;

                // Weapon selection
                case Key.Key1:
                    _selectedWeapon = 0;
                    break;
                case Key.Key2:
                    _selectedWeapon = 1;
                    break;
                case Key.Key3:
                    _selectedWeapon = 2;
                    break;
                case Key.Key4:
                    _selectedWeapon = 3;
                    break;
                case Key.Key5:
                    _selectedWeapon = 4;
                    break;

                // Toggle fire simulation
                case Key.F:
                    _fireSimEnabled = !_fireSimEnabled;
                    break;

                // Pause/Resume
                case Key.P:
                    _paused = !_paused;
                    break;

                // Speed controls
                case Key.Bracketleft:
                    _turnInterval = Mathf.Min(_turnInterval + 0.1f, 2.0f);
                    break;
                case Key.Bracketright:
                    _turnInterval = Mathf.Max(_turnInterval - 0.1f, 0.1f);
                    break;

                // Debug mode
                case Key.Tab:
                    _showDebug = !_showDebug;
                    break;

                // Reset
                case Key.R:
                    _sandbox?.Reset();
                    break;

                // Exit
                case Key.Escape:
                    GetTree().ChangeSceneToFile("res://scenes/Sandbox.tscn");
                    break;
            }

            GetViewport().SetInputAsHandled();
            _renderer?.Buffer.Invalidate();
        }
    }

    private void MoveCursor(int dx, int dy)
    {
        _cursorPos.X = Mathf.Clamp(_cursorPos.X + dx, 0, DestructionSandbox.Width - 1);
        _cursorPos.Y = Mathf.Clamp(_cursorPos.Y + dy, 0, DestructionSandbox.Height - 1);
    }

    private void FireWeapon()
    {
        if (_sandbox == null)
            return;

        var weapon = _weapons[_selectedWeapon];
        _sandbox.TriggerExplosion(_cursorPos.X, _cursorPos.Y, weapon);

        GD.Print($"[DestructionSandbox] Fired {weapon.Name} at {_cursorPos}");
    }

    private void IgniteAtCursor()
    {
        _sandbox?.IgniteTile(_cursorPos.X, _cursorPos.Y);
        GD.Print($"[DestructionSandbox] Ignited tile at {_cursorPos}");
    }

    private void DrawFrame()
    {
        if (_renderer == null || _sandbox == null)
            return;

        var buffer = _renderer.Buffer;
        buffer.Clear();

        // Draw header
        DrawHeader(buffer);

        // Draw sandbox tiles
        DrawSandboxTiles(buffer);

        // Draw explosion effects
        DrawExplosionEffects(buffer);

        // Draw cursor
        DrawCursor(buffer);

        // Draw info panel
        DrawInfoPanel(buffer);

        // Draw controls footer
        DrawFooter(buffer);
    }

    private void DrawHeader(ASCIIBuffer buffer)
    {
        string title = " DESTRUCTION SANDBOX ";
        int titleX = (ASCIIBuffer.Width - title.Length) / 2;

        buffer.DrawHorizontalLine(0, 0, ASCIIBuffer.Width, ASCIIColors.Border);
        buffer.WriteString(titleX, 0, title, ASCIIColors.AlertDanger, ASCIIColors.BgPanel);

        // Weapon selector
        string weaponStr = "Weapon: ";
        buffer.WriteString(2, 2, weaponStr, ASCIIColors.TextSecondary);

        int wx = 2 + weaponStr.Length;
        for (int i = 0; i < _weapons.Length; i++)
        {
            Color color = i == _selectedWeapon ? ASCIIColors.AlertWarning : ASCIIColors.TextMuted;
            string label = $"[{i + 1}]{_weapons[i].Name} ";
            buffer.WriteString(wx, 2, label, color);
            wx += label.Length;
        }

        // Status indicators
        string fireStatus = _fireSimEnabled ? "Fire: ON" : "Fire: OFF";
        string pauseStatus = _paused ? "[PAUSED]" : "";
        string speedStatus = $"Speed: {_turnInterval:F1}s";

        buffer.WriteString(ASCIIBuffer.Width - 40, 2, fireStatus,
            _fireSimEnabled ? ASCIIColors.AlertDanger : ASCIIColors.TextMuted);
        buffer.WriteString(ASCIIBuffer.Width - 28, 2, speedStatus, ASCIIColors.TextSecondary);
        buffer.WriteString(ASCIIBuffer.Width - 15, 2, pauseStatus, ASCIIColors.AlertWarning);
    }

    private void DrawSandboxTiles(ASCIIBuffer buffer)
    {
        if (_sandbox == null)
            return;

        for (int y = 0; y < DestructionSandbox.Height; y++)
        {
            for (int x = 0; x < DestructionSandbox.Width; x++)
            {
                int bufX = MapOffsetX + x;
                int bufY = MapOffsetY + y;

                if (bufX >= ASCIIBuffer.Width || bufY >= ASCIIBuffer.Height)
                    continue;

                var tile = _sandbox.Tiles[x, y];

                char displayChar = tile.GetCurrentChar();
                Color fg = tile.GetCurrentForeground();
                Color bg = tile.GetCurrentBackground();

                // Debug mode shows HP
                if (_showDebug && tile.Material.MaxHitPoints > 0)
                {
                    float hpPercent = (float)tile.CurrentHP / tile.Material.MaxHitPoints;
                    if (hpPercent < 1.0f)
                    {
                        // Tint based on damage
                        fg = fg.Lerp(ASCIIColors.AlertDanger, 1.0f - hpPercent);
                    }
                }

                buffer.SetCell(bufX, bufY, displayChar, fg, bg);
            }
        }

        // Draw border around map
        buffer.DrawBox(MapOffsetX - 1, MapOffsetY - 1,
            DestructionSandbox.Width + 2, DestructionSandbox.Height + 2,
            ASCIIColors.Border);
    }

    private void DrawExplosionEffects(ASCIIBuffer buffer)
    {
        if (_sandbox == null)
            return;

        foreach (var visual in _sandbox.ExplosionSys.ActiveVisuals)
        {
            float progress = visual.Progress;

            // Expanding ring effect
            int currentRadius = (int)(visual.Radius * progress);

            for (int dy = -currentRadius; dy <= currentRadius; dy++)
            {
                for (int dx = -currentRadius; dx <= currentRadius; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Draw ring
                    if (dist >= currentRadius - 1 && dist <= currentRadius)
                    {
                        int x = visual.Center.X + dx;
                        int y = visual.Center.Y + dy;

                        if (x >= 0 && x < DestructionSandbox.Width &&
                            y >= 0 && y < DestructionSandbox.Height)
                        {
                            int bufX = MapOffsetX + x;
                            int bufY = MapOffsetY + y;

                            char explosionChar = progress < 0.3f ? '*' : progress < 0.6f ? '○' : '·';
                            Color color = visual.Color.Lerp(ASCIIColors.BgDark, progress);

                            buffer.SetCell(bufX, bufY, explosionChar, color);
                        }
                    }
                }
            }

            // Center flash
            if (progress < 0.3f)
            {
                int bufX = MapOffsetX + visual.Center.X;
                int bufY = MapOffsetY + visual.Center.Y;
                buffer.SetCell(bufX, bufY, '●', visual.Color);
            }
        }
    }

    private void DrawCursor(ASCIIBuffer buffer)
    {
        int bufX = MapOffsetX + _cursorPos.X;
        int bufY = MapOffsetY + _cursorPos.Y;

        // Draw targeting reticle around cursor
        var weapon = _weapons[_selectedWeapon];

        // Show blast radius
        for (int dy = -weapon.Radius; dy <= weapon.Radius; dy++)
        {
            for (int dx = -weapon.Radius; dx <= weapon.Radius; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= weapon.Radius && dist >= weapon.Radius - 0.5f)
                {
                    int rx = bufX + dx;
                    int ry = bufY + dy;

                    if (rx >= MapOffsetX && rx < MapOffsetX + DestructionSandbox.Width &&
                        ry >= MapOffsetY && ry < MapOffsetY + DestructionSandbox.Height)
                    {
                        // Semi-transparent radius indicator
                        var existingCell = buffer.GetCell(rx, ry);
                        Color radiusColor = ASCIIColors.AlertDanger.Lerp(existingCell.GetEffectiveForeground(), 0.5f);
                        buffer.SetCell(rx, ry, existingCell.Character, radiusColor);
                    }
                }
            }
        }

        // Cursor itself
        buffer.SetCell(bufX, bufY, 'X', ASCIIColors.AlertWarning);
    }

    private void DrawInfoPanel(ASCIIBuffer buffer)
    {
        if (_sandbox == null)
            return;

        int y = MapOffsetY;

        // Panel header
        buffer.WriteString(InfoPanelX, y, "TILE INFO", ASCIIColors.PrimaryBright);
        buffer.DrawHorizontalLine(InfoPanelX, y + 1, 25, ASCIIColors.Border);
        y += 3;

        // Current tile info
        var tile = _sandbox.Tiles[_cursorPos.X, _cursorPos.Y];

        buffer.WriteString(InfoPanelX, y++, $"Position: {_cursorPos.X},{_cursorPos.Y}", ASCIIColors.TextSecondary);
        buffer.WriteString(InfoPanelX, y++, $"Material: {tile.Material.Name}", ASCIIColors.TextSecondary);
        buffer.WriteString(InfoPanelX, y++, $"HP: {tile.CurrentHP}/{tile.Material.MaxHitPoints}", ASCIIColors.TextSecondary);
        buffer.WriteString(InfoPanelX, y++, $"State: {tile.State}", ASCIIColors.TextSecondary);
        buffer.WriteString(InfoPanelX, y++, $"Flammable: {tile.Material.Flammability:P0}", ASCIIColors.TextSecondary);

        if (tile.Fire.IsActive)
        {
            buffer.WriteString(InfoPanelX, y++, $"Fire: {tile.Fire.Intensity}", ASCIIColors.AlertDanger);
        }

        y += 2;

        // Weapon info
        buffer.WriteString(InfoPanelX, y, "WEAPON", ASCIIColors.PrimaryBright);
        buffer.DrawHorizontalLine(InfoPanelX, y + 1, 25, ASCIIColors.Border);
        y += 3;

        var weapon = _weapons[_selectedWeapon];
        buffer.WriteString(InfoPanelX, y++, $"Name: {weapon.Name}", ASCIIColors.AlertWarning);
        buffer.WriteString(InfoPanelX, y++, $"Damage: {weapon.BaseDamage}", ASCIIColors.TextSecondary);
        buffer.WriteString(InfoPanelX, y++, $"Radius: {weapon.Radius}", ASCIIColors.TextSecondary);
        buffer.WriteString(InfoPanelX, y++, $"Causes Fire: {weapon.CausesFire}", ASCIIColors.TextSecondary);

        y += 2;

        // Stats
        buffer.WriteString(InfoPanelX, y, "STATS", ASCIIColors.PrimaryBright);
        buffer.DrawHorizontalLine(InfoPanelX, y + 1, 25, ASCIIColors.Border);
        y += 3;

        buffer.WriteString(InfoPanelX, y++, $"Active Fires: {_sandbox.FireSim.ActiveFireCount}", ASCIIColors.AlertDanger);
        buffer.WriteString(InfoPanelX, y++, $"Explosions: {_sandbox.ExplosionSys.ActiveVisuals.Count}", ASCIIColors.AlertWarning);
    }

    private void DrawFooter(ASCIIBuffer buffer)
    {
        int y = ASCIIBuffer.Height - 2;

        buffer.DrawHorizontalLine(0, y - 1, ASCIIBuffer.Width, ASCIIColors.Border);

        string controls = "[WASD] Move   [Enter/Space] Fire   [I] Ignite   [F] Fire Sim   [P] Pause   [R] Reset   [Tab] Debug   [ESC] Exit";
        int controlsX = (ASCIIBuffer.Width - controls.Length) / 2;
        buffer.WriteString(controlsX, y, controls, ASCIIColors.TextSecondary);
    }
}
