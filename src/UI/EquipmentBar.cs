using Godot;
using NullAndVoid.Components;
using NullAndVoid.Items;

namespace NullAndVoid.UI;

/// <summary>
/// Terminal-styled UI component showing equipped items at the bottom of the screen.
/// Displays 6 slots: 2 Core, 2 Utility, 2 Base with terminal formatting.
/// </summary>
public partial class EquipmentBar : Control
{
    private Equipment? _equipment;
    private HBoxContainer? _slotsContainer;
    private Panel? _backgroundPanel;

    public override void _Ready()
    {
        // Create the layout structure programmatically for full control
        SetupLayout();
    }

    private void SetupLayout()
    {
        // Clear any existing children
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        // Background panel
        _backgroundPanel = new Panel();
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        TerminalTheme.StylePanel(_backgroundPanel);
        AddChild(_backgroundPanel);

        // Margin container for padding
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        AddChild(margin);

        // Horizontal container for slots
        _slotsContainer = new HBoxContainer();
        _slotsContainer.Alignment = BoxContainer.AlignmentMode.Center;
        _slotsContainer.AddThemeConstantOverride("separation", 8);
        margin.AddChild(_slotsContainer);
    }

    /// <summary>
    /// Connect to an equipment component.
    /// </summary>
    public void SetEquipment(Equipment equipment)
    {
        if (_equipment != null)
        {
            _equipment.EquipmentChanged -= OnEquipmentChanged;
        }

        _equipment = equipment;

        if (_equipment != null)
        {
            _equipment.EquipmentChanged += OnEquipmentChanged;
            UpdateDisplay();
        }
    }

    private void OnEquipmentChanged()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_slotsContainer == null || _equipment == null)
            return;

        // Clear existing slots
        foreach (var child in _slotsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Create slot displays for all 6 slots
        var slots = _equipment.GetAllSlots();
        foreach (var slot in slots)
        {
            var slotPanel = CreateSlotPanel(slot);
            _slotsContainer.AddChild(slotPanel);
        }
    }

    private PanelContainer CreateSlotPanel(EquipmentSlotInfo slot)
    {
        // Outer panel container
        var panelContainer = new PanelContainer();
        panelContainer.CustomMinimumSize = new Vector2(170, 0); // Width only, height auto

        // Apply terminal styling
        var panelStyle = TerminalTheme.CreatePanelStyle(slot.Item != null);
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);

        // Margin container for internal padding
        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 6);
        marginContainer.AddThemeConstantOverride("margin_right", 6);
        marginContainer.AddThemeConstantOverride("margin_top", 4);
        marginContainer.AddThemeConstantOverride("margin_bottom", 4);
        panelContainer.AddChild(marginContainer);

        // VBox for stacking labels
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        marginContainer.AddChild(vbox);

        // Slot type label (e.g., "[CORE 1]")
        var typeLabel = new Label
        {
            Text = TerminalTheme.FormatSlotType(slot.SlotType, slot.Index),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        TerminalTheme.StyleLabel(typeLabel, TerminalTheme.GetSlotColor(slot.SlotType), 11);
        vbox.AddChild(typeLabel);

        // Item name or empty indicator
        if (slot.Item != null)
        {
            var nameLabel = new Label
            {
                Text = slot.Item.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
            };
            TerminalTheme.StyleLabel(nameLabel, TerminalTheme.GetRarityColor(slot.Item.Rarity), 12);
            vbox.AddChild(nameLabel);

            // Stats line
            var statsText = TerminalTheme.FormatStats(
                slot.Item.BonusDamage,
                slot.Item.BonusArmor,
                slot.Item.BonusHealth,
                slot.Item.BonusSightRange
            );
            var statsLabel = new Label
            {
                Text = statsText,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            TerminalTheme.StyleLabel(statsLabel, TerminalTheme.TextSecondary, 10);
            vbox.AddChild(statsLabel);
        }
        else
        {
            // Empty slot
            var emptyLabel = new Label
            {
                Text = TerminalTheme.FormatEmpty(),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            TerminalTheme.StyleLabel(emptyLabel, TerminalTheme.TextMuted, 11);
            vbox.AddChild(emptyLabel);

            // Spacer to maintain consistent height
            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 14);
            vbox.AddChild(spacer);
        }

        return panelContainer;
    }

    public override void _ExitTree()
    {
        if (_equipment != null)
        {
            _equipment.EquipmentChanged -= OnEquipmentChanged;
        }
    }
}
