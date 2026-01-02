using System.Collections.Generic;
using Godot;
using NullAndVoid.Components;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Rendering;

namespace NullAndVoid.UI;

/// <summary>
/// Renders the weapon hotkey bar showing equipped weapons and their status.
/// </summary>
public class WeaponBar
{
    private readonly ASCIIBuffer _buffer;
    private Player? _player;
    private int _selectedSlot = 0;
    private bool _isTargeting = false;

    // Layout constants - using ASCIIBuffer layout regions
    private const int BAR_Y = ASCIIBuffer.WeaponBarY;  // Row 47
    private const int BAR_X = ASCIIBuffer.MapStartX + 1;
    private const int SLOT_WIDTH = 22;
    private const int MAX_SLOTS = 6;  // Show 6 weapons at a time (wider screen)

    // Colors
    private static readonly Color _slotBorder = new(0.4f, 0.4f, 0.4f);
    private static readonly Color _slotBackground = new(0.1f, 0.1f, 0.15f);
    private static readonly Color _slotSelected = new(0.3f, 0.5f, 0.3f);
    private static readonly Color _slotCooldown = new(0.3f, 0.2f, 0.2f);
    private static readonly Color _textHotkey = new(1.0f, 0.9f, 0.3f);
    private static readonly Color _textDamage = new(1.0f, 0.4f, 0.4f);
    private static readonly Color _textRange = new(0.4f, 0.8f, 1.0f);

    public WeaponBar(ASCIIBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Set the player reference.
    /// </summary>
    public void SetPlayer(Player player)
    {
        _player = player;
    }

    /// <summary>
    /// Set the currently selected weapon slot (0 = none).
    /// </summary>
    public void SetSelectedSlot(int slot, bool isTargeting)
    {
        _selectedSlot = slot;
        _isTargeting = isTargeting;
    }

    /// <summary>
    /// Render the weapon bar.
    /// </summary>
    public void Render()
    {
        if (_player?.EquipmentComponent == null)
            return;

        // Draw weapon slots - show hotkeys 1-4 with their actual weapons
        int x = BAR_X;
        for (int slot = 1; slot <= MAX_SLOTS; slot++)
        {
            // Get weapon at this specific hotkey slot
            Item? weapon = _player.EquipmentComponent.GetWeaponByHotkey(slot);
            RenderSlot(x, BAR_Y, slot, weapon);
            x += SLOT_WIDTH + 1;
        }
    }

    /// <summary>
    /// Render a single weapon slot.
    /// </summary>
    private void RenderSlot(int x, int y, int slotNumber, Item? weapon)
    {
        // Determine slot background color
        Color bgColor = _slotBackground;
        if (slotNumber == _selectedSlot && _isTargeting)
        {
            bgColor = _slotSelected;
        }
        else if (weapon != null && !weapon.IsWeaponReady)
        {
            bgColor = _slotCooldown;
        }

        // Draw slot background
        for (int dy = 0; dy < 2; dy++)
        {
            for (int dx = 0; dx < SLOT_WIDTH; dx++)
            {
                _buffer.SetCell(x + dx, y + dy, ' ', ASCIIColors.TextNormal);
                _buffer.SetCellBackground(x + dx, y + dy, bgColor);
            }
        }

        // Draw slot border brackets
        _buffer.SetCell(x, y, '[', _slotBorder);
        _buffer.SetCell(x + SLOT_WIDTH - 1, y, ']', _slotBorder);

        // Draw hotkey number
        _buffer.SetCell(x + 1, y, (char)('0' + slotNumber), _textHotkey);

        if (weapon == null)
        {
            // Empty slot
            _buffer.DrawString(x + 3, y, "-empty-", ASCIIColors.TextDim);
            return;
        }

        // Weapon name (truncated if needed)
        string name = weapon.Name;
        if (name.Length > 12)
            name = name[..11] + ".";
        _buffer.DrawString(x + 3, y, name, weapon.DisplayColor);

        // Second line: stats
        var weaponData = weapon.WeaponData;
        if (weaponData != null)
        {
            // Damage
            string dmg = $"D{weaponData.DamageString}";
            _buffer.DrawString(x + 1, y + 1, dmg, _textDamage);

            // Range
            string rng = $"R{weaponData.Range}";
            _buffer.DrawString(x + 8, y + 1, rng, _textRange);

            // Cooldown indicator
            if (!weapon.IsWeaponReady)
            {
                string cd = $"({weaponData.CurrentCooldown}t)";
                _buffer.DrawString(x + 12, y + 1, cd, ASCIIColors.AlertWarning);
            }
        }
    }

}
