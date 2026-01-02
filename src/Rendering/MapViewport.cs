using System;
using System.Text;
using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Handles zoomed rendering of the map area using a SubViewport.
/// The map is rendered to its own buffer at a larger font size,
/// allowing it to be zoomed independently of UI elements.
/// </summary>
public partial class MapViewport : Control
{
    // Zoom settings
    private float _zoom = 1.0f;
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            UpdateViewDimensions();
        }
    }

    public const float MinZoom = 1.0f;
    public const float MaxZoom = 2.0f;
    public const float ZoomStep = 0.25f;

    // Base dimensions at zoom 1.0 (matches ASCIIBuffer map region)
    public int BaseViewWidth { get; private set; } = ASCIIBuffer.MapWidth;
    public int BaseViewHeight { get; private set; } = ASCIIBuffer.MapHeight;

    // Current view dimensions (shrink when zoomed)
    public int ViewWidth { get; private set; }
    public int ViewHeight { get; private set; }

    // Font settings
    private Font? _font;
    private int _baseFontSize;
    private float _charWidth;
    private float _charHeight;

    // Rendering components
    private SubViewportContainer? _container;
    private SubViewport? _viewport;
    private RichTextLabel? _textLabel;
    private ColorRect? _background;

    // Map buffer (smaller than main buffer, just for map content)
    private char[,]? _chars;
    private Color[,]? _colors;

    // Position in parent (where the map region is)
    private Vector2 _mapRegionPosition;
    private Vector2 _mapRegionSize;

    public MapViewport()
    {
        ViewWidth = BaseViewWidth;
        ViewHeight = BaseViewHeight;
    }

    /// <summary>
    /// Initialize the map viewport with font and positioning info.
    /// </summary>
    public void Initialize(Font font, int baseFontSize, float charWidth, float charHeight,
        Vector2 mapRegionPosition, Vector2 mapRegionSize)
    {
        _font = font;
        _baseFontSize = baseFontSize;
        _charWidth = charWidth;
        _charHeight = charHeight;
        _mapRegionPosition = mapRegionPosition;
        _mapRegionSize = mapRegionSize;

        CreateViewport();
        UpdateViewDimensions();
    }

    private void CreateViewport()
    {
        // Container holds the SubViewport and handles scaling
        _container = new SubViewportContainer
        {
            Name = "MapViewportContainer",
            Stretch = true
        };

        // SubViewport renders the map content
        _viewport = new SubViewport
        {
            Name = "MapSubViewport",
            HandleInputLocally = false,
            GuiDisableInput = true,
            TransparentBg = false
        };

        // Background for the viewport
        _background = new ColorRect
        {
            Name = "MapBackground",
            Color = ASCIIColors.BgDark
        };
        _viewport.AddChild(_background);

        // RichTextLabel for map rendering
        _textLabel = new RichTextLabel
        {
            Name = "MapDisplay",
            BbcodeEnabled = true,
            ScrollActive = false,
            SelectionEnabled = false,
            FitContent = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipContents = false
        };

        if (_font != null)
        {
            _textLabel.AddThemeFontOverride("normal_font", _font);
        }

        _textLabel.AddThemeColorOverride("default_color", ASCIIColors.Primary);

        var stylebox = new StyleBoxEmpty();
        _textLabel.AddThemeStyleboxOverride("normal", stylebox);

        _viewport.AddChild(_textLabel);
        _container.AddChild(_viewport);
        AddChild(_container);

        GD.Print("[MapViewport] Created viewport components");
    }

    private void UpdateViewDimensions()
    {
        // When zoomed, we show fewer tiles
        ViewWidth = Mathf.Max(20, (int)(BaseViewWidth / _zoom));
        ViewHeight = Mathf.Max(15, (int)(BaseViewHeight / _zoom));

        // Reallocate buffers if size changed
        if (_chars == null || _chars.GetLength(0) != ViewWidth || _chars.GetLength(1) != ViewHeight)
        {
            _chars = new char[ViewWidth, ViewHeight];
            _colors = new Color[ViewWidth, ViewHeight];
            ClearBuffer();
        }

        // Calculate the font size needed for this zoom level
        // At zoom 1.0, we use base font size
        // At zoom 2.0, characters are 2x bigger (so we need fewer of them)
        int effectiveFontSize = (int)(_baseFontSize * _zoom);

        // Update viewport and container sizes
        if (_viewport != null && _container != null && _textLabel != null)
        {
            // The viewport renders at the zoomed font size
            float viewportWidth = ViewWidth * _charWidth * _zoom;
            float viewportHeight = ViewHeight * _charHeight * _zoom;

            // Clamp to the map region size
            viewportWidth = Mathf.Min(viewportWidth, _mapRegionSize.X);
            viewportHeight = Mathf.Min(viewportHeight, _mapRegionSize.Y);

            _viewport.Size = new Vector2I((int)viewportWidth, (int)viewportHeight);

            _textLabel.AddThemeFontSizeOverride("normal_font_size", effectiveFontSize);
            _textLabel.Position = Vector2.Zero;
            _textLabel.Size = new Vector2(viewportWidth, viewportHeight);

            if (_background != null)
            {
                _background.Size = new Vector2(viewportWidth, viewportHeight);
            }

            // Container fills the map region
            _container.Position = _mapRegionPosition;
            _container.Size = _mapRegionSize;

            GD.Print($"[MapViewport] Zoom: {_zoom:F2}, View: {ViewWidth}x{ViewHeight}, Font: {effectiveFontSize}");
        }
    }

    /// <summary>
    /// Clear the map buffer.
    /// </summary>
    public void ClearBuffer()
    {
        if (_chars == null || _colors == null)
            return;

        for (int y = 0; y < ViewHeight; y++)
        {
            for (int x = 0; x < ViewWidth; x++)
            {
                _chars[x, y] = ' ';
                _colors[x, y] = ASCIIColors.BgDark;
            }
        }
    }

    /// <summary>
    /// Set a cell in the map buffer.
    /// </summary>
    public void SetCell(int x, int y, char character, Color color)
    {
        if (_chars == null || _colors == null)
            return;
        if (x < 0 || x >= ViewWidth || y < 0 || y >= ViewHeight)
            return;

        _chars[x, y] = character;
        _colors[x, y] = color;
    }

    /// <summary>
    /// Get a cell's current contents from the map buffer.
    /// </summary>
    public (char character, Color color)? GetCell(int x, int y)
    {
        if (_chars == null || _colors == null)
            return null;
        if (x < 0 || x >= ViewWidth || y < 0 || y >= ViewHeight)
            return null;

        return (_chars[x, y], _colors[x, y]);
    }

    /// <summary>
    /// Render the buffer to BBCode and update the display.
    /// </summary>
    public void Render()
    {
        if (_textLabel == null || _chars == null || _colors == null)
            return;

        var sb = new StringBuilder(ViewWidth * ViewHeight * 30);

        for (int y = 0; y < ViewHeight; y++)
        {
            for (int x = 0; x < ViewWidth; x++)
            {
                var color = _colors[x, y];
                var character = _chars[x, y];

                string hex = color.ToHtml(false);
                string charStr = character switch
                {
                    '[' => "[lb]",
                    ']' => "[rb]",
                    _ => character.ToString()
                };

                sb.Append($"[color=#{hex}]{charStr}[/color]");
            }
            if (y < ViewHeight - 1)
            {
                sb.AppendLine();
            }
        }

        _textLabel.Text = sb.ToString();
    }

    /// <summary>
    /// Increase zoom level.
    /// </summary>
    public void ZoomIn()
    {
        Zoom = _zoom + ZoomStep;
    }

    /// <summary>
    /// Decrease zoom level.
    /// </summary>
    public void ZoomOut()
    {
        Zoom = _zoom - ZoomStep;
    }

    /// <summary>
    /// Reset zoom to default.
    /// </summary>
    public void ResetZoom()
    {
        Zoom = 1.0f;
    }

    /// <summary>
    /// Check if a position is within the current view bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < ViewWidth && y >= 0 && y < ViewHeight;
    }
}
