using Godot;
using NullAndVoid.Items;

namespace NullAndVoid.UI;

/// <summary>
/// Terminal/Console theme colors and formatting helpers.
/// Provides a consistent CRT-style aesthetic across all UI.
/// </summary>
public static class TerminalTheme
{
    // Primary - Phosphor Green
    public static readonly Color Primary = new(0.2f, 0.9f, 0.4f);
    public static readonly Color PrimaryDim = new(0.15f, 0.6f, 0.3f);
    public static readonly Color PrimaryBright = new(0.4f, 1.0f, 0.6f);

    // Backgrounds - Dark CRT
    public static readonly Color Background = new(0.02f, 0.04f, 0.02f);
    public static readonly Color BackgroundPanel = new(0.04f, 0.08f, 0.04f);
    public static readonly Color BackgroundHighlight = new(0.08f, 0.12f, 0.08f);

    // Borders
    public static readonly Color Border = new(0.1f, 0.25f, 0.1f);
    public static readonly Color BorderBright = new(0.15f, 0.4f, 0.15f);

    // Slot Type Colors (distinct but terminal-adjusted)
    public static readonly Color SlotCore = new(1.0f, 0.4f, 0.4f);
    public static readonly Color SlotUtility = new(0.4f, 0.9f, 1.0f);
    public static readonly Color SlotBase = new(0.6f, 0.9f, 0.4f);

    // Text hierarchy
    public static readonly Color TextPrimary = new(0.2f, 0.9f, 0.4f);
    public static readonly Color TextSecondary = new(0.15f, 0.6f, 0.3f);
    public static readonly Color TextMuted = new(0.1f, 0.35f, 0.15f);
    public static readonly Color TextDisabled = new(0.08f, 0.2f, 0.1f);

    // Alert/Status colors
    public static readonly Color AlertDanger = new(1.0f, 0.3f, 0.3f);
    public static readonly Color AlertWarning = new(1.0f, 0.8f, 0.2f);
    public static readonly Color AlertSuccess = new(0.3f, 1.0f, 0.5f);
    public static readonly Color AlertInfo = new(0.4f, 0.8f, 1.0f);

    // Rarity colors (terminal-adjusted)
    public static readonly Color RarityCommon = new(0.6f, 0.6f, 0.6f);
    public static readonly Color RarityUncommon = new(0.3f, 0.9f, 0.4f);
    public static readonly Color RarityRare = new(0.4f, 0.6f, 1.0f);
    public static readonly Color RarityEpic = new(0.8f, 0.4f, 1.0f);
    public static readonly Color RarityLegendary = new(1.0f, 0.7f, 0.2f);

    /// <summary>
    /// Format a status display like [HP: 100]
    /// </summary>
    public static string FormatStatus(string label, string value)
        => $"[{label}: {value}]";

    /// <summary>
    /// Format a header like >> INVENTORY <<
    /// </summary>
    public static string FormatHeader(string text)
        => $">> {text.ToUpper()} <<";

    /// <summary>
    /// Format a slot type label like [CORE 1]
    /// </summary>
    public static string FormatSlotType(EquipmentSlotType slotType, int index)
        => $"[{GetSlotAbbrev(slotType)} {index + 1}]";

    /// <summary>
    /// Format an empty slot indicator
    /// </summary>
    public static string FormatEmpty()
        => "[--------]";

    /// <summary>
    /// Format item stats like [DMG +5] [ARM +2]
    /// </summary>
    public static string FormatStats(int damage, int armor, int health, int sight)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (damage > 0) parts.Add($"[DMG +{damage}]");
        if (armor > 0) parts.Add($"[ARM +{armor}]");
        if (health > 0) parts.Add($"[HP +{health}]");
        if (sight > 0) parts.Add($"[SIG +{sight}]");
        return parts.Count > 0 ? string.Join(" ", parts) : "[NO STATS]";
    }

    /// <summary>
    /// Get abbreviated slot type name
    /// </summary>
    public static string GetSlotAbbrev(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => "CORE",
            EquipmentSlotType.Utility => "UTIL",
            EquipmentSlotType.Base => "BASE",
            _ => "????"
        };
    }

    /// <summary>
    /// Get the terminal-themed color for a slot type
    /// </summary>
    public static Color GetSlotColor(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => SlotCore,
            EquipmentSlotType.Utility => SlotUtility,
            EquipmentSlotType.Base => SlotBase,
            _ => TextMuted
        };
    }

    /// <summary>
    /// Get the terminal-themed color for a rarity
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => RarityCommon,
            ItemRarity.Uncommon => RarityUncommon,
            ItemRarity.Rare => RarityRare,
            ItemRarity.Epic => RarityEpic,
            ItemRarity.Legendary => RarityLegendary,
            _ => TextMuted
        };
    }

    /// <summary>
    /// Create a StyleBoxFlat with terminal panel styling
    /// </summary>
    public static StyleBoxFlat CreatePanelStyle(bool highlighted = false)
    {
        var style = new StyleBoxFlat
        {
            BgColor = highlighted ? BackgroundHighlight : BackgroundPanel,
            BorderColor = highlighted ? BorderBright : Border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0
        };
        return style;
    }

    /// <summary>
    /// Create a StyleBoxFlat for terminal buttons
    /// </summary>
    public static StyleBoxFlat CreateButtonStyle(bool pressed = false, bool hover = false)
    {
        var style = new StyleBoxFlat
        {
            BgColor = pressed ? BackgroundHighlight : (hover ? new Color(0.06f, 0.1f, 0.06f) : BackgroundPanel),
            BorderColor = pressed ? PrimaryDim : Border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        return style;
    }

    /// <summary>
    /// Apply terminal styling to a Label
    /// </summary>
    public static void StyleLabel(Label label, Color? color = null, int fontSize = 14)
    {
        label.AddThemeColorOverride("font_color", color ?? TextPrimary);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    /// <summary>
    /// Apply terminal styling to a Button
    /// </summary>
    public static void StyleButton(Button button)
    {
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle());
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(hover: true));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(pressed: true));
        button.AddThemeStyleboxOverride("focus", CreateButtonStyle());
        button.AddThemeColorOverride("font_color", Primary);
        button.AddThemeColorOverride("font_hover_color", PrimaryBright);
        button.AddThemeColorOverride("font_pressed_color", PrimaryDim);
        button.AddThemeColorOverride("font_disabled_color", TextDisabled);
    }

    /// <summary>
    /// Apply terminal styling to a Panel
    /// </summary>
    public static void StylePanel(Panel panel, bool highlighted = false)
    {
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(highlighted));
    }
}
