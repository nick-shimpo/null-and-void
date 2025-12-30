using Godot;
using System.Collections.Generic;
using NullAndVoid.Components;
using NullAndVoid.Items;
using NullAndVoid.Core;

namespace NullAndVoid.UI;

/// <summary>
/// Full-screen inventory management UI.
/// Shows inventory items and equipment slots, allows equipping/unequipping.
/// </summary>
public partial class InventoryScreen : Control
{
    private Inventory? _inventory;
    private Equipment? _equipment;

    // UI References
    private GridContainer? _inventoryGrid;
    private VBoxContainer? _equipmentPanel;
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

    // Slot button references for highlighting
    private readonly Dictionary<string, Button> _slotButtons = new();

    public override void _Ready()
    {
        // Get UI references
        _inventoryGrid = GetNode<GridContainer>("MainPanel/HSplit/InventoryPanel/ScrollContainer/InventoryGrid");
        _equipmentPanel = GetNode<VBoxContainer>("MainPanel/HSplit/EquipmentPanel/EquipmentSlots");
        _itemNameLabel = GetNode<Label>("MainPanel/HSplit/DetailsPanel/ItemName");
        _itemDescLabel = GetNode<Label>("MainPanel/HSplit/DetailsPanel/ItemDesc");
        _statsLabel = GetNode<Label>("MainPanel/HSplit/DetailsPanel/Stats");
        _equipButton = GetNode<Button>("MainPanel/HSplit/DetailsPanel/EquipButton");
        _unequipButton = GetNode<Button>("MainPanel/HSplit/DetailsPanel/UnequipButton");
        _closeButton = GetNode<Button>("MainPanel/CloseButton");

        // Connect signals
        _equipButton?.Connect("pressed", Callable.From(OnEquipPressed));
        _unequipButton?.Connect("pressed", Callable.From(OnUnequipPressed));
        _closeButton?.Connect("pressed", Callable.From(Close));

        // Hide by default
        Visible = false;
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

    /// <summary>
    /// Open the inventory screen.
    /// </summary>
    public void Open()
    {
        Visible = true;
        _selectedItem = null;
        RefreshDisplay();
        ClearSelection();
        GameState.Instance.TransitionTo(GameState.State.Inventory);
    }

    /// <summary>
    /// Close the inventory screen.
    /// </summary>
    public void Close()
    {
        Visible = false;
        GameState.Instance.TransitionTo(GameState.State.Playing);
    }

    /// <summary>
    /// Toggle inventory visibility.
    /// </summary>
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
        if (_equipmentPanel == null || _equipment == null)
            return;

        // Clear existing
        foreach (var child in _equipmentPanel.GetChildren())
        {
            child.QueueFree();
        }
        _slotButtons.Clear();

        // Create equipment slot sections
        CreateEquipmentSection("CORE SLOTS", EquipmentSlotType.Core, 2);
        CreateEquipmentSection("UTILITY SLOTS", EquipmentSlotType.Utility, 2);
        CreateEquipmentSection("BASE SLOTS", EquipmentSlotType.Base, 2);
    }

    private void CreateEquipmentSection(string title, EquipmentSlotType slotType, int slotCount)
    {
        if (_equipmentPanel == null || _equipment == null)
            return;

        // Section header
        var header = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        header.AddThemeFontSizeOverride("font_size", 12);
        header.AddThemeColorOverride("font_color", GetSlotTypeColor(slotType));
        _equipmentPanel.AddChild(header);

        // Slots
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        _equipmentPanel.AddChild(hbox);

        for (int i = 0; i < slotCount; i++)
        {
            var item = _equipment.GetItemInSlot(slotType, i);
            var button = CreateItemButton(item, true, slotType, i);
            button.CustomMinimumSize = new Vector2(150, 60);
            hbox.AddChild(button);

            string key = $"{slotType}_{i}";
            _slotButtons[key] = button;
        }

        // Spacer
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 10) };
        _equipmentPanel.AddChild(spacer);
    }

    private Button CreateItemButton(Item? item, bool isEquipment, EquipmentSlotType slotType, int slotIndex)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(80, 60),
            ClipText = true
        };

        if (item != null)
        {
            button.Text = item.Name;
            button.Modulate = item.DisplayColor;
            button.Connect("pressed", Callable.From(() => SelectItem(item, isEquipment, slotType, slotIndex)));
        }
        else if (isEquipment)
        {
            button.Text = "[Empty]";
            button.Modulate = new Color(0.5f, 0.5f, 0.5f);
            button.Connect("pressed", Callable.From(() => SelectEmptySlot(slotType, slotIndex)));
        }
        else
        {
            button.Text = "";
            button.Disabled = true;
            button.Modulate = new Color(0.3f, 0.3f, 0.3f);
        }

        return button;
    }

    private Button CreateEmptySlotButton()
    {
        return new Button
        {
            CustomMinimumSize = new Vector2(80, 60),
            Text = "",
            Disabled = true,
            Modulate = new Color(0.3f, 0.3f, 0.3f)
        };
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
            _itemNameLabel.AddThemeColorOverride("font_color", _selectedItem.DisplayColor);
            _itemDescLabel.Text = _selectedItem.Description;
            _statsLabel.Text = $"Slot Type: {_selectedItem.SlotType}\nRarity: {_selectedItem.Rarity}";

            // Show appropriate button
            _equipButton.Visible = !_selectedFromEquipment;
            _unequipButton.Visible = _selectedFromEquipment;
        }
        else
        {
            _itemNameLabel.Text = "No Item Selected";
            _itemNameLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            _itemDescLabel.Text = "Select an item from your inventory or equipment slots.";
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
            GD.Print("Inventory is full - cannot unequip!");
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
        if (_inventory != null)
            _inventory.InventoryChanged -= RefreshDisplay;
        if (_equipment != null)
            _equipment.EquipmentChanged -= RefreshDisplay;
    }
}
