using Godot;
using System.Collections.Generic;
using NullAndVoid.Components;
using NullAndVoid.Items;
using NullAndVoid.Core;

namespace NullAndVoid.UI;

/// <summary>
/// Terminal-styled full-screen inventory management UI.
/// Shows inventory items and equipment slots, allows equipping/unequipping.
/// </summary>
public partial class InventoryScreen : Control
{
    private Inventory? _inventory;
    private Equipment? _equipment;

    // UI References
    private Panel? _mainPanel;
    private GridContainer? _inventoryGrid;
    private VBoxContainer? _equipmentSlots;
    private Label? _itemNameLabel;
    private Label? _itemDescLabel;
    private Label? _statsLabel;
    private Button? _equipButton;
    private Button? _unequipButton;
    private Button? _closeButton;

    // Selection state
    private Item? _selectedItem;
    private bool _selectedFromEquipment;
    private EquipmentSlotType _selectedSlotType;
    private int _selectedSlotIndex;

    public override void _Ready()
    {
        // Get UI references with updated paths
        _mainPanel = GetNode<Panel>("MainPanel");
        _inventoryGrid = GetNode<GridContainer>("MainPanel/VBoxContainer/HSplit/InventoryPanel/VBox/ScrollContainer/InventoryGrid");
        _equipmentSlots = GetNode<VBoxContainer>("MainPanel/VBoxContainer/HSplit/EquipmentPanel/VBox/EquipmentSlots");
        _itemNameLabel = GetNode<Label>("MainPanel/VBoxContainer/HSplit/DetailsPanel/VBox/ItemName");
        _itemDescLabel = GetNode<Label>("MainPanel/VBoxContainer/HSplit/DetailsPanel/VBox/DescScroll/ItemDesc");
        _statsLabel = GetNode<Label>("MainPanel/VBoxContainer/HSplit/DetailsPanel/VBox/Stats");
        _equipButton = GetNode<Button>("MainPanel/VBoxContainer/HSplit/DetailsPanel/VBox/ButtonContainer/EquipButton");
        _unequipButton = GetNode<Button>("MainPanel/VBoxContainer/HSplit/DetailsPanel/VBox/ButtonContainer/UnequipButton");
        _closeButton = GetNode<Button>("MainPanel/VBoxContainer/TitleBar/CloseButton");

        // Connect signals
        _equipButton?.Connect("pressed", Callable.From(OnEquipPressed));
        _unequipButton?.Connect("pressed", Callable.From(OnUnequipPressed));
        _closeButton?.Connect("pressed", Callable.From(Close));

        // Apply terminal styling
        ApplyTerminalStyling();

        // Hide by default
        Visible = false;
    }

    private void ApplyTerminalStyling()
    {
        // Style main panel
        if (_mainPanel != null)
            TerminalTheme.StylePanel(_mainPanel);

        // Style all sub-panels
        var inventoryPanel = GetNodeOrNull<Panel>("MainPanel/VBoxContainer/HSplit/InventoryPanel");
        var equipmentPanel = GetNodeOrNull<Panel>("MainPanel/VBoxContainer/HSplit/EquipmentPanel");
        var detailsPanel = GetNodeOrNull<Panel>("MainPanel/VBoxContainer/HSplit/DetailsPanel");

        if (inventoryPanel != null) TerminalTheme.StylePanel(inventoryPanel);
        if (equipmentPanel != null) TerminalTheme.StylePanel(equipmentPanel);
        if (detailsPanel != null) TerminalTheme.StylePanel(detailsPanel);

        // Style headers
        var titleLabel = GetNodeOrNull<Label>("MainPanel/VBoxContainer/TitleBar/Title");
        var invHeader = GetNodeOrNull<Label>("MainPanel/VBoxContainer/HSplit/InventoryPanel/VBox/Header");
        var equipHeader = GetNodeOrNull<Label>("MainPanel/VBoxContainer/HSplit/EquipmentPanel/VBox/Header");
        var detailsHeader = GetNodeOrNull<Label>("MainPanel/VBoxContainer/HSplit/DetailsPanel/VBox/Header");

        if (titleLabel != null) TerminalTheme.StyleLabel(titleLabel, TerminalTheme.PrimaryBright, 18);
        if (invHeader != null) TerminalTheme.StyleLabel(invHeader, TerminalTheme.Primary, 14);
        if (equipHeader != null) TerminalTheme.StyleLabel(equipHeader, TerminalTheme.Primary, 14);
        if (detailsHeader != null) TerminalTheme.StyleLabel(detailsHeader, TerminalTheme.Primary, 14);

        // Style buttons
        if (_closeButton != null) TerminalTheme.StyleButton(_closeButton);
        if (_equipButton != null) TerminalTheme.StyleButton(_equipButton);
        if (_unequipButton != null) TerminalTheme.StyleButton(_unequipButton);
    }

    /// <summary>
    /// Connect to inventory and equipment components.
    /// </summary>
    public void Setup(Inventory inventory, Equipment equipment)
    {
        _inventory = inventory;
        _equipment = equipment;

        if (_inventory != null)
            _inventory.InventoryChanged += RefreshDisplay;
        if (_equipment != null)
            _equipment.EquipmentChanged += RefreshDisplay;
    }

    public void Open()
    {
        Visible = true;
        _selectedItem = null;
        RefreshDisplay();
        ClearSelection();
        GameState.Instance.TransitionTo(GameState.State.Inventory);
    }

    public void Close()
    {
        Visible = false;
        GameState.Instance.TransitionTo(GameState.State.Playing);
    }

    public void Toggle()
    {
        if (Visible)
            Close();
        else
            Open();
    }

    private void RefreshDisplay()
    {
        RefreshInventoryGrid();
        RefreshEquipmentSlots();
        UpdateDetailsPanel();
    }

    private void RefreshInventoryGrid()
    {
        if (_inventoryGrid == null || _inventory == null)
            return;

        // Clear existing
        foreach (var child in _inventoryGrid.GetChildren())
        {
            child.QueueFree();
        }

        // Add inventory items
        foreach (var item in _inventory.Items)
        {
            var button = CreateItemButton(item, false, EquipmentSlotType.Any, 0);
            _inventoryGrid.AddChild(button);
        }

        // Add empty slots
        for (int i = _inventory.ItemCount; i < _inventory.MaxSlots; i++)
        {
            var emptyButton = CreateEmptySlotButton();
            _inventoryGrid.AddChild(emptyButton);
        }
    }

    private void RefreshEquipmentSlots()
    {
        if (_equipmentSlots == null || _equipment == null)
            return;

        // Clear existing
        foreach (var child in _equipmentSlots.GetChildren())
        {
            child.QueueFree();
        }

        // Create equipment slot sections
        CreateEquipmentSection(">> CORE MODULES <<", EquipmentSlotType.Core, 2);
        CreateEquipmentSection(">> UTILITY MODULES <<", EquipmentSlotType.Utility, 2);
        CreateEquipmentSection(">> BASE MODULES <<", EquipmentSlotType.Base, 2);
    }

    private void CreateEquipmentSection(string title, EquipmentSlotType slotType, int slotCount)
    {
        if (_equipmentSlots == null || _equipment == null)
            return;

        // Section header
        var header = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        TerminalTheme.StyleLabel(header, TerminalTheme.GetSlotColor(slotType), 12);
        _equipmentSlots.AddChild(header);

        // Slots in horizontal container
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        _equipmentSlots.AddChild(hbox);

        for (int i = 0; i < slotCount; i++)
        {
            var item = _equipment.GetItemInSlot(slotType, i);
            var button = CreateItemButton(item, true, slotType, i);
            button.CustomMinimumSize = new Vector2(140, 55);
            hbox.AddChild(button);
        }

        // Spacer
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
        _equipmentSlots.AddChild(spacer);
    }

    private Button CreateItemButton(Item? item, bool isEquipment, EquipmentSlotType slotType, int slotIndex)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(75, 55),
            ClipText = true
        };

        // Apply terminal button styling
        TerminalTheme.StyleButton(button);

        if (item != null)
        {
            button.Text = item.Name;
            button.AddThemeColorOverride("font_color", TerminalTheme.GetRarityColor(item.Rarity));
            button.AddThemeColorOverride("font_hover_color", TerminalTheme.PrimaryBright);
            button.Connect("pressed", Callable.From(() => SelectItem(item, isEquipment, slotType, slotIndex)));
        }
        else if (isEquipment)
        {
            button.Text = TerminalTheme.FormatEmpty();
            button.AddThemeColorOverride("font_color", TerminalTheme.TextMuted);
            button.Connect("pressed", Callable.From(() => SelectEmptySlot(slotType, slotIndex)));
        }
        else
        {
            button.Text = "";
            button.Disabled = true;
            button.AddThemeColorOverride("font_color", TerminalTheme.TextDisabled);
        }

        return button;
    }

    private Button CreateEmptySlotButton()
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(75, 55),
            Text = "",
            Disabled = true
        };
        TerminalTheme.StyleButton(button);
        button.AddThemeColorOverride("font_color", TerminalTheme.TextDisabled);
        return button;
    }

    private void SelectItem(Item item, bool fromEquipment, EquipmentSlotType slotType, int slotIndex)
    {
        _selectedItem = item;
        _selectedFromEquipment = fromEquipment;
        _selectedSlotType = slotType;
        _selectedSlotIndex = slotIndex;
        UpdateDetailsPanel();
    }

    private void SelectEmptySlot(EquipmentSlotType slotType, int slotIndex)
    {
        _selectedItem = null;
        _selectedFromEquipment = true;
        _selectedSlotType = slotType;
        _selectedSlotIndex = slotIndex;
        UpdateDetailsPanel();
    }

    private void ClearSelection()
    {
        _selectedItem = null;
        _selectedFromEquipment = false;
        UpdateDetailsPanel();
    }

    private void UpdateDetailsPanel()
    {
        if (_itemNameLabel == null || _itemDescLabel == null || _statsLabel == null ||
            _equipButton == null || _unequipButton == null)
            return;

        if (_selectedItem != null)
        {
            _itemNameLabel.Text = _selectedItem.Name;
            TerminalTheme.StyleLabel(_itemNameLabel, TerminalTheme.GetRarityColor(_selectedItem.Rarity), 16);

            _itemDescLabel.Text = _selectedItem.Description;
            TerminalTheme.StyleLabel(_itemDescLabel, TerminalTheme.TextSecondary, 12);

            // Format stats with terminal style
            var statsText = TerminalTheme.FormatStats(
                _selectedItem.BonusDamage,
                _selectedItem.BonusArmor,
                _selectedItem.BonusHealth,
                _selectedItem.BonusSightRange
            );
            _statsLabel.Text = $"Slot: [{_selectedItem.SlotType}]  Rarity: [{_selectedItem.Rarity}]\n{statsText}";
            TerminalTheme.StyleLabel(_statsLabel, TerminalTheme.Primary, 11);

            // Show appropriate button
            _equipButton.Visible = !_selectedFromEquipment;
            _unequipButton.Visible = _selectedFromEquipment;
        }
        else
        {
            _itemNameLabel.Text = "No Item Selected";
            TerminalTheme.StyleLabel(_itemNameLabel, TerminalTheme.TextMuted, 16);

            _itemDescLabel.Text = "Select an item from your inventory or equipment slots to view its details.";
            TerminalTheme.StyleLabel(_itemDescLabel, TerminalTheme.TextMuted, 12);

            _statsLabel.Text = "";
            _equipButton.Visible = false;
            _unequipButton.Visible = false;
        }
    }

    private void OnEquipPressed()
    {
        if (_selectedItem == null || _inventory == null || _equipment == null || _selectedFromEquipment)
            return;

        // Find a slot for this item
        var slot = _equipment.FindEmptySlot(_selectedItem.SlotType);
        if (slot == null)
        {
            // No empty slot, try to swap with first slot of the right type
            var targetType = _selectedItem.SlotType == EquipmentSlotType.Any
                ? EquipmentSlotType.Core
                : _selectedItem.SlotType;

            var unequipped = _equipment.Equip(_selectedItem, targetType, 0);
            _inventory.RemoveItem(_selectedItem);

            if (unequipped != null)
                _inventory.AddItem(unequipped);
        }
        else
        {
            _equipment.Equip(_selectedItem, slot.Value.slotType, slot.Value.index);
            _inventory.RemoveItem(_selectedItem);
        }

        ClearSelection();
        RefreshDisplay();
    }

    private void OnUnequipPressed()
    {
        if (_selectedItem == null || _inventory == null || _equipment == null || !_selectedFromEquipment)
            return;

        if (_inventory.IsFull)
        {
            GD.Print("> ERROR: Inventory full - cannot unequip!");
            return;
        }

        var unequipped = _equipment.Unequip(_selectedSlotType, _selectedSlotIndex);
        if (unequipped != null)
        {
            _inventory.AddItem(unequipped);
        }

        ClearSelection();
        RefreshDisplay();
    }

    public override void _ExitTree()
    {
        if (_inventory != null)
            _inventory.InventoryChanged -= RefreshDisplay;
        if (_equipment != null)
            _equipment.EquipmentChanged -= RefreshDisplay;
    }
}
