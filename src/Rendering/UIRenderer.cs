using System.Collections.Generic;
using Godot;
using NullAndVoid.Components;
using NullAndVoid.Entities;
using NullAndVoid.Items;

namespace NullAndVoid.Rendering;

/// <summary>
/// Renders all UI elements (messages, sidebar, status bar) to the ASCII buffer.
/// </summary>
public class UIRenderer
{
    private readonly ASCIIBuffer _buffer;

    // Message log
    private readonly Queue<(string text, Color color)> _messages = new();
    private const int MaxMessages = 2;  // 2 visible message lines

    // Nearby entities tracking
    private readonly List<(char symbol, string name, Color color)> _nearbyEntities = new();
    private const int MaxNearby = 5;

    public UIRenderer(ASCIIBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Render all UI elements.
    /// </summary>
    public void Render(Player? player, int turnNumber)
    {
        RenderMessageArea();
        RenderSidebar(player, turnNumber);
        RenderStatusBar(turnNumber);
        RenderMapBorder();
    }

    /// <summary>
    /// Add a message to the log.
    /// </summary>
    public void AddMessage(string text, Color? color = null)
    {
        _messages.Enqueue((text, color ?? ASCIIColors.TextSecondary));

        while (_messages.Count > MaxMessages)
        {
            _messages.Dequeue();
        }
    }

    /// <summary>
    /// Clear all messages.
    /// </summary>
    public void ClearMessages()
    {
        _messages.Clear();
    }

    /// <summary>
    /// Update nearby entities list.
    /// </summary>
    public void UpdateNearby(IEnumerable<(char symbol, string name, Color color)> entities)
    {
        _nearbyEntities.Clear();
        int count = 0;
        foreach (var entity in entities)
        {
            if (count >= MaxNearby)
                break;
            _nearbyEntities.Add(entity);
            count++;
        }
    }

    private void RenderMessageArea()
    {
        // Top border
        _buffer.DrawHorizontalLine(0, 0, ASCIIBuffer.Width, ASCIIColors.Border);

        // Message lines (2 lines of actual messages)
        int y = 1;
        foreach (var (text, color) in _messages)
        {
            string displayText = $"> {text}";
            // Truncate if too long
            if (displayText.Length > ASCIIBuffer.Width - 2)
            {
                displayText = displayText[..(ASCIIBuffer.Width - 5)] + "...";
            }
            _buffer.WriteString(1, y, displayText, color);
            y++;
            if (y >= ASCIIBuffer.MessageSeparatorY)
                break; // Don't overflow into separator
        }

        // Fill empty message lines
        for (; y < ASCIIBuffer.MessageSeparatorY; y++)
        {
            _buffer.WriteString(1, y, "", ASCIIColors.TextMuted);
        }

        // Bottom border of message area (separator line)
        _buffer.DrawHorizontalLine(0, ASCIIBuffer.MessageSeparatorY, ASCIIBuffer.Width, ASCIIColors.Border);
    }

    private void RenderMapBorder()
    {
        // Vertical separator between map and sidebar
        int separatorX = ASCIIBuffer.MapSeparatorX;

        // Draw vertical line from map start to weapon bar separator
        for (int y = ASCIIBuffer.MapStartY; y < ASCIIBuffer.WeaponBarSeparatorY; y++)
        {
            _buffer.SetCell(separatorX, y, ASCIIChars.BoxV, ASCIIColors.Border);
        }

        // Corner connectors
        _buffer.SetCell(separatorX, ASCIIBuffer.MessageSeparatorY, ASCIIChars.BoxTeeT, ASCIIColors.Border);
        _buffer.SetCell(separatorX, ASCIIBuffer.WeaponBarSeparatorY, ASCIIChars.BoxTeeB, ASCIIColors.Border);

        // Weapon bar separator line (full width)
        _buffer.DrawHorizontalLine(0, ASCIIBuffer.WeaponBarSeparatorY, ASCIIBuffer.Width, ASCIIColors.Border);
    }

    private void RenderSidebar(Player? player, int turnNumber)
    {
        int x = ASCIIBuffer.SidebarX;
        int y = ASCIIBuffer.MapStartY;
        int width = ASCIIBuffer.SidebarWidth;

        // Status header
        _buffer.WriteCenteredInRegion(x, width, y++, ">> STATUS <<", ASCIIColors.PrimaryBright);
        _buffer.DrawHorizontalLine(x, y++, width, ASCIIColors.Border);

        if (player?.AttributesComponent != null)
        {
            var attrs = player.AttributesComponent;
            int barWidth = width - 7; // "XX: " + padding

            // Integrity (Health) bar
            float integrityPercent = attrs.IntegrityPercent;
            string integrityBar = ASCIIChars.ProgressBar(integrityPercent, barWidth);
            _buffer.WriteString(x + 1, y++, $"INT:{integrityBar}", ASCIIColors.GetHealthColor(integrityPercent));
            _buffer.WriteString(x + 1, y++, $"    {attrs.CurrentIntegrity}/{attrs.MaxIntegrity}", ASCIIColors.TextSecondary);

            // Energy Reserve bar
            float energyPercent = attrs.EnergyReservePercent;
            string energyBar = ASCIIChars.ProgressBar(energyPercent, barWidth);
            Color energyColor = attrs.HasEnergyDeficit ? ASCIIColors.Warning : ASCIIColors.Energy;
            _buffer.WriteString(x + 1, y++, $"PWR:{energyBar}", energyColor);

            // Energy balance line
            string balanceSign = attrs.EnergyBalance >= 0 ? "+" : "";
            string balanceText = $"{attrs.CurrentEnergyReserve}/{attrs.EnergyReserveCapacity} ({balanceSign}{attrs.EnergyBalance}/t)";
            Color balanceColor = attrs.HasEnergyDeficit ? ASCIIColors.Warning : ASCIIColors.TextSecondary;
            _buffer.WriteString(x + 1, y++, $"    {balanceText}", balanceColor);

            // Stats line 1: Combat
            _buffer.WriteString(x + 1, y++, $"DMG:{attrs.AttackDamage,2} ARM:{attrs.Armor,2} VIS:{attrs.SightRange,2}", ASCIIColors.TextSecondary);

            // Stats line 2: Mobility/Detection
            _buffer.WriteString(x + 1, y++, $"SPD:{attrs.Speed,3} NSE:{attrs.Noise,2}", ASCIIColors.TextSecondary);
        }
        else if (player != null)
        {
            // Fallback to legacy display
            float healthPercent = player.MaxHealth > 0 ? (float)player.CurrentHealth / player.MaxHealth : 0;
            int barWidth = width - 7;
            string healthBar = ASCIIChars.ProgressBar(healthPercent, barWidth);
            _buffer.WriteString(x + 1, y++, $"INT:{healthBar}", ASCIIColors.GetHealthColor(healthPercent));
            _buffer.WriteString(x + 1, y++, $"    {player.CurrentHealth}/{player.MaxHealth}", ASCIIColors.TextSecondary);
            y += 4; // Skip energy lines
        }
        else
        {
            y += 6;
        }

        y++; // Spacing

        // Equipment header
        _buffer.DrawHorizontalLine(x, y++, width, ASCIIColors.Border);
        _buffer.WriteCenteredInRegion(x, width, y++, ">> EQUIPMENT <<", ASCIIColors.PrimaryBright);

        if (player?.EquipmentComponent != null)
        {
            RenderEquipmentSlots(x, ref y, width, player.EquipmentComponent);
        }
        else
        {
            y += 6; // Space for equipment
        }

        y++; // Spacing

        // Nearby header
        _buffer.DrawHorizontalLine(x, y++, width, ASCIIColors.Border);
        _buffer.WriteCenteredInRegion(x, width, y++, ">> NEARBY <<", ASCIIColors.PrimaryBright);

        // Nearby entities
        if (_nearbyEntities.Count > 0)
        {
            foreach (var (symbol, name, color) in _nearbyEntities)
            {
                string truncatedName = name.Length > width - 4 ? name[..(width - 7)] + "..." : name;
                _buffer.SetCell(x + 1, y, symbol, color);
                _buffer.WriteString(x + 3, y++, truncatedName, color);
            }
        }
        else
        {
            _buffer.WriteString(x + 1, y++, "(nothing)", ASCIIColors.TextMuted);
        }
    }

    private void RenderEquipmentSlots(int x, ref int y, int width, Equipment equipment)
    {
        // Core slots (2)
        RenderEquipmentSlot(x, y++, width, "C1", equipment.GetItemInSlot(EquipmentSlotType.Core, 0), ASCIIColors.SlotCore);
        RenderEquipmentSlot(x, y++, width, "C2", equipment.GetItemInSlot(EquipmentSlotType.Core, 1), ASCIIColors.SlotCore);

        // Utility slots (2)
        RenderEquipmentSlot(x, y++, width, "U1", equipment.GetItemInSlot(EquipmentSlotType.Utility, 0), ASCIIColors.SlotUtility);
        RenderEquipmentSlot(x, y++, width, "U2", equipment.GetItemInSlot(EquipmentSlotType.Utility, 1), ASCIIColors.SlotUtility);

        // Base slots (2)
        RenderEquipmentSlot(x, y++, width, "B1", equipment.GetItemInSlot(EquipmentSlotType.Base, 0), ASCIIColors.SlotBase);
        RenderEquipmentSlot(x, y++, width, "B2", equipment.GetItemInSlot(EquipmentSlotType.Base, 1), ASCIIColors.SlotBase);
    }

    private void RenderEquipmentSlot(int x, int y, int width, string label, Item? item, Color slotColor)
    {
        if (item != null)
        {
            // Status indicator for toggleable modules
            string statusIndicator = "";
            Color statusColor = slotColor;

            if (item.IsToggleable)
            {
                if (item.IsActive)
                {
                    statusIndicator = "+";
                    statusColor = ASCIIColors.Success;
                }
                else
                {
                    statusIndicator = "-";
                    statusColor = ASCIIColors.TextDisabled;
                }
            }

            // Build item display with energy impact
            string itemName = item.Name;
            string energyHint = "";

            // Show energy impact for active items
            if (item.EnergyOutput > 0 || item.EnergyConsumption > 0)
            {
                int net = item.NetEnergyImpact;
                if (net != 0)
                {
                    energyHint = net > 0 ? $" +{net}" : $" {net}";
                }
            }

            int maxNameLen = width - 6 - statusIndicator.Length - energyHint.Length;
            if (itemName.Length > maxNameLen)
            {
                itemName = itemName[..(maxNameLen - 2)] + "..";
            }

            var rarityColor = item.IsToggleable && !item.IsActive
                ? ASCIIColors.Dimmed(ASCIIColors.GetRarityColor(item.Rarity))
                : ASCIIColors.GetRarityColor(item.Rarity);

            _buffer.WriteString(x + 1, y, $"{label}:", slotColor);
            int xPos = x + 4;

            if (!string.IsNullOrEmpty(statusIndicator))
            {
                _buffer.WriteString(xPos, y, statusIndicator, statusColor);
                xPos++;
            }

            _buffer.WriteString(xPos, y, itemName, rarityColor);

            if (!string.IsNullOrEmpty(energyHint))
            {
                int hintX = x + width - energyHint.Length - 1;
                Color energyColor = item.NetEnergyImpact > 0 ? ASCIIColors.Energy : ASCIIColors.Warning;
                _buffer.WriteString(hintX, y, energyHint, item.IsActive ? energyColor : ASCIIColors.TextDisabled);
            }
        }
        else
        {
            _buffer.WriteString(x + 1, y, $"{label}: ", ASCIIColors.Dimmed(slotColor));
            _buffer.WriteString(x + 5, y, "[empty]", ASCIIColors.TextDisabled);
        }
    }

    private void RenderStatusBar(int turnNumber)
    {
        int y = ASCIIBuffer.StatusBarLine;

        // Key hints
        int hintX = 1;
        hintX = WriteHint(hintX, y, "[?]", "Help");
        hintX = WriteHint(hintX, y, "[I]", "Inventory");
        hintX = WriteHint(hintX, y, "[1-6]", "Weapons");
        hintX = WriteHint(hintX, y, "[Tab]", "Cycle");

        // Turn counter (right aligned)
        string turnText = $"Turn: {turnNumber}";
        _buffer.WriteString(ASCIIBuffer.Width - turnText.Length - 1, y, turnText, ASCIIColors.Primary);
    }

    private int WriteHint(int x, int y, string key, string action)
    {
        _buffer.WriteString(x, y, key, ASCIIColors.Primary);
        _buffer.WriteString(x + key.Length, y, action, ASCIIColors.TextMuted);
        return x + key.Length + action.Length + 2; // +2 for spacing
    }
}
