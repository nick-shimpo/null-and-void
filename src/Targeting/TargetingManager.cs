using System;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Rendering;

namespace NullAndVoid.Targeting;

/// <summary>
/// Coordinates targeting between input, TargetingSystem, TargetingRenderer, and CombatResolver.
/// Handles weapon hotkey selection and targeting input.
/// </summary>
public class TargetingManager
{
    private readonly TargetingSystem _targetingSystem;
    private readonly TargetingRenderer _targetingRenderer;
    private Player? _player;
    private SceneTree? _sceneTree;

    /// <summary>
    /// Currently selected weapon slot (1-9).
    /// </summary>
    public int SelectedWeaponSlot { get; private set; } = 0;

    /// <summary>
    /// Whether targeting mode is active.
    /// </summary>
    public bool IsTargeting => _targetingSystem.IsActive;

    /// <summary>
    /// Event fired when targeting starts.
    /// </summary>
    public event Action? TargetingStarted;

    /// <summary>
    /// Event fired when targeting ends.
    /// </summary>
    public event Action? TargetingEnded;

    /// <summary>
    /// Event fired when an attack is performed.
    /// </summary>
    public event Action<CombatResult>? AttackPerformed;

    /// <summary>
    /// Event fired when a message should be displayed.
    /// </summary>
    public event Action<string, Color>? MessageRequested;

    public TargetingManager(ASCIIBuffer buffer, MapRenderer mapRenderer)
    {
        _targetingSystem = new TargetingSystem();
        _targetingRenderer = new TargetingRenderer(buffer, mapRenderer);
    }

    /// <summary>
    /// Initialize with player and scene tree references.
    /// </summary>
    public void Initialize(Player player, SceneTree sceneTree)
    {
        _player = player;
        _sceneTree = sceneTree;
        _targetingSystem.Initialize(sceneTree);
        _targetingRenderer.SetSceneTree(sceneTree);
    }

    /// <summary>
    /// Set the MapViewport for zoomed rendering.
    /// </summary>
    public void SetMapViewport(MapViewport? viewport)
    {
        _targetingRenderer.SetMapViewport(viewport);
    }

    /// <summary>
    /// Update targeting animations.
    /// </summary>
    public void Update(float delta)
    {
        if (IsTargeting)
        {
            _targetingRenderer.Update(delta);
        }
    }

    /// <summary>
    /// Render targeting interface if active.
    /// </summary>
    public void Render()
    {
        if (IsTargeting)
        {
            _targetingRenderer.Render(_targetingSystem);
        }
    }

    /// <summary>
    /// Handle input when in targeting mode.
    /// Returns true if input was consumed.
    /// </summary>
    public bool HandleTargetingInput(InputEvent @event)
    {
        if (!IsTargeting)
            return false;

        // Cancel targeting
        if (@event.IsActionPressed("ui_cancel") ||
            (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape))
        {
            CancelTargeting();
            return true;
        }

        // Confirm target
        if (@event.IsActionPressed("ui_accept") ||
            (@event is InputEventKey enterKey && enterKey.Pressed &&
             (enterKey.Keycode == Key.Enter || enterKey.Keycode == Key.KpEnter)))
        {
            ConfirmTarget();
            return true;
        }

        // Cycle targets with Tab
        if (@event is InputEventKey tabKey && tabKey.Pressed && tabKey.Keycode == Key.Tab)
        {
            if (tabKey.ShiftPressed)
                _targetingSystem.CycleTarget(-1);  // Previous target
            else
                _targetingSystem.CycleTarget(1);  // Next target
            return true;
        }

        // Cursor movement (same as player movement)
        Vector2I cursorMove = Vector2I.Zero;
        if (@event.IsActionPressed("move_up"))
            cursorMove = new Vector2I(0, -1);
        else if (@event.IsActionPressed("move_down"))
            cursorMove = new Vector2I(0, 1);
        else if (@event.IsActionPressed("move_left"))
            cursorMove = new Vector2I(-1, 0);
        else if (@event.IsActionPressed("move_right"))
            cursorMove = new Vector2I(1, 0);
        else if (@event.IsActionPressed("move_up_left"))
            cursorMove = new Vector2I(-1, -1);
        else if (@event.IsActionPressed("move_up_right"))
            cursorMove = new Vector2I(1, -1);
        else if (@event.IsActionPressed("move_down_left"))
            cursorMove = new Vector2I(-1, 1);
        else if (@event.IsActionPressed("move_down_right"))
            cursorMove = new Vector2I(1, 1);

        if (cursorMove != Vector2I.Zero)
        {
            _targetingSystem.MoveCursor(cursorMove);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handle weapon hotkey input.
    /// Returns true if a weapon was selected and targeting started.
    /// </summary>
    public bool HandleWeaponHotkey(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
            return false;

        // Number keys 1-9 for weapon hotkeys
        int slot = keyEvent.Keycode switch
        {
            Key.Key1 => 1,
            Key.Key2 => 2,
            Key.Key3 => 3,
            Key.Key4 => 4,
            Key.Key5 => 5,
            Key.Key6 => 6,
            Key.Key7 => 7,
            Key.Key8 => 8,
            Key.Key9 => 9,
            _ => 0
        };

        if (slot == 0)
            return false;

        // If already targeting with this slot, cancel
        if (IsTargeting && SelectedWeaponSlot == slot)
        {
            CancelTargeting();
            return true;
        }

        return TryStartTargetingWithSlot(slot);
    }

    /// <summary>
    /// Try to start targeting with a specific weapon slot.
    /// </summary>
    private bool TryStartTargetingWithSlot(int slot)
    {
        if (_player?.EquipmentComponent == null)
            return false;

        // Get weapon at hotkey slot (utility slots are indexed 0-based)
        var weapon = _player.EquipmentComponent.GetWeaponByHotkey(slot);
        if (weapon == null)
        {
            MessageRequested?.Invoke($"No weapon in slot {slot}", ASCIIColors.TextDim);
            return false;
        }

        // Check if ranged weapon
        if (!weapon.IsRangedWeapon)
        {
            MessageRequested?.Invoke($"{weapon.Name} is a melee weapon - bump to attack", ASCIIColors.TextDim);
            return false;
        }

        // Check if weapon is ready (cooldown)
        if (!weapon.IsWeaponReady)
        {
            MessageRequested?.Invoke($"{weapon.Name} is on cooldown ({weapon.WeaponData!.CurrentCooldown}t)", ASCIIColors.AlertWarning);
            return false;
        }

        // Start targeting
        if (_targetingSystem.BeginTargeting(weapon, _player.GridPosition))
        {
            SelectedWeaponSlot = slot;
            TargetingStarted?.Invoke();

            // Show targeting info
            int targetCount = _targetingSystem.ValidTargets.Count;
            if (targetCount > 0)
            {
                MessageRequested?.Invoke(
                    $"[{weapon.Name}] {targetCount} targets in range - Tab to cycle, Enter to fire, Esc to cancel",
                    ASCIIColors.TargetingCursor);
            }
            else
            {
                MessageRequested?.Invoke(
                    $"[{weapon.Name}] No targets in range - use cursor keys to aim, Enter to fire",
                    ASCIIColors.AlertWarning);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Start targeting with a specific weapon.
    /// </summary>
    public bool StartTargeting(Item weapon)
    {
        if (_player == null)
            return false;

        if (_targetingSystem.BeginTargeting(weapon, _player.GridPosition))
        {
            SelectedWeaponSlot = 0;  // No specific slot
            TargetingStarted?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancel current targeting.
    /// </summary>
    public void CancelTargeting()
    {
        if (!IsTargeting)
            return;

        _targetingSystem.CancelTargeting();
        SelectedWeaponSlot = 0;
        TargetingEnded?.Invoke();
        MessageRequested?.Invoke("Targeting cancelled", ASCIIColors.TextDim);
    }

    /// <summary>
    /// Confirm current target and fire.
    /// Returns action cost if attack was performed, 0 otherwise.
    /// </summary>
    public int ConfirmTarget()
    {
        if (!IsTargeting || _player == null || _sceneTree == null)
            return 0;

        var attackInfo = _targetingSystem.ConfirmTarget();
        if (attackInfo == null)
        {
            MessageRequested?.Invoke("Invalid target!", ASCIIColors.AlertDanger);
            return 0;
        }

        // Resolve the attack
        var result = CombatResolver.ResolveRangedAttack(_player, attackInfo, _sceneTree);

        // Notify listeners
        SelectedWeaponSlot = 0;
        TargetingEnded?.Invoke();
        AttackPerformed?.Invoke(result);

        // Display result message
        MessageRequested?.Invoke(result.GetSummary(), result.Success ? ASCIIColors.AlertSuccess : ASCIIColors.AlertWarning);

        return result.ActionCost;
    }

    /// <summary>
    /// Get the current targeting system (for rendering).
    /// </summary>
    public TargetingSystem TargetingSystem => _targetingSystem;
}
