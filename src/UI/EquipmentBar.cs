using Godot;
using NullAndVoid.Components;
using NullAndVoid.Items;

namespace NullAndVoid.UI;

/// <summary>
/// UI component showing equipped items at the bottom of the main screen.
/// Displays 6 slots: 2 Core, 2 Utility, 2 Base with short descriptions.
/// </summary>
public partial class EquipmentBar : Control
{
    private Equipment? _equipment;
    private HBoxContainer? _slotsContainer;

    public override void _Ready()
    {
        _slotsContainer = GetNode<HBoxContainer>("SlotsContainer");
    }

    /// <summary>
    /// Connect to an equipment component.
    /// </summary>
    public void SetEquipment(Equipment equipment)
    {
        // Disconnect from previous
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

        // Create slot displays
        var slots = _equipment.GetAllSlots();
        foreach (var slot in slots)
        {
            var slotPanel = CreateSlotPanel(slot);
            _slotsContainer.AddChild(slotPanel);
        }
    }

    private Panel CreateSlotPanel(EquipmentSlotInfo slot)
    {
        var panel = new Panel
        {
            CustomMinimumSize = new Vector2(180, 50)
        };

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        // Slot type label
        var typeLabel = new Label
        {
            Text = GetSlotTypeName(slot.SlotType, slot.Index),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        typeLabel.AddThemeFontSizeOverride("font_size", 10);
        typeLabel.AddThemeColorOverride("font_color", GetSlotTypeColor(slot.SlotType));
        vbox.AddChild(typeLabel);

        // Item name or empty
        if (slot.Item != null)
        {
            var nameLabel = new Label
            {
                Text = slot.Item.Name,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            nameLabel.AddThemeColorOverride("font_color", slot.Item.DisplayColor);
            vbox.AddChild(nameLabel);

            // Stats
            var statsLabel = new Label
            {
                Text = slot.Item.ShortDesc,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            statsLabel.AddThemeFontSizeOverride("font_size", 10);
            statsLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            vbox.AddChild(statsLabel);
        }
        else
        {
            var emptyLabel = new Label
            {
                Text = "[Empty]",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 11);
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
            vbox.AddChild(emptyLabel);
        }

        // Position the vbox
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.OffsetLeft = 5;
        vbox.OffsetTop = 2;
        vbox.OffsetRight = -5;
        vbox.OffsetBottom = -2;

        return panel;
    }

    private string GetSlotTypeName(EquipmentSlotType slotType, int index)
    {
        string typeName = slotType switch
        {
            EquipmentSlotType.Core => "CORE",
            EquipmentSlotType.Utility => "UTIL",
            EquipmentSlotType.Base => "BASE",
            _ => "????"
        };
        return $"{typeName} {index + 1}";
    }

    private Color GetSlotTypeColor(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => new Color(1.0f, 0.4f, 0.4f),
            EquipmentSlotType.Utility => new Color(0.4f, 0.8f, 1.0f),
            EquipmentSlotType.Base => new Color(0.6f, 0.8f, 0.4f),
            _ => new Color(0.7f, 0.7f, 0.7f)
        };
    }

    public override void _ExitTree()
    {
        if (_equipment != null)
        {
            _equipment.EquipmentChanged -= OnEquipmentChanged;
        }
    }
}
