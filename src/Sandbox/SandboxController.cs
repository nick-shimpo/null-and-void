using System.Collections.Generic;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Sandbox;

/// <summary>
/// Controller for the terrain sandbox demonstration mode.
/// Allows switching between different environment examples to compare
/// terrain rendering approaches.
/// </summary>
public partial class SandboxController : Control
{
    private ASCIIRenderer? _renderer;
    private List<SandboxEnvironment> _environments = new();
    private int _currentIndex = 0;
    private bool _animationEnabled = true;
    private bool _showInfo = true;

    // Layout constants
    private const int MapOffsetX = 2;
    private const int MapOffsetY = 4;
    private const int InfoPanelY = 1;

    public override void _Ready()
    {
        // Create the ASCII renderer
        _renderer = new ASCIIRenderer
        {
            FontSize = 24
        };
        _renderer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_renderer);

        // Register environments
        RegisterEnvironments();

        // Initialize first environment
        if (_environments.Count > 0)
        {
            _environments[_currentIndex].Initialize();
        }

        // Hook up rendering
        _renderer.OnDraw += DrawFrame;

        GD.Print($"[SandboxController] Initialized with {_environments.Count} environments");
    }

    private void RegisterEnvironments()
    {
        // Add all sandbox environments
        _environments.Add(new ForestClearing());
        _environments.Add(new RiverValley());
        _environments.Add(new MountainPass());
        _environments.Add(new RuinedSettlement());
        _environments.Add(new CoastalWetlands());
        _environments.Add(new TechRuins());
    }

    public override void _ExitTree()
    {
        if (_renderer != null)
            _renderer.OnDraw -= DrawFrame;
    }

    public override void _Process(double delta)
    {
        if (_animationEnabled && _environments.Count > 0)
        {
            _environments[_currentIndex].Update((float)delta);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Left:
                case Key.A:
                    PreviousEnvironment();
                    break;

                case Key.Right:
                case Key.D:
                    NextEnvironment();
                    break;

                case Key.R:
                    ResetCurrentEnvironment();
                    break;

                case Key.T:
                    ToggleAnimation();
                    break;

                case Key.H:
                case Key.Slash when keyEvent.ShiftPressed:
                    ToggleInfo();
                    break;

                case Key.Escape:
                    ReturnToMainMenu();
                    break;

                case Key.F:
                    // Switch to destruction sandbox
                    GetTree().ChangeSceneToFile("res://scenes/DestructionSandbox.tscn");
                    break;

                case Key.Key1:
                case Key.Key2:
                case Key.Key3:
                case Key.Key4:
                case Key.Key5:
                case Key.Key6:
                    int index = (int)keyEvent.Keycode - (int)Key.Key1;
                    if (index < _environments.Count)
                    {
                        SwitchToEnvironment(index);
                    }
                    break;
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private void NextEnvironment()
    {
        if (_environments.Count == 0)
            return;
        _currentIndex = (_currentIndex + 1) % _environments.Count;
        _environments[_currentIndex].Initialize();
        _renderer?.Buffer.Invalidate();
        GD.Print($"[Sandbox] Switched to: {_environments[_currentIndex].Name}");
    }

    private void PreviousEnvironment()
    {
        if (_environments.Count == 0)
            return;
        _currentIndex = (_currentIndex - 1 + _environments.Count) % _environments.Count;
        _environments[_currentIndex].Initialize();
        _renderer?.Buffer.Invalidate();
        GD.Print($"[Sandbox] Switched to: {_environments[_currentIndex].Name}");
    }

    private void SwitchToEnvironment(int index)
    {
        if (index >= 0 && index < _environments.Count)
        {
            _currentIndex = index;
            _environments[_currentIndex].Initialize();
            _renderer?.Buffer.Invalidate();
            GD.Print($"[Sandbox] Switched to: {_environments[_currentIndex].Name}");
        }
    }

    private void ResetCurrentEnvironment()
    {
        if (_environments.Count > 0)
        {
            _environments[_currentIndex].Reset();
            _renderer?.Buffer.Invalidate();
            GD.Print("[Sandbox] Environment reset");
        }
    }

    private void ToggleAnimation()
    {
        _animationEnabled = !_animationEnabled;
        _renderer?.Buffer.Invalidate();
        GD.Print($"[Sandbox] Animation: {(_animationEnabled ? "ON" : "OFF")}");
    }

    private void ToggleInfo()
    {
        _showInfo = !_showInfo;
        _renderer?.Buffer.Invalidate();
    }

    private void ReturnToMainMenu()
    {
        GetTree().ChangeSceneToFile("res://scenes/ASCIIMain.tscn");
    }

    private void DrawFrame()
    {
        if (_renderer == null || _environments.Count == 0)
            return;

        var buffer = _renderer.Buffer;
        buffer.Clear();

        var env = _environments[_currentIndex];

        // Draw header
        DrawHeader(buffer, env);

        // Draw environment
        env.Render(buffer, MapOffsetX, MapOffsetY);

        // Draw border around environment
        DrawEnvironmentBorder(buffer, env);

        // Draw info panel if enabled
        if (_showInfo)
        {
            DrawInfoPanel(buffer, env);
        }

        // Draw controls footer
        DrawFooter(buffer);
    }

    private void DrawHeader(ASCIIBuffer buffer, SandboxEnvironment env)
    {
        // Title bar
        string title = $" TERRAIN SANDBOX: {env.Name.ToUpper()} ";
        int titleX = (ASCIIBuffer.Width - title.Length) / 2;

        buffer.DrawHorizontalLine(0, 0, ASCIIBuffer.Width, ASCIIColors.Border);
        buffer.WriteString(titleX, 0, title, ASCIIColors.PrimaryBright, ASCIIColors.BgPanel);

        // Environment selector
        string selector = "";
        for (int i = 0; i < _environments.Count; i++)
        {
            if (i == _currentIndex)
                selector += $"[{i + 1}] ";
            else
                selector += $" {i + 1}  ";
        }
        buffer.WriteString(2, 2, selector, ASCIIColors.TextSecondary);

        // Animation indicator
        string animStatus = _animationEnabled ? "[T] Anim: ON " : "[T] Anim: OFF";
        buffer.WriteString(ASCIIBuffer.Width - animStatus.Length - 2, 2, animStatus,
            _animationEnabled ? ASCIIColors.AlertSuccess : ASCIIColors.TextMuted);
    }

    private void DrawEnvironmentBorder(ASCIIBuffer buffer, SandboxEnvironment env)
    {
        int borderX = MapOffsetX - 1;
        int borderY = MapOffsetY - 1;
        int borderW = env.Width + 2;
        int borderH = env.Height + 2;

        buffer.DrawBox(borderX, borderY, borderW, borderH, ASCIIColors.Border);
    }

    private void DrawInfoPanel(ASCIIBuffer buffer, SandboxEnvironment env)
    {
        int panelX = MapOffsetX + env.Width + 4;
        int panelY = MapOffsetY;
        int panelWidth = ASCIIBuffer.Width - panelX - 2;

        if (panelWidth < 20)
            return; // Not enough space

        // Panel title
        buffer.WriteString(panelX, panelY, "ENVIRONMENT INFO", ASCIIColors.PrimaryBright);
        buffer.DrawHorizontalLine(panelX, panelY + 1, panelWidth, ASCIIColors.Border);

        // Description
        int y = panelY + 3;
        buffer.WriteString(panelX, y, "Description:", ASCIIColors.TextSecondary);
        y++;

        // Word wrap description
        string desc = env.Description;
        int lineWidth = panelWidth - 2;
        while (desc.Length > 0 && y < panelY + 12)
        {
            int len = desc.Length > lineWidth ? lineWidth : desc.Length;
            if (len < desc.Length)
            {
                int lastSpace = desc.LastIndexOf(' ', len);
                if (lastSpace > 0)
                    len = lastSpace;
            }
            buffer.WriteString(panelX, y, desc[..len].Trim(), ASCIIColors.TextMuted);
            desc = desc[len..].Trim();
            y++;
        }

        // Dimensions
        y += 2;
        buffer.WriteString(panelX, y, $"Size: {env.Width}x{env.Height}", ASCIIColors.TextSecondary);

        // Legend section
        y += 3;
        buffer.WriteString(panelX, y, "LEGEND", ASCIIColors.PrimaryBright);
        buffer.DrawHorizontalLine(panelX, y + 1, panelWidth, ASCIIColors.Border);
        y += 2;

        // Show common symbols
        DrawLegendItem(buffer, panelX, y++, '.', "Floor/Grass", ASCIIColors.Grass);
        DrawLegendItem(buffer, panelX, y++, ASCIIChars.TreeDeciduous, "Deciduous Tree", ASCIIColors.TreeCanopy);
        DrawLegendItem(buffer, panelX, y++, ASCIIChars.TreeEvergreen, "Evergreen Tree", ASCIIColors.TreePine);
        DrawLegendItem(buffer, panelX, y++, '~', "Water", ASCIIColors.WaterFg);
        DrawLegendItem(buffer, panelX, y++, '^', "Hill", ASCIIColors.HillBrown);
        DrawLegendItem(buffer, panelX, y++, ASCIIChars.Mountain, "Mountain", ASCIIColors.MountainSlope);
        DrawLegendItem(buffer, panelX, y++, '#', "Wall", ASCIIColors.Wall);
        DrawLegendItem(buffer, panelX, y++, ASCIIChars.Rubble, "Rubble", ASCIIColors.Rubble);
    }

    private void DrawLegendItem(ASCIIBuffer buffer, int x, int y, char symbol, string label, Color color)
    {
        buffer.SetCell(x, y, symbol, color);
        buffer.WriteString(x + 2, y, label, ASCIIColors.TextMuted);
    }

    private void DrawFooter(ASCIIBuffer buffer)
    {
        int y = ASCIIBuffer.Height - 2;

        buffer.DrawHorizontalLine(0, y - 1, ASCIIBuffer.Width, ASCIIColors.Border);

        string controls = "[A/D] Prev/Next  [1-6] Select  [R] Reset  [T] Anim  [H] Info  [F] Destruction  [ESC] Exit";
        int controlsX = (ASCIIBuffer.Width - controls.Length) / 2;
        buffer.WriteString(controlsX, y, controls, ASCIIColors.TextSecondary);
    }
}
