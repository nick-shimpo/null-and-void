using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Items;
using NullAndVoid.Rendering;
using NullAndVoid.Targeting;

namespace NullAndVoid.UI;

/// <summary>
/// Renders targeting mode information overlay.
/// Shows weapon stats, target info, accuracy, and controls when targeting.
/// </summary>
public class TargetingOverlay
{
    private readonly ASCIIBuffer _buffer;

    // Layout constants - draws in sidebar area
    private const int OVERLAY_X = ASCIIBuffer.SidebarX + 1;
    private const int OVERLAY_Y = 2;
    private const int OVERLAY_WIDTH = ASCIIBuffer.SidebarWidth - 2;
    private const int OVERLAY_HEIGHT = 20;

    // Colors
    private static readonly Color _headerColor = new(0.2f, 0.8f, 1.0f);
    private static readonly Color _weaponColor = new(1.0f, 0.8f, 0.2f);
    private static readonly Color _statColor = new(0.7f, 0.7f, 0.7f);
    private static readonly Color _targetColor = new(1.0f, 0.4f, 0.4f);
    private static readonly Color _accuracyGood = new(0.3f, 1.0f, 0.3f);
    private static readonly Color _accuracyMedium = new(1.0f, 0.9f, 0.3f);
    private static readonly Color _accuracyPoor = new(1.0f, 0.3f, 0.3f);

    public TargetingOverlay(ASCIIBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Render the targeting overlay if in targeting mode.
    /// </summary>
    public void Render(TargetingSystem targeting)
    {
        if (!targeting.IsActive)
            return;

        // Clear overlay area
        ClearArea();

        int y = OVERLAY_Y;

        // Header with targeting mode indicator
        DrawHeader(ref y, targeting);

        y += 2;

        // Weapon info
        if (targeting.ActiveWeapon != null)
        {
            DrawWeaponInfo(ref y, targeting.ActiveWeapon);
        }

        y += 2;

        // Target info or AoE info
        if (targeting.CurrentTarget != null)
        {
            DrawTargetInfo(ref y, targeting);
        }
        else if (targeting.WeaponData?.AreaRadius > 0)
        {
            DrawAoEInfo(ref y, targeting);
        }
        else
        {
            DrawNoTarget(ref y);
        }

        y += 2;

        // Line of fire status
        DrawLineOfFireStatus(ref y, targeting);

        y += 2;

        // Controls
        DrawControls(ref y, targeting);
    }

    /// <summary>
    /// Clear the overlay area.
    /// </summary>
    private void ClearArea()
    {
        for (int y = OVERLAY_Y; y < OVERLAY_Y + OVERLAY_HEIGHT; y++)
        {
            for (int x = OVERLAY_X; x < OVERLAY_X + OVERLAY_WIDTH; x++)
            {
                _buffer.SetCell(x, y, ' ', ASCIIColors.TextNormal);
            }
        }
    }

    /// <summary>
    /// Draw targeting mode header.
    /// </summary>
    private void DrawHeader(ref int y, TargetingSystem targeting)
    {
        string modeText = targeting.Mode switch
        {
            TargetingMode.AutoTarget => "AUTO-TARGET",
            TargetingMode.ManualCursor => "MANUAL AIM",
            TargetingMode.AreaSelect => "AREA SELECT",
            _ => "TARGETING"
        };

        // Centered header with brackets
        string header = $"[{modeText}]";
        int headerX = OVERLAY_X + (OVERLAY_WIDTH - header.Length) / 2;
        _buffer.DrawString(headerX, y, header, _headerColor);
    }

    /// <summary>
    /// Draw weapon information.
    /// </summary>
    private void DrawWeaponInfo(ref int y, Item weapon)
    {
        var weaponData = weapon.WeaponData;
        if (weaponData == null)
            return;

        // Weapon name
        _buffer.DrawString(OVERLAY_X, y, weapon.Name, _weaponColor);
        y++;

        // Damage
        string dmgLine = $"Damage: {weaponData.DamageString}";
        _buffer.DrawString(OVERLAY_X, y, dmgLine, _statColor);
        y++;

        // Range
        string rngLine = $"Range:  {weaponData.Range} tiles";
        _buffer.DrawString(OVERLAY_X, y, rngLine, _statColor);
        y++;

        // Energy cost
        if (weaponData.EnergyCost > 0)
        {
            string nrgLine = $"Energy: {weaponData.EnergyCost}";
            _buffer.DrawString(OVERLAY_X, y, nrgLine, _statColor);
            y++;
        }

        // AoE radius
        if (weaponData.AreaRadius > 0)
        {
            string aoeLine = $"Radius: {weaponData.AreaRadius} tiles";
            _buffer.DrawString(OVERLAY_X, y, aoeLine, ASCIIColors.ExplosionOuter);
            y++;
        }
    }

    /// <summary>
    /// Draw current target information.
    /// </summary>
    private void DrawTargetInfo(ref int y, TargetingSystem targeting)
    {
        var target = targeting.CurrentTarget;
        if (target == null)
            return;

        // Target label
        _buffer.DrawString(OVERLAY_X, y, "TARGET:", ASCIIColors.TextDim);
        y++;

        // Target name
        _buffer.DrawString(OVERLAY_X, y, target.Name, _targetColor);
        y++;

        // Health bar
        int barWidth = OVERLAY_WIDTH - 2;
        int filledWidth = (int)(barWidth * target.HealthPercent);
        string healthBar = new string('#', filledWidth) + new string('-', barWidth - filledWidth);
        Color healthColor = GetHealthColor(target.HealthPercent);
        _buffer.DrawString(OVERLAY_X, y, $"[{healthBar}]", healthColor);
        y++;

        // Health percentage
        string healthText = $"Integrity: {target.CurrentHealth}/{target.MaxHealth}";
        _buffer.DrawString(OVERLAY_X, y, healthText, healthColor);
        y++;

        // Accuracy
        Color accColor = GetAccuracyColor(target.Accuracy);
        string accText = $"Accuracy: {target.Accuracy}%";
        _buffer.DrawString(OVERLAY_X, y, accText, accColor);
        y++;

        // Distance
        string distText = $"Distance: {target.Distance}";
        _buffer.DrawString(OVERLAY_X, y, distText, _statColor);
    }

    /// <summary>
    /// Draw AoE attack information.
    /// </summary>
    private void DrawAoEInfo(ref int y, TargetingSystem targeting)
    {
        _buffer.DrawString(OVERLAY_X, y, "AREA ATTACK", ASCIIColors.ExplosionOuter);
        y++;

        if (targeting.WeaponData != null)
        {
            string radiusText = $"Blast Radius: {targeting.WeaponData.AreaRadius}";
            _buffer.DrawString(OVERLAY_X, y, radiusText, _statColor);
            y++;
        }

        // Count targets in area
        int targetCount = 0;
        foreach (var t in targeting.ValidTargets)
        {
            int dist = LineOfFire.GetDistance(targeting.CursorPosition, t.Position);
            if (targeting.WeaponData != null && dist <= targeting.WeaponData.AreaRadius)
            {
                targetCount++;
            }
        }

        string targetText = targetCount > 0
            ? $"Enemies in area: {targetCount}"
            : "No enemies in area";
        Color targetColor = targetCount > 0 ? _accuracyGood : ASCIIColors.TextDim;
        _buffer.DrawString(OVERLAY_X, y, targetText, targetColor);
        y++;

        _buffer.DrawString(OVERLAY_X, y, "AoE always hits!", ASCIIColors.AlertSuccess);
    }

    /// <summary>
    /// Draw no target message.
    /// </summary>
    private void DrawNoTarget(ref int y)
    {
        _buffer.DrawString(OVERLAY_X, y, "No target selected", ASCIIColors.TextDim);
        y++;
        _buffer.DrawString(OVERLAY_X, y, "Use cursor to aim", ASCIIColors.TextDim);
    }

    /// <summary>
    /// Draw line of fire status.
    /// </summary>
    private void DrawLineOfFireStatus(ref int y, TargetingSystem targeting)
    {
        var lof = targeting.CurrentLineOfFire;
        string statusText;
        Color statusColor;

        switch (lof.Result)
        {
            case LineOfFireResult.Clear:
                statusText = "Line of Fire: CLEAR";
                statusColor = _accuracyGood;
                break;
            case LineOfFireResult.PartialCover:
                statusText = "Line of Fire: PARTIAL COVER";
                statusColor = _accuracyMedium;
                break;
            case LineOfFireResult.Blocked:
                statusText = "Line of Fire: BLOCKED";
                statusColor = _accuracyPoor;
                break;
            case LineOfFireResult.OutOfRange:
                statusText = "OUT OF RANGE";
                statusColor = _accuracyPoor;
                break;
            default:
                statusText = "Unknown";
                statusColor = _statColor;
                break;
        }

        _buffer.DrawString(OVERLAY_X, y, statusText, statusColor);
    }

    /// <summary>
    /// Draw control hints.
    /// </summary>
    private void DrawControls(ref int y, TargetingSystem targeting)
    {
        _buffer.DrawString(OVERLAY_X, y, "CONTROLS:", ASCIIColors.TextDim);
        y++;

        // Movement
        _buffer.DrawString(OVERLAY_X, y, "Arrows - Move cursor", ASCIIColors.TextDim);
        y++;

        // Tab to cycle
        if (targeting.ValidTargets.Count > 1)
        {
            _buffer.DrawString(OVERLAY_X, y, "Tab    - Next target", ASCIIColors.TextDim);
            y++;
        }

        // Confirm/Cancel
        _buffer.DrawString(OVERLAY_X, y, "Enter  - Fire weapon", _accuracyGood);
        y++;
        _buffer.DrawString(OVERLAY_X, y, "Escape - Cancel", _accuracyPoor);
    }

    /// <summary>
    /// Get health bar color based on percentage.
    /// </summary>
    private Color GetHealthColor(float percent)
    {
        if (percent > 0.6f)
            return _accuracyGood;
        if (percent > 0.3f)
            return _accuracyMedium;
        return _accuracyPoor;
    }

    /// <summary>
    /// Get accuracy color based on percentage.
    /// </summary>
    private Color GetAccuracyColor(int accuracy)
    {
        if (accuracy >= 70)
            return _accuracyGood;
        if (accuracy >= 40)
            return _accuracyMedium;
        return _accuracyPoor;
    }
}
