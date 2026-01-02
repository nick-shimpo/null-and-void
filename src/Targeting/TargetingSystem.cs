using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Items;

namespace NullAndVoid.Targeting;

/// <summary>
/// Current targeting mode.
/// </summary>
public enum TargetingMode
{
    /// <summary>
    /// Not in targeting mode.
    /// </summary>
    None,

    /// <summary>
    /// Automatically targeting nearest enemy.
    /// </summary>
    AutoTarget,

    /// <summary>
    /// Manual cursor movement for precise targeting.
    /// </summary>
    ManualCursor,

    /// <summary>
    /// Selecting area for AoE attack.
    /// </summary>
    AreaSelect
}

/// <summary>
/// Information about a potential target.
/// </summary>
public class TargetInfo
{
    public Node Entity { get; set; } = null!;
    public Vector2I Position { get; set; }
    public int Distance { get; set; }
    public LineOfFireInfo LineOfFire { get; set; }
    public int Accuracy { get; set; }
    public string Name { get; set; } = "";
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }

    public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0;
}

/// <summary>
/// Manages the targeting interface for ranged combat.
/// </summary>
public class TargetingSystem
{
    private TargetingMode _mode = TargetingMode.None;
    private Vector2I _cursorPosition;
    private Vector2I _attackerPosition;
    private Item? _activeWeapon;
    private List<TargetInfo> _validTargets = new();
    private int _currentTargetIndex = 0;
    private SceneTree? _sceneTree;

    /// <summary>
    /// Current targeting mode.
    /// </summary>
    public TargetingMode Mode => _mode;

    /// <summary>
    /// Whether targeting is currently active.
    /// </summary>
    public bool IsActive => _mode != TargetingMode.None;

    /// <summary>
    /// Current cursor position in world coordinates.
    /// </summary>
    public Vector2I CursorPosition => _cursorPosition;

    /// <summary>
    /// Attacker's position.
    /// </summary>
    public Vector2I AttackerPosition => _attackerPosition;

    /// <summary>
    /// Currently selected weapon.
    /// </summary>
    public Item? ActiveWeapon => _activeWeapon;

    /// <summary>
    /// Weapon data for the active weapon.
    /// </summary>
    public WeaponData? WeaponData => _activeWeapon?.WeaponData;

    /// <summary>
    /// Current target (if any).
    /// </summary>
    public TargetInfo? CurrentTarget =>
        _validTargets.Count > 0 && _currentTargetIndex < _validTargets.Count
            ? _validTargets[_currentTargetIndex]
            : null;

    /// <summary>
    /// All valid targets in range.
    /// </summary>
    public IReadOnlyList<TargetInfo> ValidTargets => _validTargets;

    /// <summary>
    /// Line of fire to current cursor position.
    /// </summary>
    public LineOfFireInfo CurrentLineOfFire { get; private set; }

    /// <summary>
    /// Calculated accuracy for current target.
    /// </summary>
    public int CurrentAccuracy => CurrentTarget?.Accuracy ?? 0;

    /// <summary>
    /// Event fired when targeting mode changes.
    /// </summary>
    public event Action<TargetingMode>? ModeChanged;

    /// <summary>
    /// Event fired when cursor moves.
    /// </summary>
    public event Action<Vector2I>? CursorMoved;

    /// <summary>
    /// Event fired when target changes.
    /// </summary>
    public event Action<TargetInfo?>? TargetChanged;

    /// <summary>
    /// Initialize the targeting system with a scene tree reference.
    /// </summary>
    public void Initialize(SceneTree sceneTree)
    {
        _sceneTree = sceneTree;
    }

    /// <summary>
    /// Begin targeting mode with a weapon.
    /// </summary>
    public bool BeginTargeting(Item weapon, Vector2I attackerPosition)
    {
        if (weapon.WeaponData == null || !weapon.IsRangedWeapon)
        {
            GD.Print($"[TargetingSystem] Cannot target with non-ranged weapon: {weapon.Name}");
            return false;
        }

        if (!weapon.IsWeaponReady)
        {
            GD.Print($"[TargetingSystem] Weapon on cooldown: {weapon.Name}");
            return false;
        }

        _activeWeapon = weapon;
        _attackerPosition = attackerPosition;
        _cursorPosition = attackerPosition;

        // Find all valid targets
        RefreshValidTargets();

        // Set initial mode and target
        if (_validTargets.Count > 0)
        {
            _mode = TargetingMode.AutoTarget;
            _currentTargetIndex = 0;
            _cursorPosition = CurrentTarget!.Position;
            UpdateLineOfFire();
        }
        else
        {
            _mode = TargetingMode.ManualCursor;
            _cursorPosition = attackerPosition;
        }

        ModeChanged?.Invoke(_mode);
        TargetChanged?.Invoke(CurrentTarget);

        GD.Print($"[TargetingSystem] Targeting started with {weapon.Name}, {_validTargets.Count} valid targets");
        return true;
    }

    /// <summary>
    /// Cancel targeting mode.
    /// </summary>
    public void CancelTargeting()
    {
        _mode = TargetingMode.None;
        _activeWeapon = null;
        _validTargets.Clear();
        _currentTargetIndex = 0;

        ModeChanged?.Invoke(_mode);
        TargetChanged?.Invoke(null);

        GD.Print("[TargetingSystem] Targeting cancelled");
    }

    /// <summary>
    /// Move cursor in a direction.
    /// </summary>
    public void MoveCursor(Vector2I direction)
    {
        if (!IsActive)
            return;

        // Switch to manual mode when moving cursor
        if (_mode == TargetingMode.AutoTarget)
        {
            _mode = TargetingMode.ManualCursor;
            ModeChanged?.Invoke(_mode);
        }

        var newPosition = _cursorPosition + direction;

        // Validate position is in range
        int distance = LineOfFire.GetDistance(_attackerPosition, newPosition);
        if (WeaponData != null && distance <= WeaponData.Range)
        {
            _cursorPosition = newPosition;
            UpdateLineOfFire();
            UpdateCurrentTargetFromCursor();
            CursorMoved?.Invoke(_cursorPosition);
        }
    }

    /// <summary>
    /// Set cursor to a specific position.
    /// </summary>
    public void SetCursorPosition(Vector2I position)
    {
        if (!IsActive)
            return;

        int distance = LineOfFire.GetDistance(_attackerPosition, position);
        if (WeaponData != null && distance <= WeaponData.Range)
        {
            _cursorPosition = position;

            if (_mode == TargetingMode.AutoTarget)
            {
                _mode = TargetingMode.ManualCursor;
                ModeChanged?.Invoke(_mode);
            }

            UpdateLineOfFire();
            UpdateCurrentTargetFromCursor();
            CursorMoved?.Invoke(_cursorPosition);
        }
    }

    /// <summary>
    /// Cycle to the next valid target.
    /// </summary>
    public void CycleTarget(int direction = 1)
    {
        if (!IsActive || _validTargets.Count == 0)
            return;

        // Switch to auto-target mode
        if (_mode != TargetingMode.AutoTarget)
        {
            _mode = TargetingMode.AutoTarget;
            ModeChanged?.Invoke(_mode);
        }

        _currentTargetIndex = (_currentTargetIndex + direction + _validTargets.Count) % _validTargets.Count;

        // Safety check - CurrentTarget could be null if targets changed
        var target = CurrentTarget;
        if (target != null)
        {
            _cursorPosition = target.Position;
        }
        UpdateLineOfFire();

        CursorMoved?.Invoke(_cursorPosition);
        TargetChanged?.Invoke(CurrentTarget);
    }

    /// <summary>
    /// Confirm the current target and return attack info.
    /// Returns null if no valid target.
    /// </summary>
    public AttackInfo? ConfirmTarget()
    {
        if (!IsActive || _activeWeapon == null || WeaponData == null)
            return null;

        // Check if we have a valid line of fire
        if (CurrentLineOfFire.Result == LineOfFireResult.Blocked)
        {
            GD.Print("[TargetingSystem] Cannot attack - line of fire blocked");
            return null;
        }

        if (CurrentLineOfFire.Result == LineOfFireResult.OutOfRange)
        {
            GD.Print("[TargetingSystem] Cannot attack - target out of range");
            return null;
        }

        var attackInfo = new AttackInfo
        {
            Weapon = _activeWeapon,
            AttackerPosition = _attackerPosition,
            TargetPosition = _cursorPosition,
            LineOfFire = CurrentLineOfFire,
            Target = CurrentTarget,
            Accuracy = CurrentAccuracy,
            AffectedTiles = GetAffectedTiles()
        };

        // End targeting
        CancelTargeting();

        return attackInfo;
    }

    /// <summary>
    /// Get tiles affected by current weapon at cursor position.
    /// </summary>
    public List<Vector2I> GetAffectedTiles()
    {
        if (WeaponData == null || WeaponData.AreaRadius <= 0)
        {
            return new List<Vector2I> { _cursorPosition };
        }

        return LineOfFire.GetPositionsInRadius(_cursorPosition, WeaponData.AreaRadius);
    }

    /// <summary>
    /// Refresh the list of valid targets.
    /// </summary>
    private void RefreshValidTargets()
    {
        _validTargets.Clear();

        if (_sceneTree == null || WeaponData == null)
            return;

        // Get all enemies
        var enemies = _sceneTree.GetNodesInGroup("Enemies");

        foreach (var node in enemies)
        {
            if (node is not Entity entity)
                continue;
            if (!GodotObject.IsInstanceValid(entity))
                continue;

            var targetPos = entity.GridPosition;
            int distance = LineOfFire.GetDistance(_attackerPosition, targetPos);

            // Check range
            if (distance > WeaponData.Range)
                continue;

            // Check line of fire
            var lof = LineOfFire.Check(_attackerPosition, targetPos, WeaponData.Range);
            if (lof.Result == LineOfFireResult.Blocked)
                continue;

            // Calculate accuracy
            int accuracy = AccuracyCalculator.CalculateSimple(WeaponData, distance, lof.Result);

            // Get health info
            int currentHealth = 0;
            int maxHealth = 1;
            string name = "Unknown";

            if (node is Enemy enemy)
            {
                currentHealth = enemy.CurrentHealth;
                maxHealth = enemy.MaxHealth;
                name = enemy.EntityName;
            }

            _validTargets.Add(new TargetInfo
            {
                Entity = node,
                Position = targetPos,
                Distance = distance,
                LineOfFire = lof,
                Accuracy = accuracy,
                Name = name,
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth
            });
        }

        // Sort by distance (nearest first)
        _validTargets = _validTargets.OrderBy(t => t.Distance).ToList();
    }

    /// <summary>
    /// Update line of fire to current cursor position.
    /// </summary>
    private void UpdateLineOfFire()
    {
        if (WeaponData == null)
            return;

        CurrentLineOfFire = LineOfFire.Check(_attackerPosition, _cursorPosition, WeaponData.Range);
    }

    /// <summary>
    /// Update current target based on cursor position.
    /// </summary>
    private void UpdateCurrentTargetFromCursor()
    {
        // Find target at cursor position
        var targetAtCursor = _validTargets.FirstOrDefault(t => t.Position == _cursorPosition);

        if (targetAtCursor != null)
        {
            _currentTargetIndex = _validTargets.IndexOf(targetAtCursor);
            TargetChanged?.Invoke(CurrentTarget);
        }
        else
        {
            // No target at cursor - calculate accuracy for empty tile
            if (WeaponData != null)
            {
                int distance = LineOfFire.GetDistance(_attackerPosition, _cursorPosition);
                // For AoE weapons, can target empty tiles
                if (WeaponData.AreaRadius > 0)
                {
                    TargetChanged?.Invoke(null);
                }
            }
        }
    }
}

/// <summary>
/// Information about a confirmed attack.
/// </summary>
public class AttackInfo
{
    public Item Weapon { get; set; } = null!;
    public Vector2I AttackerPosition { get; set; }
    public Vector2I TargetPosition { get; set; }
    public LineOfFireInfo LineOfFire { get; set; }
    public TargetInfo? Target { get; set; }
    public int Accuracy { get; set; }
    public List<Vector2I> AffectedTiles { get; set; } = new();
}
