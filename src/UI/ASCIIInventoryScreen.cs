using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Components;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Rendering;

namespace NullAndVoid.UI;

/// <summary>
/// Full-screen ASCII inventory management with keyboard-only navigation.
/// Displays inventory items, equipment slots, and item details.
/// Sized for widescreen displays.
/// </summary>
public partial class ASCIIInventoryScreen : Control
{
    // Layout constants (large widescreen - nearly fullscreen)
    // Total width: 1 (border) + 48 (inv) + 1 (sep) + 50 (equip) + 1 (sep) + 48 (details) + 1 (border) = 150
    private const int ScreenWidth = 150;
    private const int ScreenHeight = 43;

    // Panel content areas (not including borders/separators)
    private const int InvPanelStart = 1;
    private const int InvPanelWidth = 48;
    private const int Sep1X = 49;
    private const int EquipPanelStart = 50;
    private const int EquipPanelWidth = 50;
    private const int Sep2X = 100;
    private const int DetailsPanelStart = 101;
    private const int DetailsPanelWidth = 48;

    private const int ContentStartY = 3;
    private const int ContentHeight = 36;

    // Panel modes
    public enum Panel { Inventory, Equipment }
    private Panel _activePanel = Panel.Inventory;

    // Selection state
    private int _inventoryIndex = 0;
    private int _equipmentIndex = 0;

    // References
    private Inventory? _inventory;
    private Equipment? _equipment;
    private Player? _player;
    private ASCIIBuffer _buffer = null!;
    private RichTextLabel? _display;
    private Font? _font;

    // State
    private bool _isOpen = false;
    private Item? _selectedItem;
    private string _statusMessage = "";
    private float _statusTimer = 0f;

    // Comparison mode
    private bool _comparisonMode = false;
    private Item? _comparisonTarget; // The equipped item to compare against

    public bool IsOpen => _isOpen;

    // Events
    public event Action? Closed;

    public override void _Ready()
    {
        _buffer = new ASCIIBuffer();

        // Continue processing even when game tree is paused (for inventory state)
        ProcessMode = ProcessModeEnum.Always;

        // Ensure this Control doesn't block input and can receive keyboard events
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Ignore;

        CreateDisplay();
        Visible = false;
    }

    private void CreateDisplay()
    {
        // Create system font
        var systemFont = new SystemFont();
        systemFont.FontNames = new string[]
        {
            "Consolas", "Courier New", "SF Mono",
            "DejaVu Sans Mono", "Liberation Mono", "monospace"
        };
        systemFont.Antialiasing = TextServer.FontAntialiasing.Lcd;
        systemFont.Hinting = TextServer.Hinting.Normal;
        _font = systemFont;

        _display = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = false,
            SelectionEnabled = false,
            FitContent = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = true,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };

        _display.AddThemeFontOverride("normal_font", _font);
        _display.AddThemeFontSizeOverride("normal_font_size", 24);  // Match ASCIIRenderer font size

        var stylebox = new StyleBoxFlat { BgColor = ASCIIColors.BgDark };
        _display.AddThemeStyleboxOverride("normal", stylebox);

        // Size for 2560x1440 viewport at font size 24
        // Character dimensions at font 24: ~14px wide x ~28px tall
        // 150 chars * 14px = 2100px wide, 43 lines * 28px = 1204px tall
        float charWidth = 14f;
        float charHeight = 28f;
        float displayWidth = ScreenWidth * charWidth;
        float displayHeight = ScreenHeight * charHeight;
        _display.Size = new Vector2(displayWidth, displayHeight);
        _display.CustomMinimumSize = new Vector2(displayWidth, displayHeight);

        // Center in 2560x1440 viewport
        float viewportWidth = ASCIIRenderer.TargetWidth;
        float viewportHeight = ASCIIRenderer.TargetHeight;
        _display.Position = new Vector2(
            (viewportWidth - displayWidth) / 2,
            (viewportHeight - displayHeight) / 2
        );

        AddChild(_display);
    }

    public void Setup(Inventory inventory, Equipment equipment, Player? player = null)
    {
        _inventory = inventory;
        _equipment = equipment;
        _player = player;
    }

    public void Open()
    {
        if (_inventory == null || _equipment == null)
            return;

        _isOpen = true;
        _activePanel = Panel.Inventory;
        _inventoryIndex = 0;
        _equipmentIndex = 0;
        _statusMessage = "";
        UpdateSelectedItem();
        Render();
        Visible = true;
        GrabFocus();
        GameState.Instance.TransitionTo(GameState.State.Inventory);
    }

    public void Close()
    {
        _isOpen = false;
        Visible = false;
        GameState.Instance.TransitionTo(GameState.State.Playing);
        Closed?.Invoke();
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public override void _Process(double delta)
    {
        if (!_isOpen)
            return;

        // Update status message timer
        if (_statusTimer > 0)
        {
            _statusTimer -= (float)delta;
            if (_statusTimer <= 0)
            {
                _statusMessage = "";
                Render();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isOpen)
            return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            HandleKeyInput(keyEvent);
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        var keycode = keyEvent.Keycode;
        var physical = keyEvent.PhysicalKeycode;

        // Escape - exit comparison mode or close inventory
        if (keycode == Key.Escape)
        {
            if (_comparisonMode)
            {
                ExitComparisonMode();
                return;
            }
            Close();
            return;
        }

        // I key - close inventory (but not if in comparison mode)
        if (keycode == Key.I && !_comparisonMode)
        {
            Close();
            return;
        }

        // Navigation - WASD and Arrow keys
        if (keycode == Key.Up || physical == Key.W)
        {
            NavigateUp();
        }
        else if (keycode == Key.Down || physical == Key.S)
        {
            NavigateDown();
        }
        else if (keycode == Key.Left || physical == Key.A)
        {
            NavigateLeft();
        }
        else if (keycode == Key.Right || physical == Key.D)
        {
            NavigateRight();
        }
        // Tab to switch panels
        else if (keycode == Key.Tab)
        {
            SwitchPanel();
        }
        // Actions
        else if (keycode == Key.E || keycode == Key.Enter || keycode == Key.KpEnter)
        {
            PerformEquipAction();
        }
        else if (keycode == Key.U || physical == Key.R)
        {
            PerformUnequipAction();
        }
        else if (physical == Key.X)
        {
            PerformDropAction();
        }
        // Comparison mode toggle
        else if (physical == Key.C && _activePanel == Panel.Inventory && !_comparisonMode)
        {
            ToggleComparisonMode();
        }
        // Toggle module on/off
        else if (physical == Key.T && _activePanel == Panel.Equipment)
        {
            PerformToggleAction();
        }
        // Letter selection (a-z for inventory items)
        else if (keycode >= Key.A && keycode <= Key.Z && _activePanel == Panel.Inventory && !_comparisonMode)
        {
            int index = (int)keycode - (int)Key.A;
            SelectInventoryByLetter(index);
        }
        // Number selection (1-6 for equipment slots)
        else if (keycode >= Key.Key1 && keycode <= Key.Key6 && _activePanel == Panel.Equipment)
        {
            int index = (int)keycode - (int)Key.Key1;
            SelectEquipmentByNumber(index);
        }
    }

    private void ToggleComparisonMode()
    {
        if (_selectedItem == null || _equipment == null)
            return;

        // Find the equipped item in the same slot type
        var slotType = _selectedItem.SlotType;
        if (slotType == EquipmentSlotType.Any)
            slotType = EquipmentSlotType.Core; // Default to Core for "Any" type

        // Check both slots of the type
        _comparisonTarget = _equipment.GetItemInSlot(slotType, 0)
                         ?? _equipment.GetItemInSlot(slotType, 1);

        if (_comparisonTarget == null)
        {
            SetStatus("No equipped item to compare", ASCIIColors.AlertWarning);
            return;
        }

        _comparisonMode = true;
        Render();
    }

    private void ExitComparisonMode()
    {
        _comparisonMode = false;
        _comparisonTarget = null;
        Render();
    }

    private void PerformToggleAction()
    {
        if (_selectedItem == null || _equipment == null)
            return;

        if (!_selectedItem.IsToggleable)
        {
            SetStatus("This module cannot be toggled", ASCIIColors.AlertWarning);
            return;
        }

        // Toggle the module
        var (slotType, slotIndex) = GetSlotInfoAtIndex(_equipmentIndex);
        _equipment.ToggleModule(slotType, slotIndex);

        string state = _selectedItem.IsActive ? "ON" : "OFF";
        SetStatus($"{_selectedItem.Name} toggled {state}", ASCIIColors.AlertInfo);
        Render();
    }

    private void NavigateUp()
    {
        if (_activePanel == Panel.Inventory)
        {
            if (_inventoryIndex > 0)
                _inventoryIndex--;
        }
        else
        {
            if (_equipmentIndex > 0)
                _equipmentIndex--;
        }
        UpdateSelectedItem();
        Render();
    }

    private void NavigateDown()
    {
        if (_activePanel == Panel.Inventory)
        {
            int maxIndex = (_inventory?.ItemCount ?? 1) - 1;
            if (_inventoryIndex < maxIndex)
                _inventoryIndex++;
        }
        else
        {
            if (_equipmentIndex < 5) // 6 equipment slots (0-5)
                _equipmentIndex++;
        }
        UpdateSelectedItem();
        Render();
    }

    private void NavigateLeft()
    {
        if (_activePanel == Panel.Equipment)
        {
            _activePanel = Panel.Inventory;
            UpdateSelectedItem();
            Render();
        }
    }

    private void NavigateRight()
    {
        if (_activePanel == Panel.Inventory)
        {
            _activePanel = Panel.Equipment;
            UpdateSelectedItem();
            Render();
        }
    }

    private void SwitchPanel()
    {
        _activePanel = _activePanel == Panel.Inventory ? Panel.Equipment : Panel.Inventory;
        UpdateSelectedItem();
        Render();
    }

    private void SelectInventoryByLetter(int index)
    {
        if (_inventory == null)
            return;

        if (index < _inventory.ItemCount)
        {
            _activePanel = Panel.Inventory;
            _inventoryIndex = index;
            UpdateSelectedItem();
            Render();
        }
    }

    private void SelectEquipmentByNumber(int index)
    {
        if (index <= 5)
        {
            _activePanel = Panel.Equipment;
            _equipmentIndex = index;
            UpdateSelectedItem();
            Render();
        }
    }

    private void UpdateSelectedItem()
    {
        if (_activePanel == Panel.Inventory)
        {
            if (_inventory != null && _inventoryIndex < _inventory.ItemCount)
            {
                var items = new List<Item>(_inventory.Items);
                _selectedItem = items[_inventoryIndex];
            }
            else
            {
                _selectedItem = null;
            }
        }
        else
        {
            _selectedItem = GetEquipmentAtIndex(_equipmentIndex);
        }
    }

    private Item? GetEquipmentAtIndex(int index)
    {
        if (_equipment == null)
            return null;

        return index switch
        {
            0 => _equipment.GetItemInSlot(EquipmentSlotType.Core, 0),
            1 => _equipment.GetItemInSlot(EquipmentSlotType.Core, 1),
            2 => _equipment.GetItemInSlot(EquipmentSlotType.Utility, 0),
            3 => _equipment.GetItemInSlot(EquipmentSlotType.Utility, 1),
            4 => _equipment.GetItemInSlot(EquipmentSlotType.Base, 0),
            5 => _equipment.GetItemInSlot(EquipmentSlotType.Base, 1),
            _ => null
        };
    }

    private (EquipmentSlotType type, int slotIndex) GetSlotInfoAtIndex(int index)
    {
        return index switch
        {
            0 => (EquipmentSlotType.Core, 0),
            1 => (EquipmentSlotType.Core, 1),
            2 => (EquipmentSlotType.Utility, 0),
            3 => (EquipmentSlotType.Utility, 1),
            4 => (EquipmentSlotType.Base, 0),
            5 => (EquipmentSlotType.Base, 1),
            _ => (EquipmentSlotType.Any, 0)
        };
    }

    private void PerformEquipAction()
    {
        if (_inventory == null || _equipment == null)
            return;

        // In comparison mode, we're equipping from inventory panel
        if (_comparisonMode && _selectedItem != null)
        {
            // Equip and replace the comparison target
            if (_comparisonTarget != null)
            {
                var (slotType, slotIndex) = FindItemSlot(_comparisonTarget);
                if (slotType != EquipmentSlotType.Any)
                {
                    var itemToEquip = _selectedItem;
                    var previousItem = _equipment.Equip(itemToEquip, slotType, slotIndex);

                    // Check if item is now in the slot
                    if (_equipment.GetItemInSlot(slotType, slotIndex) == itemToEquip)
                    {
                        // Item equipped - remove from inventory
                        _inventory.RemoveItem(itemToEquip);
                        if (previousItem != null)
                            _inventory.AddItem(previousItem);
                        ConsumeEquipTurn();

                        // Check if mount failure occurred (item jammed)
                        if (_equipment.IsItemJammed(slotType, slotIndex))
                        {
                            SetStatus($"Mount failure! {itemToEquip.Name} jammed - both damaged!", ASCIIColors.AlertDanger);
                        }
                        else
                        {
                            SetStatus($"Equipped {itemToEquip.Name}", ASCIIColors.AlertSuccess);
                        }
                    }
                    else
                    {
                        // Equip failed (damaged mount, insufficient energy, etc.)
                        SetStatus($"Cannot equip - check mount point or energy", ASCIIColors.AlertWarning);
                    }
                }
            }
            ExitComparisonMode();
            UpdateSelectedItem();
            Render();
            return;
        }

        if (_activePanel == Panel.Inventory && _selectedItem != null)
        {
            var itemToEquip = _selectedItem;

            // Equip from inventory
            var slot = _equipment.FindEmptySlot(itemToEquip.SlotType);
            if (slot != null)
            {
                var previousItem = _equipment.Equip(itemToEquip, slot.Value.slotType, slot.Value.index);

                // Check if item is now in the slot
                if (_equipment.GetItemInSlot(slot.Value.slotType, slot.Value.index) == itemToEquip)
                {
                    // Item equipped - remove from inventory
                    _inventory.RemoveItem(itemToEquip);
                    ConsumeEquipTurn();

                    // Check if mount failure occurred (item jammed)
                    if (_equipment.IsItemJammed(slot.Value.slotType, slot.Value.index))
                    {
                        SetStatus($"Mount failure! {itemToEquip.Name} jammed - both damaged!", ASCIIColors.AlertDanger);
                    }
                    else
                    {
                        SetStatus($"Equipped {itemToEquip.Name}", ASCIIColors.AlertSuccess);
                    }
                }
                else
                {
                    // Equip failed
                    SetStatus($"Cannot equip - check mount point or energy", ASCIIColors.AlertWarning);
                }
            }
            else
            {
                // Try to swap with first compatible slot
                var targetType = itemToEquip.SlotType == EquipmentSlotType.Any
                    ? EquipmentSlotType.Core : itemToEquip.SlotType;
                var previousItem = _equipment.Equip(itemToEquip, targetType, 0);

                // Check if item is now in the slot
                if (_equipment.GetItemInSlot(targetType, 0) == itemToEquip)
                {
                    // Item equipped - remove from inventory
                    _inventory.RemoveItem(itemToEquip);
                    if (previousItem != null)
                        _inventory.AddItem(previousItem);
                    ConsumeEquipTurn();

                    // Check if mount failure occurred (item jammed)
                    if (_equipment.IsItemJammed(targetType, 0))
                    {
                        SetStatus($"Mount failure! {itemToEquip.Name} jammed - both damaged!", ASCIIColors.AlertDanger);
                    }
                    else
                    {
                        SetStatus($"Equipped {itemToEquip.Name}", ASCIIColors.AlertSuccess);
                    }
                }
                else
                {
                    // Equip failed
                    SetStatus($"Cannot equip - check mount point or energy", ASCIIColors.AlertWarning);
                }
            }

            UpdateSelectedItem();
            Render();
        }
    }

    /// <summary>
    /// Consume a turn for equipping an item.
    /// </summary>
    private void ConsumeEquipTurn()
    {
        // Equipping costs one full turn (100 action points)
        // Note: This schedules the turn to be consumed when inventory closes
        // The turn is actually processed by TurnManager after returning to gameplay
        _pendingActionCost += ActionCosts.EquipItem;
        GD.Print($"[Inventory] Equip action queued: {ActionCosts.EquipItem} AP (total pending: {_pendingActionCost})");
    }

    // Track pending action costs from inventory operations
    private int _pendingActionCost = 0;

    /// <summary>
    /// Get and clear any pending action cost from inventory operations.
    /// Called by game controller when inventory closes.
    /// </summary>
    public int GetAndClearPendingActionCost()
    {
        int cost = _pendingActionCost;
        _pendingActionCost = 0;
        return cost;
    }

    private (EquipmentSlotType, int) FindItemSlot(Item item)
    {
        if (_equipment == null)
            return (EquipmentSlotType.Any, 0);

        // Check all slots for this item
        for (int i = 0; i < 2; i++)
        {
            if (_equipment.GetItemInSlot(EquipmentSlotType.Core, i) == item)
                return (EquipmentSlotType.Core, i);
            if (_equipment.GetItemInSlot(EquipmentSlotType.Utility, i) == item)
                return (EquipmentSlotType.Utility, i);
            if (_equipment.GetItemInSlot(EquipmentSlotType.Base, i) == item)
                return (EquipmentSlotType.Base, i);
        }
        return (EquipmentSlotType.Any, 0);
    }

    private void PerformUnequipAction()
    {
        if (_inventory == null || _equipment == null)
            return;

        if (_activePanel == Panel.Equipment && _selectedItem != null)
        {
            var (slotType, slotIndex) = GetSlotInfoAtIndex(_equipmentIndex);

            // Check if item is jammed in damaged mount
            if (_equipment.IsItemJammed(slotType, slotIndex))
            {
                SetStatus($"{_selectedItem.Name} is jammed! Repair mount first.", ASCIIColors.AlertDanger);
                Render();
                return;
            }

            if (_inventory.IsFull)
            {
                SetStatus("Inventory full!", ASCIIColors.AlertDanger);
                return;
            }

            var item = _equipment.Unequip(slotType, slotIndex);
            if (item != null)
            {
                _inventory.AddItem(item);
                SetStatus($"Unequipped {item.Name}", ASCIIColors.AlertInfo);
            }

            UpdateSelectedItem();
            Render();
        }
    }

    private void PerformDropAction()
    {
        if (_inventory == null)
            return;

        if (_activePanel == Panel.Inventory && _selectedItem != null)
        {
            _inventory.RemoveItem(_selectedItem);
            SetStatus($"Dropped {_selectedItem.Name}", ASCIIColors.AlertWarning);

            // Adjust index if needed
            if (_inventoryIndex >= _inventory.ItemCount && _inventoryIndex > 0)
                _inventoryIndex--;

            UpdateSelectedItem();
            Render();
        }
    }

    private void SetStatus(string message, Color color)
    {
        _statusMessage = message;
        _statusTimer = 2.0f;
        // Status color is handled in render
    }

    private void Render()
    {
        if (_display == null)
            return;

        // Clear buffer (we're using a smaller portion for the inventory overlay)
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                _buffer.SetCell(x, y, ' ', ASCIIColors.BgDark, ASCIIColors.BgDark);
            }
        }

        if (_comparisonMode && _selectedItem != null && _comparisonTarget != null)
        {
            DrawComparisonView();
        }
        else
        {
            DrawBorder();
            DrawInventoryPanel();
            DrawEquipmentPanel();
            DrawDetailsPanel();
        }
        DrawStatusBar();

        // Render to display
        _display.Text = RenderToBBCode();
    }

    private void DrawComparisonView()
    {
        if (_selectedItem == null || _comparisonTarget == null)
            return;

        int midX = ScreenWidth / 2;
        int leftWidth = midX - 2;
        int rightWidth = ScreenWidth - midX - 2;

        // Outer border
        _buffer.DrawBox(0, 0, ScreenWidth, ScreenHeight, ASCIIColors.Border, doubleLines: true);

        // Title
        string title = ">> COMPARE ITEMS <<";
        int titleX = (ScreenWidth - title.Length) / 2;
        _buffer.WriteString(titleX, 0, title, ASCIIColors.PrimaryBright);

        // Center divider
        for (int y = 1; y < ScreenHeight - 2; y++)
        {
            _buffer.SetCell(midX, y, ASCIIChars.BoxVD, ASCIIColors.Border);
        }
        _buffer.SetCell(midX, 0, ASCIIChars.BoxTeeTd, ASCIIColors.Border);
        _buffer.SetCell(midX, ScreenHeight - 1, ASCIIChars.BoxTeeBd, ASCIIColors.Border);

        // Left panel - Stashed item
        DrawComparisonItem(1, 2, leftWidth - 1, _selectedItem, "STASHED", true);

        // Right panel - Equipped item
        DrawComparisonItem(midX + 1, 2, rightWidth - 1, _comparisonTarget, "EQUIPPED", false);

        // Net change summary at bottom
        DrawComparisonSummary(1, ScreenHeight - 8, ScreenWidth - 2);
    }

    private void DrawComparisonItem(int x, int startY, int width, Item item, string label, bool isNew)
    {
        int y = startY;

        // Header
        var rarityColor = ASCIIColors.GetRarityColor(item.Rarity);
        string header = $"{label}: {item.GetDisplayName()}";
        if (header.Length > width)
            header = header[..(width - 3)] + "...";
        _buffer.WriteString(x, y++, header, rarityColor);

        string rarityLabel = $"[{item.Rarity}]";
        _buffer.WriteString(x + width - rarityLabel.Length, y - 1, rarityLabel, rarityColor);

        _buffer.WriteString(x, y++, new string('─', width), ASCIIColors.Border);

        // Energy info
        _buffer.WriteString(x, y++, $"Boot Cost:    {item.BootCost} NRG", ASCIIColors.TextNormal);

        string energyLine = "";
        if (item.EnergyConsumption > 0)
            energyLine = $"Energy Consumption: {item.EnergyConsumption}/turn";
        else if (item.EnergyOutput > 0)
            energyLine = $"Energy Output: +{item.EnergyOutput}/turn";
        if (!string.IsNullOrEmpty(energyLine))
            _buffer.WriteString(x, y++, energyLine, ASCIIColors.AlertInfo);

        _buffer.WriteString(x, y++, new string('─', width), ASCIIColors.Border);

        // Stats
        if (item.IsWeapon && item.WeaponData != null)
        {
            var weapon = item.WeaponData;
            _buffer.WriteString(x, y++, $"DAMAGE:   {weapon.DamageString}", ASCIIColors.TextNormal);
            _buffer.WriteString(x, y++, $"CRIT:     {weapon.CriticalChance}% (x{weapon.CriticalMultiplier:F1})", ASCIIColors.TextNormal);
            _buffer.WriteString(x, y++, $"RANGE:    {weapon.Range} tiles", ASCIIColors.TextNormal);
            if (weapon.BaseAccuracy > 0)
                _buffer.WriteString(x, y++, $"ACCURACY: {weapon.BaseAccuracy}%", ASCIIColors.TextNormal);
            if (weapon.EnergyCost > 0)
                _buffer.WriteString(x, y++, $"NRG/SHOT: {weapon.EnergyCost}", ASCIIColors.TextNormal);
            if (weapon.Cooldown > 0)
                _buffer.WriteString(x, y++, $"COOLDOWN: {weapon.Cooldown} turns", ASCIIColors.TextNormal);
            if (weapon.AreaRadius > 0)
                _buffer.WriteString(x, y++, $"RADIUS:   {weapon.AreaRadius}", ASCIIColors.TextNormal);
            if (weapon.PrimaryEffect != WeaponEffect.None)
                _buffer.WriteString(x, y++, $"EFFECT:   {weapon.PrimaryEffect} {weapon.EffectChance}%", ASCIIColors.TextNormal);
        }
        else
        {
            // Non-weapon stats
            if (item.BonusDamage != 0)
                _buffer.WriteString(x, y++, $"DMG:  {item.BonusDamage:+#;-#;0}", item.BonusDamage > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger);
            if (item.BonusArmor != 0)
                _buffer.WriteString(x, y++, $"ARM:  {item.BonusArmor:+#;-#;0}", item.BonusArmor > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger);
            if (item.BonusHealth != 0)
                _buffer.WriteString(x, y++, $"INT:  {item.BonusHealth:+#;-#;0}", item.BonusHealth > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger);
            if (item.BonusSightRange != 0)
                _buffer.WriteString(x, y++, $"VIS:  {item.BonusSightRange:+#;-#;0}", item.BonusSightRange > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger);
            if (item.BonusSpeed != 0)
                _buffer.WriteString(x, y++, $"SPD:  {item.BonusSpeed:+#;-#;0}", item.BonusSpeed > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger);
            if (item.EnergyOutput > 0)
                _buffer.WriteString(x, y++, $"PWR:  +{item.EnergyOutput}", ASCIIColors.AlertSuccess);
            if (item.BonusEnergyCapacity != 0)
                _buffer.WriteString(x, y++, $"CAP:  {item.BonusEnergyCapacity:+#;-#;0}", item.BonusEnergyCapacity > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger);
        }

        _buffer.WriteString(x, y++, new string('─', width), ASCIIColors.Border);

        // Module armor
        _buffer.WriteString(x, y++, $"Module Armor: {item.CurrentModuleArmor}/{item.MaxModuleArmor}", ASCIIColors.TextSecondary);
    }

    private void DrawComparisonSummary(int x, int startY, int width)
    {
        if (_selectedItem == null || _comparisonTarget == null || _equipment == null || _player?.AttributesComponent == null)
            return;

        int y = startY;
        _buffer.WriteString(x, y++, new string('═', width), ASCIIColors.Border);
        _buffer.WriteString(x, y++, "NET CHANGE IF EQUIPPED:", ASCIIColors.PrimaryBright);

        var preview = InventoryDataFormatter.CalculateEquipPreview(_selectedItem, _equipment, _player.AttributesComponent);

        // Key stat changes
        var changes = new List<string>();

        // Damage change (for weapons)
        if (_selectedItem.IsWeapon && _comparisonTarget.IsWeapon &&
            _selectedItem.WeaponData != null && _comparisonTarget.WeaponData != null)
        {
            float newAvg = _selectedItem.WeaponData.AverageDamage;
            float oldAvg = _comparisonTarget.WeaponData.AverageDamage;
            float diff = newAvg - oldAvg;
            if (Math.Abs(diff) > 0.5f)
            {
                var color = diff > 0 ? ASCIIColors.AlertSuccess : ASCIIColors.AlertDanger;
                changes.Add($"DMG: {diff:+#.#;-#.#;0}");
            }
        }

        // Energy consumption change
        int consumptionDiff = _selectedItem.EnergyConsumption - _comparisonTarget.EnergyConsumption;
        if (consumptionDiff != 0)
        {
            changes.Add($"CONSUMPTION: {consumptionDiff:+#;-#;0}/turn");
        }

        // Boot cost
        changes.Add($"BOOT: -{preview.BootCost} NRG (Have: {preview.CurrentEnergy})");

        string changeLine = string.Join("   ", changes);
        if (changeLine.Length > width)
            changeLine = changeLine[..(width - 3)] + "...";
        _buffer.WriteString(x, y++, changeLine, ASCIIColors.TextNormal);

        // Warnings
        if (!preview.CanAffordBoot)
        {
            _buffer.WriteString(x, y++, "WARNING: Not enough energy to boot!", ASCIIColors.AlertDanger);
        }
        else if (preview.WillCauseDeficit)
        {
            _buffer.WriteString(x, y++, "WARNING: Will cause energy deficit!", ASCIIColors.AlertWarning);
        }
    }

    private void DrawBorder()
    {
        // Outer border
        _buffer.DrawBox(0, 0, ScreenWidth, ScreenHeight, ASCIIColors.Border, doubleLines: true);

        // Title
        string title = ">> INVENTORY MANAGEMENT <<";
        int titleX = (ScreenWidth - title.Length) / 2;
        _buffer.WriteString(titleX, 0, title, ASCIIColors.PrimaryBright);

        // Panel separators (vertical lines with background)
        for (int y = 1; y < ScreenHeight - 1; y++)
        {
            _buffer.SetCell(Sep1X, y, ASCIIChars.BoxVD, ASCIIColors.Border, ASCIIColors.BgPanel);
            _buffer.SetCell(Sep2X, y, ASCIIChars.BoxVD, ASCIIColors.Border, ASCIIColors.BgPanel);
        }

        // Connectors at top and bottom
        _buffer.SetCell(Sep1X, 0, ASCIIChars.BoxTeeTd, ASCIIColors.Border);
        _buffer.SetCell(Sep2X, 0, ASCIIChars.BoxTeeTd, ASCIIColors.Border);
        _buffer.SetCell(Sep1X, ScreenHeight - 1, ASCIIChars.BoxTeeBd, ASCIIColors.Border);
        _buffer.SetCell(Sep2X, ScreenHeight - 1, ASCIIChars.BoxTeeBd, ASCIIColors.Border);

        // Panel headers with clear separation
        _buffer.DrawHorizontalLine(1, 1, InvPanelWidth, ASCIIColors.Border);
        _buffer.DrawHorizontalLine(EquipPanelStart, 1, EquipPanelWidth, ASCIIColors.Border);
        _buffer.DrawHorizontalLine(DetailsPanelStart, 1, DetailsPanelWidth, ASCIIColors.Border);
    }

    private void DrawInventoryPanel()
    {
        int x = InvPanelStart;
        int y = 2;
        int width = InvPanelWidth;

        // Clear panel area first
        for (int py = 2; py < ScreenHeight - 2; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                _buffer.SetCell(px, py, ' ', ASCIIColors.BgDark, ASCIIColors.BgDark);
            }
        }

        // Header with item count
        bool isActive = _activePanel == Panel.Inventory;
        var headerColor = isActive ? ASCIIColors.PrimaryBright : ASCIIColors.TextMuted;
        string header = _inventory != null
            ? $"INVENTORY [{_inventory.ItemCount}/{_inventory.MaxSlots}]"
            : "INVENTORY";
        _buffer.WriteString(x, y++, header, headerColor);
        _buffer.DrawHorizontalLine(x, y++, width, ASCIIColors.Border);

        if (_inventory == null)
        {
            _buffer.WriteString(x, y, "(no inventory)", ASCIIColors.TextMuted);
            return;
        }

        // Item list
        int itemY = y;
        int index = 0;
        foreach (var item in _inventory.Items)
        {
            char letter = (char)('a' + index);
            bool selected = isActive && index == _inventoryIndex;

            Color bgColor = selected ? ASCIIColors.Selection : ASCIIColors.BgDark;

            // Determine item color based on state
            Color fgColor;
            if (item.IsDisabled)
                fgColor = ASCIIColors.TextDisabled;
            else if (selected)
                fgColor = ASCIIColors.PrimaryBright;
            else
                fgColor = ASCIIColors.GetRarityColor(item.Rarity);

            // State prefix (* = disabled, ~ = inactive)
            string statePrefix = InventoryDataFormatter.GetItemStateIndicator(item);

            // Get module type abbreviation
            string typeAbbrev = InventoryDataFormatter.GetModuleTypeAbbrev(item.ModuleCategory);
            string typeSuffix = $"[{typeAbbrev}]";

            // Calculate available space for item name
            // Format: ">[a] Name... [TYPE]"
            int prefixLen = 4 + statePrefix.Length; // "> [a] " or "  [a] "
            int suffixLen = typeSuffix.Length + 1; // " [TYPE]"
            int maxNameLen = width - prefixLen - suffixLen;

            // Get display name with unidentified suffix
            string itemName = item.GetDisplayName();
            string idSuffix = InventoryDataFormatter.GetItemStateSuffix(item);
            itemName = statePrefix + itemName + idSuffix;

            // Truncate if needed
            if (itemName.Length > maxNameLen)
                itemName = itemName[..(maxNameLen - 3)] + "...";

            // Selection indicator
            string indicator = selected ? ">" : " ";
            _buffer.WriteString(x, itemY, indicator, ASCIIColors.Primary, bgColor);

            // Letter
            _buffer.WriteString(x + 1, itemY, $"[{letter}]", ASCIIColors.TextMuted, bgColor);

            // Item name
            _buffer.WriteString(x + 5, itemY, itemName, fgColor, bgColor);

            // Fill space between name and type
            int nameEndX = x + 5 + itemName.Length;
            int typeStartX = x + width - typeSuffix.Length;
            for (int fillX = nameEndX; fillX < typeStartX; fillX++)
            {
                _buffer.SetCell(fillX, itemY, ' ', fgColor, bgColor);
            }

            // Type suffix (right-aligned)
            var typeColor = item.IsDisabled ? ASCIIColors.TextDisabled : ASCIIColors.TextMuted;
            _buffer.WriteString(typeStartX, itemY, typeSuffix, typeColor, bgColor);

            itemY++;
            index++;

            if (itemY >= ScreenHeight - 4)
                break; // Leave room for status
        }

        // Empty slots indicator
        int emptySlots = _inventory.MaxSlots - _inventory.ItemCount;
        if (emptySlots > 0 && itemY < ScreenHeight - 4)
        {
            _buffer.WriteString(x, itemY, $"({emptySlots} empty slots)", ASCIIColors.TextDisabled);
        }
    }

    private void DrawEquipmentPanel()
    {
        int x = EquipPanelStart;
        int y = 2;
        int width = EquipPanelWidth;

        // Clear panel area first
        for (int py = 2; py < ScreenHeight - 2; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                _buffer.SetCell(px, py, ' ', ASCIIColors.BgDark, ASCIIColors.BgDark);
            }
        }

        // Header with energy balance
        bool isActive = _activePanel == Panel.Equipment;
        var headerColor = isActive ? ASCIIColors.PrimaryBright : ASCIIColors.TextMuted;
        _buffer.WriteString(x, y, "EQUIPMENT", headerColor);

        // Show net power in header
        if (_equipment != null)
        {
            int balance = _equipment.NetEnergyBalance;
            var (balanceStr, balanceColor) = InventoryDataFormatter.FormatEnergyBalance(balance);
            string powerStr = $"NET: {balance:+#;-#;0}";
            _buffer.WriteString(x + width - powerStr.Length - 1, y, powerStr, balanceColor);
        }
        y++;
        _buffer.DrawHorizontalLine(x, y++, width, ASCIIColors.Border);

        if (_equipment == null)
        {
            _buffer.WriteString(x, y, "(no equipment)", ASCIIColors.TextMuted);
            return;
        }

        // Draw ASCII art schematic
        DrawEquipmentSchematic(x, y);
        y += 6;

        // Separator
        _buffer.WriteString(x, y++, "SLOT DETAILS:", ASCIIColors.TextSecondary);

        // Draw slot list
        DrawEquipmentSlot(x, y++, 0, "1.C1", EquipmentSlotType.Core, 0, width, isActive);
        DrawEquipmentSlot(x, y++, 1, "2.C2", EquipmentSlotType.Core, 1, width, isActive);
        DrawEquipmentSlot(x, y++, 2, "3.U1", EquipmentSlotType.Utility, 0, width, isActive);
        DrawEquipmentSlot(x, y++, 3, "4.U2", EquipmentSlotType.Utility, 1, width, isActive);
        DrawEquipmentSlot(x, y++, 4, "5.B1", EquipmentSlotType.Base, 0, width, isActive);
        DrawEquipmentSlot(x, y++, 5, "6.B2", EquipmentSlotType.Base, 1, width, isActive);

        y++; // Spacing

        // Energy summary section
        DrawEnergySummary(x, y, width);
    }

    private void DrawEnergySummary(int x, int startY, int width)
    {
        if (_equipment == null || _player?.AttributesComponent == null)
            return;

        int y = startY;
        var attrs = _player.AttributesComponent;

        _buffer.WriteString(x, y++, new string('─', width - 1), ASCIIColors.Border);
        _buffer.WriteString(x, y++, "ENERGY SUMMARY:", ASCIIColors.TextSecondary);

        int barWidth = width - 22; // Leave room for labels

        // Output
        int maxOutput = 30; // Reasonable max for bar scaling
        string outputLabel = $"Output:  +{_equipment.TotalEnergyOutput}/turn ";
        string outputBar = InventoryDataFormatter.FormatEnergyBar(_equipment.TotalEnergyOutput, maxOutput, barWidth);
        _buffer.WriteString(x, y, outputLabel, ASCIIColors.AlertSuccess);
        _buffer.WriteString(x + outputLabel.Length, y++, outputBar, ASCIIColors.AlertSuccess);

        // Consumption
        string consumptionLabel = $"Consumes: -{_equipment.TotalEnergyConsumption}/turn ";
        string consumptionBar = InventoryDataFormatter.FormatEnergyBar(_equipment.TotalEnergyConsumption, maxOutput, barWidth);
        _buffer.WriteString(x, y, consumptionLabel, ASCIIColors.AlertDanger);
        _buffer.WriteString(x + consumptionLabel.Length, y++, consumptionBar, ASCIIColors.AlertDanger);

        // Balance
        int balance = _equipment.NetEnergyBalance;
        var (balanceText, balanceColor) = InventoryDataFormatter.FormatEnergyBalance(balance);
        string balanceLabel = $"Balance: {balance:+#;-#;0}/turn ";
        _buffer.WriteString(x, y, balanceLabel, balanceColor);
        string statusTag = balance > 0 ? "[SURPLUS]" : (balance < 0 ? "[DEFICIT!]" : "[BALANCED]");
        _buffer.WriteString(x + balanceLabel.Length, y++, statusTag, balanceColor);

        // Reserve
        int reserve = attrs.CurrentEnergyReserve;
        int maxReserve = attrs.EnergyReserveCapacity;
        string reserveLabel = $"Reserve: {reserve}/{maxReserve} ";
        string reserveBar = InventoryDataFormatter.FormatEnergyBar(reserve, maxReserve, barWidth);
        float reservePct = maxReserve > 0 ? (float)reserve / maxReserve : 0;
        var reserveColor = reservePct > 0.5f ? ASCIIColors.AlertSuccess :
                          (reservePct > 0.25f ? ASCIIColors.AlertWarning : ASCIIColors.AlertDanger);
        _buffer.WriteString(x, y, reserveLabel, reserveColor);
        _buffer.WriteString(x + reserveLabel.Length, y++, reserveBar, reserveColor);
    }

    private void DrawEquipmentSchematic(int startX, int startY)
    {
        if (_equipment == null)
            return;

        int x = startX;
        int y = startY;

        // Get modules for each slot
        var c1 = _equipment.GetItemInSlot(EquipmentSlotType.Core, 0);
        var c2 = _equipment.GetItemInSlot(EquipmentSlotType.Core, 1);
        var u1 = _equipment.GetItemInSlot(EquipmentSlotType.Utility, 0);
        var u2 = _equipment.GetItemInSlot(EquipmentSlotType.Utility, 1);
        var b1 = _equipment.GetItemInSlot(EquipmentSlotType.Base, 0);
        var b2 = _equipment.GetItemInSlot(EquipmentSlotType.Base, 1);

        // Row 1: Core section top border
        _buffer.WriteString(x, y, "     .-----+------.     ", ASCIIColors.Border);
        y++;

        // Row 2: Core modules
        _buffer.WriteString(x, y, "     |", ASCIIColors.Border);
        DrawModuleBox(x + 6, y, c1, EquipmentSlotType.Core, 0);
        _buffer.WriteString(x + 11, y, "  ", ASCIIColors.Border);
        DrawModuleBox(x + 13, y, c2, EquipmentSlotType.Core, 1);
        _buffer.WriteString(x + 18, y, "|     ", ASCIIColors.Border);
        y++;

        // Row 3: Core to Utility connector with AI core
        _buffer.WriteString(x, y, "     '---. ", ASCIIColors.Border);
        _buffer.SetCell(x + 11, y, '@', ASCIIColors.Player);
        _buffer.WriteString(x + 12, y, " .---'     ", ASCIIColors.Border);
        y++;

        // Row 4: Utility modules (arms)
        _buffer.WriteString(x, y, "    ", ASCIIColors.Border);
        DrawModuleBox(x + 4, y, u1, EquipmentSlotType.Utility, 0, '<', '>');
        _buffer.WriteString(x + 9, y, "-----", ASCIIColors.TextDisabled);
        DrawModuleBox(x + 14, y, u2, EquipmentSlotType.Utility, 1, '<', '>');
        _buffer.WriteString(x + 19, y, "    ", ASCIIColors.Border);
        y++;

        // Row 5: Utility to Base connector
        _buffer.WriteString(x, y, "     .---+-+---.     ", ASCIIColors.Border);
        y++;

        // Row 6: Base modules
        _buffer.WriteString(x, y, "     ", ASCIIColors.Border);
        DrawModuleBox(x + 5, y, b1, EquipmentSlotType.Base, 0);
        _buffer.WriteString(x + 10, y, "   ", ASCIIColors.Border);
        DrawModuleBox(x + 13, y, b2, EquipmentSlotType.Base, 1);
        _buffer.WriteString(x + 18, y, "     ", ASCIIColors.Border);
    }

    /// <summary>
    /// Draw a module box with type icon and state indicators.
    /// </summary>
    private void DrawModuleBox(int x, int y, Item? item, EquipmentSlotType slotType, int slotIndex, char leftBracket = '[', char rightBracket = ']')
    {
        Color slotColor = ASCIIColors.GetSlotColor(slotType);

        if (item == null)
        {
            // Empty slot - show dots
            _buffer.WriteString(x, y, $"{leftBracket}...{rightBracket}", ASCIIColors.TextDisabled);
            return;
        }

        // Get module icon (3 chars)
        string icon = GetModuleIcon(item.ModuleCategory);

        // Check states
        bool isJammed = _equipment?.IsItemJammed(slotType, slotIndex) ?? false;
        bool isSelected = _activePanel == Panel.Equipment && _equipmentIndex == GetSlotIndex(slotType, slotIndex);

        // Determine color based on state
        Color color;
        if (item.IsDisabled)
            color = ASCIIColors.AlertDanger;
        else if (!item.IsActive && item.IsToggleable)
            color = ASCIIColors.TextMuted;
        else if (isSelected)
            color = ASCIIColors.PrimaryBright;
        else
            color = slotColor;

        // Draw module with state indicators
        if (isJammed)
        {
            // Jammed - show exclamation marks
            _buffer.WriteString(x, y, $"!{icon}!", ASCIIColors.AlertDanger);
        }
        else if (item.IsDisabled)
        {
            // Disabled - show X's
            _buffer.WriteString(x, y, $"{leftBracket}XXX{rightBracket}", color);
        }
        else if (item.IsToggleable && !item.IsActive)
        {
            // Inactive toggleable - show dashes
            _buffer.WriteString(x, y, $"-{icon}-", color);
        }
        else
        {
            // Normal - show icon with brackets
            _buffer.WriteString(x, y, $"{leftBracket}{icon}{rightBracket}", color);
        }
    }

    /// <summary>
    /// Get a 3-character icon for the module type.
    /// </summary>
    private static string GetModuleIcon(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Logic => "CPU",
            ModuleType.Battery => "BAT",
            ModuleType.Generator => "GEN",
            ModuleType.Weapon => "WPN",
            ModuleType.Sensor => "SEN",
            ModuleType.Shield => "SHL",
            ModuleType.Treads => "TRD",
            ModuleType.Legs => "LEG",
            ModuleType.Flight => "FLY",
            ModuleType.Cargo => "CRG",
            _ => "MOD"
        };
    }

    /// <summary>
    /// Get the linear slot index from slot type and index.
    /// </summary>
    private static int GetSlotIndex(EquipmentSlotType slotType, int slotIndex)
    {
        return slotType switch
        {
            EquipmentSlotType.Core => slotIndex,
            EquipmentSlotType.Utility => 2 + slotIndex,
            EquipmentSlotType.Base => 4 + slotIndex,
            _ => 0
        };
    }

    private void DrawEquipmentSlot(int x, int y, int index, string label, EquipmentSlotType slotType, int slotIndex, int width, bool panelActive)
    {
        var item = _equipment?.GetItemInSlot(slotType, slotIndex);
        bool selected = panelActive && index == _equipmentIndex;
        bool isJammed = _equipment?.IsItemJammed(slotType, slotIndex) ?? false;

        Color slotColor = ASCIIColors.GetSlotColor(slotType);
        Color bgColor = selected ? ASCIIColors.Selection : ASCIIColors.BgDark;

        // Selection indicator
        string indicator = selected ? ">" : " ";
        _buffer.WriteString(x, y, indicator, ASCIIColors.Primary, bgColor);

        // Label with bracket - use ! marks if jammed
        string slotId = label[2..]; // Extract C1, C2, U1, etc.
        if (isJammed)
        {
            // Jammed mount point - show with exclamation marks
            _buffer.WriteString(x + 1, y, $"!{slotId}!", ASCIIColors.AlertDanger, bgColor);
        }
        else
        {
            _buffer.WriteString(x + 1, y, $"[{slotId}]", slotColor, bgColor);
        }

        if (item == null)
        {
            // Empty slot (can still be damaged even if empty)
            string emptyText = isJammed ? "[DAMAGED]" : "[empty]";
            var emptyColor = isJammed ? ASCIIColors.AlertDanger : ASCIIColors.TextDisabled;
            _buffer.WriteString(x + 6, y, emptyText, emptyColor, bgColor);

            // Fill rest of line
            int endX = x + width - 1;
            for (int fillX = x + 6 + emptyText.Length; fillX <= endX; fillX++)
            {
                _buffer.SetCell(fillX, y, ' ', emptyColor, bgColor);
            }
            return;
        }

        // Item name - show jammed state
        Color itemColor;
        if (isJammed)
            itemColor = ASCIIColors.AlertDanger;
        else if (item.IsDisabled)
            itemColor = ASCIIColors.TextDisabled;
        else
            itemColor = ASCIIColors.GetRarityColor(item.Rarity);

        string itemName = item.GetDisplayName();

        // Add JAMMED prefix if applicable
        if (isJammed)
            itemName = "JAMMED: " + itemName;

        // Get short stats for inline display
        string shortStats = item.GetShortStatsString();

        // Calculate space: Label (5) + Name + Stats
        int statsLen = shortStats.Length > 0 ? shortStats.Length + 2 : 0; // +2 for spacing
        int maxNameLen = width - 7 - statsLen;

        if (itemName.Length > maxNameLen)
            itemName = itemName[..(maxNameLen - 2)] + "..";

        // Write item name
        _buffer.WriteString(x + 6, y, itemName, itemColor, bgColor);

        // Fill space between name and stats
        int nameEndX = x + 6 + itemName.Length;
        int statsStartX = x + width - shortStats.Length - 1;

        for (int fillX = nameEndX; fillX < statsStartX; fillX++)
        {
            _buffer.SetCell(fillX, y, ' ', itemColor, bgColor);
        }

        // Write stats (right-aligned)
        if (shortStats.Length > 0)
        {
            var statsColor = item.IsDisabled ? ASCIIColors.TextDisabled : ASCIIColors.TextMuted;
            _buffer.WriteString(statsStartX, y, shortStats, statsColor, bgColor);
        }

        // Fill to end
        for (int fillX = x + width - 1; fillX <= x + width - 1; fillX++)
        {
            _buffer.SetCell(fillX, y, ' ', itemColor, bgColor);
        }
    }

    private void DrawDetailsPanel()
    {
        int x = DetailsPanelStart;
        int y = 2;
        int width = DetailsPanelWidth;

        // Clear panel area first
        for (int py = 2; py < ScreenHeight - 2; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                _buffer.SetCell(px, py, ' ', ASCIIColors.BgDark, ASCIIColors.BgDark);
            }
        }

        // Header
        _buffer.WriteString(x, y++, "ITEM DETAILS", ASCIIColors.PrimaryBright);
        _buffer.DrawHorizontalLine(x, y++, width, ASCIIColors.Border);

        if (_selectedItem == null)
        {
            _buffer.WriteString(x, y++, "No item selected", ASCIIColors.TextMuted);
            y++;
            _buffer.WriteString(x, y++, "Select an item", ASCIIColors.TextDisabled);
            _buffer.WriteString(x, y++, "to view details.", ASCIIColors.TextDisabled);
            return;
        }

        // Helper to truncate text to panel width
        string Truncate(string text) => text.Length > width ? text[..(width - 3)] + "..." : text;

        // Item name with rarity color and diamond marker
        var rarityColor = ASCIIColors.GetRarityColor(_selectedItem.Rarity);
        string displayName = _selectedItem.GetDisplayName();
        string suffix = InventoryDataFormatter.GetItemStateSuffix(_selectedItem);
        string nameWithPrefix = "◆ " + displayName + suffix;
        // Truncate to width - 1 to stay within panel bounds
        if (nameWithPrefix.Length > width - 1)
            nameWithPrefix = nameWithPrefix[..(width - 4)] + "...";
        _buffer.WriteString(x, y++, nameWithPrefix, rarityColor);

        // Separator line
        _buffer.WriteString(x, y++, new string('═', width - 1), ASCIIColors.Border);

        // Type and slot info
        string typeStr = $"Type: {InventoryDataFormatter.GetModuleTypeAbbrev(_selectedItem.ModuleCategory)}";
        string slotStr = $"Slot: {_selectedItem.SlotType}";
        _buffer.WriteString(x, y, typeStr, ASCIIColors.TextNormal);
        _buffer.WriteString(x + 24, y++, slotStr, ASCIIColors.GetSlotColor(_selectedItem.SlotType));

        // Energy costs (always visible)
        string energyInfo = InventoryDataFormatter.FormatEnergyCost(_selectedItem);
        _buffer.WriteString(x, y++, Truncate(energyInfo), ASCIIColors.AlertInfo);

        // Toggleable state
        if (_selectedItem.IsToggleable)
        {
            string toggleState = _selectedItem.IsActive ? "[ON]" : "[OFF]";
            var toggleColor = _selectedItem.IsActive ? ASCIIColors.AlertSuccess : ASCIIColors.AlertWarning;
            _buffer.WriteString(x, y++, $"Toggleable: {toggleState}", toggleColor);
        }

        // Disabled warning
        if (_selectedItem.IsDisabled)
        {
            _buffer.WriteString(x, y++, "[DISABLED - Armor Depleted]", ASCIIColors.AlertDanger);
        }

        // Separator
        _buffer.WriteString(x, y++, new string('─', width - 1), ASCIIColors.Border);

        // Stats section - weapon or module
        if (_selectedItem.IsIdentified || _selectedItem.Type == ItemType.Consumable)
        {
            if (_selectedItem.IsWeapon && _selectedItem.WeaponData != null)
            {
                // Full weapon stats
                var weaponLines = InventoryDataFormatter.FormatWeaponStats(_selectedItem.WeaponData, width - 1);
                foreach (var line in weaponLines)
                {
                    if (y >= ScreenHeight - 12)
                        break;
                    _buffer.WriteString(x, y++, Truncate(line), ASCIIColors.TextNormal);
                }
            }
            else
            {
                // Module stats
                var statLines = InventoryDataFormatter.FormatModuleStats(_selectedItem, width - 1);
                foreach (var line in statLines)
                {
                    if (y >= ScreenHeight - 12)
                        break;
                    _buffer.WriteString(x, y++, Truncate(line), ASCIIColors.TextNormal);
                }
            }
        }
        else
        {
            // Unidentified - show limited info
            _buffer.WriteString(x, y++, "[UNIDENTIFIED]", ASCIIColors.AlertWarning);
            _buffer.WriteString(x, y++, "Mount or use Analyzer", ASCIIColors.TextMuted);
            _buffer.WriteString(x, y++, "to reveal stats.", ASCIIColors.TextMuted);
        }

        // Separator before module armor
        _buffer.WriteString(x, y++, new string('─', width - 1), ASCIIColors.Border);

        // Module armor bar
        if (_selectedItem.MaxModuleArmor > 0)
        {
            _buffer.WriteString(x, y++, "MODULE ARMOR:", ASCIIColors.TextSecondary);
            string healthBar = InventoryDataFormatter.FormatModuleHealthBar(
                _selectedItem.CurrentModuleArmor,
                _selectedItem.MaxModuleArmor,
                width - 2);
            var healthColor = InventoryDataFormatter.GetHealthBarColor(
                _selectedItem.CurrentModuleArmor,
                _selectedItem.MaxModuleArmor);
            _buffer.WriteString(x, y++, healthBar, healthColor);
        }

        // Separator before description
        _buffer.WriteString(x, y++, new string('─', width - 1), ASCIIColors.Border);

        // Description (word wrapped)
        string description = _selectedItem.GetDisplayDescription();
        var descLines = WrapText(description, width - 2);
        foreach (var line in descLines)
        {
            if (y >= ScreenHeight - 6)
                break;
            _buffer.WriteString(x, y++, $"\"{line}\"", ASCIIColors.TextSecondary);
        }

        // Equip preview box (if in inventory panel and item selected)
        if (_activePanel == Panel.Inventory && _equipment != null && _player != null)
        {
            DrawEquipPreview(x, y + 1, width);
        }
    }

    private void DrawEquipPreview(int x, int startY, int width)
    {
        if (_selectedItem == null || _equipment == null || _player?.AttributesComponent == null)
            return;

        var preview = InventoryDataFormatter.CalculateEquipPreview(
            _selectedItem, _equipment, _player.AttributesComponent);

        int y = startY;

        // Preview box
        _buffer.WriteString(x, y++, $"┌─ EQUIP PREVIEW {new string('─', width - 18)}┐", ASCIIColors.Border);

        // Boot cost
        string bootLine = $"│ Boot Cost: -{preview.BootCost} NRG (Have: {preview.CurrentEnergy})";
        bootLine = bootLine.PadRight(width - 1) + "│";
        var bootColor = preview.CanAffordBoot ? ASCIIColors.TextNormal : ASCIIColors.AlertDanger;
        _buffer.WriteString(x, y++, bootLine, bootColor);

        // Balance change
        var (balanceText, balanceColor) = InventoryDataFormatter.FormatEnergyBalance(preview.NewBalance);
        string balanceLine = $"│ New Balance: {preview.CurrentBalance:+#;-#;0} → {preview.NewBalance:+#;-#;0}/turn";
        balanceLine = balanceLine.PadRight(width - 1) + "│";
        _buffer.WriteString(x, y++, balanceLine, balanceColor);

        // Warnings
        if (!preview.CanAffordBoot)
        {
            string warnLine = "│ WARNING: Not enough energy!";
            warnLine = warnLine.PadRight(width - 1) + "│";
            _buffer.WriteString(x, y++, warnLine, ASCIIColors.AlertDanger);
        }
        else if (preview.WillCauseDeficit)
        {
            string warnLine = "│ WARNING: Will cause deficit!";
            warnLine = warnLine.PadRight(width - 1) + "│";
            _buffer.WriteString(x, y++, warnLine, ASCIIColors.AlertWarning);
        }

        // Displaced item
        if (preview.DisplacedItem != null)
        {
            string dispLine = $"│ Replaces: {preview.DisplacedItem.Name}";
            if (dispLine.Length > width - 1)
                dispLine = dispLine[..(width - 4)] + "...";
            dispLine = dispLine.PadRight(width - 1) + "│";
            _buffer.WriteString(x, y++, dispLine, ASCIIColors.AlertWarning);
        }

        _buffer.WriteString(x, y++, $"└{new string('─', width - 2)}┘", ASCIIColors.Border);
    }

    private void DrawStatusBar()
    {
        int y = ScreenHeight - 2;

        // Separator
        _buffer.DrawHorizontalLine(1, y - 1, ScreenWidth - 2, ASCIIColors.Border);

        // Status message or help
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            _buffer.WriteString(2, y, _statusMessage, ASCIIColors.AlertInfo);
        }
        else
        {
            // Help text based on active panel and context
            string help;
            if (_comparisonMode)
            {
                help = "[E]quip & Replace [Esc]Back";
            }
            else if (_activePanel == Panel.Inventory)
            {
                help = "[E]quip [X]Drop [C]ompare [Tab]Switch [Esc]Close";
            }
            else
            {
                // Equipment panel
                bool canToggle = _selectedItem?.IsToggleable ?? false;
                help = canToggle
                    ? "[U]nequip [T]oggle [Tab]Switch [Esc]Close"
                    : "[U]nequip [Tab]Switch [Esc]Close";
            }
            _buffer.WriteString(2, y, help, ASCIIColors.TextMuted);
        }

        // Navigation hint
        string nav = "WASD/Arrows:Move 1-6:Slots";
        _buffer.WriteString(ScreenWidth - nav.Length - 2, y, nav, ASCIIColors.TextDisabled);
    }

    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 <= maxWidth)
            {
                if (currentLine.Length > 0)
                    currentLine += " ";
                currentLine += word;
            }
            else
            {
                if (currentLine.Length > 0)
                    lines.Add(currentLine);
                currentLine = word.Length <= maxWidth ? word : word[..maxWidth];
            }
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine);

        return lines;
    }

    private string RenderToBBCode()
    {
        var sb = new System.Text.StringBuilder(ScreenWidth * ScreenHeight * 30);

        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                var cell = _buffer.GetCell(x, y);
                var fgHex = cell.GetForegroundHex();
                var character = cell.Character;

                string charStr = character switch
                {
                    '[' => "[lb]",
                    ']' => "[rb]",
                    _ => character.ToString()
                };

                sb.Append($"[color=#{fgHex}]{charStr}[/color]");
            }
            if (y < ScreenHeight - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }
}
