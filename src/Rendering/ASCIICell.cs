using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Represents a single cell in the ASCII buffer.
/// Contains character, foreground/background colors, and rendering flags.
/// </summary>
public struct ASCIICell
{
    public char Character;
    public DancingColor Foreground;
    public DancingColor Background;
    public bool NeedsUpdate;      // Dirty flag for optimization
    public bool IsVisible;        // Currently in FOV
    public bool WasSeen;          // Has been seen before (memory rendering)
    public float LightLevel;      // Lighting multiplier (0-1)

    /// <summary>
    /// Create an empty/default cell.
    /// </summary>
    public static ASCIICell Empty()
    {
        return new ASCIICell
        {
            Character = ' ',
            Foreground = DancingColor.Static(ASCIIColors.TextMuted),
            Background = DancingColor.Static(ASCIIColors.BgDark),
            NeedsUpdate = true,
            IsVisible = false,
            WasSeen = false,
            LightLevel = 1.0f
        };
    }

    /// <summary>
    /// Create a cell with static colors.
    /// </summary>
    public static ASCIICell Create(char character, Color foreground, Color background)
    {
        return new ASCIICell
        {
            Character = character,
            Foreground = DancingColor.Static(foreground),
            Background = DancingColor.Static(background),
            NeedsUpdate = true,
            IsVisible = true,
            WasSeen = false,
            LightLevel = 1.0f
        };
    }

    /// <summary>
    /// Create a cell with dancing foreground color.
    /// </summary>
    public static ASCIICell CreateDancing(char character, DancingColor foreground, Color background)
    {
        return new ASCIICell
        {
            Character = character,
            Foreground = foreground,
            Background = DancingColor.Static(background),
            NeedsUpdate = true,
            IsVisible = true,
            WasSeen = false,
            LightLevel = 1.0f
        };
    }

    /// <summary>
    /// Create a cell with both dancing colors.
    /// </summary>
    public static ASCIICell CreateFullDancing(char character, DancingColor foreground, DancingColor background)
    {
        return new ASCIICell
        {
            Character = character,
            Foreground = foreground,
            Background = background,
            NeedsUpdate = true,
            IsVisible = true,
            WasSeen = false,
            LightLevel = 1.0f
        };
    }

    /// <summary>
    /// Update dancing colors.
    /// </summary>
    public void Update(float delta)
    {
        Foreground.Update(delta);
        Background.Update(delta);
    }

    /// <summary>
    /// Set visibility state.
    /// </summary>
    public void SetVisibility(bool visible, bool wasSeen)
    {
        IsVisible = visible;
        WasSeen = wasSeen;
        NeedsUpdate = true;
    }

    /// <summary>
    /// Get the effective foreground color considering visibility and lighting.
    /// </summary>
    public readonly Color GetEffectiveForeground()
    {
        if (IsVisible)
        {
            return Foreground.WithLighting(LightLevel);
        }
        else if (WasSeen)
        {
            // Memory rendering - significantly dimmed
            return Foreground.WithLighting(0.3f);
        }
        else
        {
            // Unexplored - very dark
            return ASCIIColors.BgDark;
        }
    }

    /// <summary>
    /// Get the effective background color considering visibility and lighting.
    /// </summary>
    public readonly Color GetEffectiveBackground()
    {
        if (IsVisible)
        {
            return Background.WithLighting(LightLevel);
        }
        else if (WasSeen)
        {
            // Memory rendering - dimmed
            return Background.WithLighting(0.2f);
        }
        else
        {
            // Unexplored
            return ASCIIColors.BgDark;
        }
    }

    /// <summary>
    /// Get the effective character considering visibility.
    /// </summary>
    public readonly char GetEffectiveCharacter()
    {
        if (IsVisible || WasSeen)
        {
            return Character;
        }
        else
        {
            // Unexplored areas show fog character
            return ASCIIChars.Fog;
        }
    }

    /// <summary>
    /// Get foreground hex color for BBCode rendering.
    /// </summary>
    public readonly string GetForegroundHex()
    {
        return GetEffectiveForeground().ToHtml(false);
    }

    /// <summary>
    /// Get background hex color for BBCode rendering.
    /// </summary>
    public readonly string GetBackgroundHex()
    {
        return GetEffectiveBackground().ToHtml(false);
    }
}
