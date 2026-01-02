using System;
using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Main ASCII renderer component. Uses a RichTextLabel with BBCode
/// to display the ASCII buffer with colored characters.
///
/// Target resolution: 2560x1440
/// Buffer size: 180x50 characters
/// Auto-calculates font size to fill viewport.
/// </summary>
public partial class ASCIIRenderer : Control
{
    [Export] public string FontPath { get; set; } = "res://assets/fonts/SourceCodePro-Regular.ttf";
    [Export] public int FontSize { get; set; } = 24;
    [Export] public bool AutoSizeFont { get; set; } = true;

    // Target resolution for fullscreen
    public const int TargetWidth = 2560;
    public const int TargetHeight = 1440;

    // Measured font metrics
    private float _charWidth;
    private float _charHeight;

    private ColorRect? _background;
    private RichTextLabel? _textLabel;
    private MapViewport? _mapViewport;
    private ASCIIBuffer _buffer;
    private Font? _font;
    private float _updateTimer = 0f;
    private const float UpdateInterval = 0.05f; // 20 FPS for text updates (dancing colors still smooth)

    // Events for game integration
    public event Action<float>? OnUpdate;
    public event Action? OnDraw;

    public ASCIIBuffer Buffer => _buffer;
    public MapViewport? MapViewport => _mapViewport;

    public ASCIIRenderer()
    {
        _buffer = new ASCIIBuffer();
    }

    public override void _Ready()
    {
        // Load monospace font
        LoadFont();

        // Auto-calculate font size to fill viewport
        if (AutoSizeFont)
        {
            CalculateOptimalFontSize();
        }

        // Measure and log actual font metrics
        MeasureFontMetrics();

        // Create and configure the RichTextLabel
        CreateTextLabel();

        // Connect to viewport size changed signal for resize handling
        GetTree().Root.SizeChanged += OnViewportResized;

        // Initial render
        _buffer.Invalidate();
        Render();
    }

    public override void _ExitTree()
    {
        // Disconnect from viewport resize signal
        if (GetTree()?.Root != null)
        {
            GetTree().Root.SizeChanged -= OnViewportResized;
        }
    }

    private void OnViewportResized()
    {
        // Safely handle viewport resize
        // The fixed-size ASCII buffer design means we can't dynamically resize
        // but we can ensure the render doesn't crash
        try
        {
            if (!IsInstanceValid(this))
                return;

            _buffer?.Invalidate();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ASCIIRenderer] Error during resize: {ex.Message}");
        }
    }

    private void CalculateOptimalFontSize()
    {
        if (_font == null)
            return;

        // Get viewport size (use target resolution as reference)
        float viewportWidth = TargetWidth;
        float viewportHeight = TargetHeight;

        // Binary search for optimal font size
        int minSize = 16;
        int maxSize = 48;
        int bestSize = FontSize;

        while (minSize <= maxSize)
        {
            int testSize = (minSize + maxSize) / 2;

            // Measure character dimensions at this size
            float charWidth = _font.GetStringSize("M", HorizontalAlignment.Left, -1, testSize).X;
            float charHeight = _font.GetHeight(testSize);

            float contentWidth = ASCIIBuffer.Width * charWidth;
            float contentHeight = ASCIIBuffer.Height * charHeight;

            // Check if it fits
            bool fitsWidth = contentWidth <= viewportWidth;
            bool fitsHeight = contentHeight <= viewportHeight;

            if (fitsWidth && fitsHeight)
            {
                bestSize = testSize;
                minSize = testSize + 1; // Try larger
            }
            else
            {
                maxSize = testSize - 1; // Try smaller
            }
        }

        FontSize = bestSize;
        GD.Print($"[ASCIIRenderer] Auto-calculated font size: {FontSize}");
    }

    private void MeasureFontMetrics()
    {
        if (_font == null)
            return;

        _charWidth = _font.GetStringSize("M", HorizontalAlignment.Left, -1, FontSize).X;
        _charHeight = _font.GetHeight(FontSize);

        float contentWidth = ASCIIBuffer.Width * _charWidth;
        float contentHeight = ASCIIBuffer.Height * _charHeight;

        GD.Print($"[ASCIIRenderer] Font metrics at size {FontSize}:");
        GD.Print($"  Character size: {_charWidth:F1}px x {_charHeight:F1}px");
        GD.Print($"  Content size: {contentWidth:F0}px x {contentHeight:F0}px");
        GD.Print($"  Target viewport: {TargetWidth}px x {TargetHeight}px");
        GD.Print($"  Buffer dimensions: {ASCIIBuffer.Width} x {ASCIIBuffer.Height} characters");

        float usedWidth = contentWidth / TargetWidth * 100;
        float usedHeight = contentHeight / TargetHeight * 100;
        GD.Print($"  Viewport usage: {usedWidth:F1}% x {usedHeight:F1}%");
    }

    private void LoadFont()
    {
        // Try to load custom font first
        if (ResourceLoader.Exists(FontPath))
        {
            _font = GD.Load<Font>(FontPath);
            GD.Print($"[ASCIIRenderer] Loaded font: {FontPath}");
            return;
        }

        // Try fallback font paths
        var fallbackPaths = new[]
        {
            "res://assets/fonts/SourceCodePro-Regular.ttf",
            "res://assets/fonts/SourceCodePro.ttf",
            "res://assets/fonts/JetBrainsMono-Regular.ttf",
            "res://assets/fonts/monospace.ttf"
        };

        foreach (var path in fallbackPaths)
        {
            if (ResourceLoader.Exists(path))
            {
                _font = GD.Load<Font>(path);
                FontPath = path;
                GD.Print($"[ASCIIRenderer] Using fallback font: {path}");
                return;
            }
        }

        // Use Godot 4's SystemFont to load a system monospace font
        var systemFont = new SystemFont();
        systemFont.FontNames = new string[]
        {
            "Consolas",           // Windows
            "Courier New",        // Cross-platform
            "SF Mono",            // macOS
            "DejaVu Sans Mono",   // Linux
            "Liberation Mono",    // Linux
            "monospace"           // Generic fallback
        };
        systemFont.Antialiasing = TextServer.FontAntialiasing.Lcd;
        systemFont.Hinting = TextServer.Hinting.Normal;
        _font = systemFont;
        GD.Print("[ASCIIRenderer] Using system monospace font");
    }

    private void CreateTextLabel()
    {
        // Create full-screen background first
        _background = new ColorRect
        {
            Name = "Background",
            Color = ASCIIColors.BgDark
        };
        _background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        _textLabel = new RichTextLabel
        {
            Name = "ASCIIDisplay",
            BbcodeEnabled = true,
            ScrollActive = false,
            SelectionEnabled = false,
            FitContent = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipContents = false  // Don't clip - let us see if content overflows
        };

        // Apply font settings
        if (_font != null)
        {
            _textLabel.AddThemeFontOverride("normal_font", _font);
            _textLabel.AddThemeFontOverride("bold_font", _font);
            _textLabel.AddThemeFontOverride("italics_font", _font);
            _textLabel.AddThemeFontOverride("mono_font", _font);
        }

        _textLabel.AddThemeFontSizeOverride("normal_font_size", FontSize);
        _textLabel.AddThemeFontSizeOverride("bold_font_size", FontSize);
        _textLabel.AddThemeFontSizeOverride("italics_font_size", FontSize);
        _textLabel.AddThemeFontSizeOverride("mono_font_size", FontSize);

        // Set default text color
        _textLabel.AddThemeColorOverride("default_color", ASCIIColors.Primary);

        // Transparent background on label (background ColorRect handles it)
        var stylebox = new StyleBoxEmpty();
        _textLabel.AddThemeStyleboxOverride("normal", stylebox);

        // Calculate exact content size
        float contentWidth = ASCIIBuffer.Width * _charWidth;
        float contentHeight = ASCIIBuffer.Height * _charHeight;

        // Center the content in the viewport
        float offsetX = Math.Max(0, (TargetWidth - contentWidth) / 2);
        float offsetY = Math.Max(0, (TargetHeight - contentHeight) / 2);

        _textLabel.Position = new Vector2(offsetX, offsetY);
        _textLabel.Size = new Vector2(contentWidth, contentHeight);

        AddChild(_textLabel);

        GD.Print($"[ASCIIRenderer] Created RichTextLabel with font size {FontSize}");
        GD.Print($"[ASCIIRenderer] Content size: {contentWidth:F0}x{contentHeight:F0}px");
        GD.Print($"[ASCIIRenderer] Position: ({offsetX:F0}, {offsetY:F0})");
        GD.Print($"[ASCIIRenderer] Unused space: {offsetX * 2:F0}px horizontal, {offsetY * 2:F0}px vertical");

        // Create the map viewport for zoomed map rendering
        CreateMapViewport(offsetX, offsetY);
    }

    private void CreateMapViewport(float contentOffsetX, float contentOffsetY)
    {
        _mapViewport = new MapViewport
        {
            Name = "MapViewport"
        };

        // Calculate the map region position and size in pixels
        float mapRegionX = contentOffsetX + ASCIIBuffer.MapStartX * _charWidth;
        float mapRegionY = contentOffsetY + ASCIIBuffer.MapStartY * _charHeight;
        float mapRegionWidth = ASCIIBuffer.MapWidth * _charWidth;
        float mapRegionHeight = ASCIIBuffer.MapHeight * _charHeight;

        _mapViewport.Initialize(
            _font!,
            FontSize,
            _charWidth,
            _charHeight,
            new Vector2(mapRegionX, mapRegionY),
            new Vector2(mapRegionWidth, mapRegionHeight)
        );

        AddChild(_mapViewport);

        GD.Print($"[ASCIIRenderer] Created MapViewport at ({mapRegionX:F0}, {mapRegionY:F0})");
        GD.Print($"[ASCIIRenderer] Map region size: {mapRegionWidth:F0}x{mapRegionHeight:F0}px");
    }

    public override void _Process(double delta)
    {
        // Safety check - don't process if we're being destroyed
        if (!IsInstanceValid(this) || _buffer == null)
            return;

        float dt = (float)delta;

        // Update dancing colors in buffer
        _buffer.Update(dt);

        // Invoke update event for game logic to modify buffer
        OnUpdate?.Invoke(dt);

        // Rate-limited text rendering (BBCode generation is expensive)
        _updateTimer += dt;
        if (_updateTimer >= UpdateInterval || _buffer.NeedsFullRedraw)
        {
            _updateTimer = 0f;
            OnDraw?.Invoke();
            Render();
        }
    }

    /// <summary>
    /// Render the buffer to the RichTextLabel.
    /// </summary>
    public void Render()
    {
        if (_textLabel == null)
            return;

        _textLabel.Text = _buffer.ToBBCode();
        _buffer.MarkRedrawn();
    }

    /// <summary>
    /// Force an immediate full redraw including game content.
    /// </summary>
    public void ForceRender()
    {
        _buffer.Invalidate();
        OnDraw?.Invoke();  // Trigger DrawFrame to redraw game content
        Render();
    }

    /// <summary>
    /// Clear the buffer.
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Get the calculated size for the display based on font metrics.
    /// </summary>
    public Vector2 GetDisplaySize()
    {
        if (_font == null)
            return new Vector2(ASCIIBuffer.Width * 10, ASCIIBuffer.Height * 16);

        // Estimate character size (monospace font)
        float charWidth = _font.GetStringSize("M", HorizontalAlignment.Left, -1, FontSize).X;
        float charHeight = _font.GetHeight(FontSize);

        return new Vector2(
            ASCIIBuffer.Width * charWidth,
            ASCIIBuffer.Height * charHeight
        );
    }

    /// <summary>
    /// Set the font size and update display.
    /// </summary>
    public void SetFontSize(int size)
    {
        FontSize = size;
        if (_textLabel != null)
        {
            _textLabel.AddThemeFontSizeOverride("normal_font_size", size);
            _textLabel.AddThemeFontSizeOverride("bold_font_size", size);
            _textLabel.AddThemeFontSizeOverride("italics_font_size", size);
            _textLabel.AddThemeFontSizeOverride("mono_font_size", size);
        }
        ForceRender();
    }

    /// <summary>
    /// Access buffer for direct manipulation.
    /// </summary>
    public ASCIIBuffer GetBuffer()
    {
        return _buffer;
    }

    // Convenience methods that delegate to buffer

    public void SetCell(int x, int y, char character, Color foreground)
        => _buffer.SetCell(x, y, character, foreground);

    public void SetCell(int x, int y, char character, Color foreground, Color background)
        => _buffer.SetCell(x, y, character, foreground, background);

    public void WriteString(int x, int y, string text, Color foreground)
        => _buffer.WriteString(x, y, text, foreground);

    public void WriteString(int x, int y, string text, Color foreground, Color background)
        => _buffer.WriteString(x, y, text, foreground, background);

    public void WriteCentered(int y, string text, Color foreground)
        => _buffer.WriteCentered(y, text, foreground);

    public void DrawBox(int x, int y, int width, int height, Color color, bool doubleLines = false)
        => _buffer.DrawBox(x, y, width, height, color, doubleLines);

    public void DrawHorizontalLine(int x, int y, int length, Color color)
        => _buffer.DrawHorizontalLine(x, y, length, color);

    public void DrawVerticalLine(int x, int y, int length, Color color)
        => _buffer.DrawVerticalLine(x, y, length, color);

    // Map zoom controls

    /// <summary>
    /// Get the current map zoom level.
    /// </summary>
    public float GetMapZoom() => _mapViewport?.Zoom ?? 1.0f;

    /// <summary>
    /// Zoom in on the map.
    /// </summary>
    public void ZoomMapIn()
    {
        _mapViewport?.ZoomIn();
        GD.Print($"[ASCIIRenderer] Map zoom: {GetMapZoom():F2}x");
    }

    /// <summary>
    /// Zoom out on the map.
    /// </summary>
    public void ZoomMapOut()
    {
        _mapViewport?.ZoomOut();
        GD.Print($"[ASCIIRenderer] Map zoom: {GetMapZoom():F2}x");
    }

    /// <summary>
    /// Reset map zoom to default.
    /// </summary>
    public void ResetMapZoom()
    {
        _mapViewport?.ResetZoom();
        GD.Print($"[ASCIIRenderer] Map zoom reset to {GetMapZoom():F2}x");
    }

    /// <summary>
    /// Set map zoom directly.
    /// </summary>
    public void SetMapZoom(float zoom)
    {
        if (_mapViewport != null)
        {
            _mapViewport.Zoom = zoom;
            GD.Print($"[ASCIIRenderer] Map zoom set to {GetMapZoom():F2}x");
        }
    }
}
