using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using NullAndVoid.AI;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Rendering;

namespace NullAndVoid.Debug;

/// <summary>
/// Debug system that captures game state to files for external analysis.
/// Dumps turn state, ASCII buffer snapshots, and scheduler info.
/// </summary>
public partial class GameDebugger : Node
{
    private static GameDebugger? _instance;
    public static GameDebugger? Instance => _instance;

    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool DumpEveryTurn { get; set; } = true;
    [Export] public bool DumpASCIIBuffer { get; set; } = true;
    [Export] public string DebugOutputPath { get; set; } = "debug_output";

    private int _turnCounter = 0;
    private readonly List<string> _turnLog = new();
    private ASCIIBuffer? _buffer;

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;

        // Ensure debug directory exists
        if (!Directory.Exists(DebugOutputPath))
        {
            Directory.CreateDirectory(DebugOutputPath);
        }

        // Clear old debug files
        ClearDebugFiles();
    }

    public override void _Ready()
    {
        if (!Enabled)
            return;

        // Subscribe to turn events
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.PlayerTurnEnded += OnPlayerTurnEnded;
        EventBus.Instance.EntityMoved += OnEntityMoved;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
        EventBus.Instance.AttackPerformed += OnAttackPerformed;

        Log("GameDebugger initialized");
        DumpSchedulerState("initialization");
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        if (EventBus.Instance != null)
        {
            EventBus.Instance.TurnStarted -= OnTurnStarted;
            EventBus.Instance.PlayerTurnEnded -= OnPlayerTurnEnded;
            EventBus.Instance.EntityMoved -= OnEntityMoved;
            EventBus.Instance.EntityDamaged -= OnEntityDamaged;
            EventBus.Instance.AttackPerformed -= OnAttackPerformed;
        }

        // Write final log
        WriteLogToFile();
    }

    public void SetBuffer(ASCIIBuffer buffer)
    {
        _buffer = buffer;
    }

    private void ClearDebugFiles()
    {
        try
        {
            foreach (var file in Directory.GetFiles(DebugOutputPath, "*.txt"))
            {
                File.Delete(file);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameDebugger] Failed to clear debug files: {e.Message}");
        }
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";
        _turnLog.Add(logLine);
        GD.Print($"[DEBUG] {message}");
    }

    private void OnTurnStarted(int turnNumber)
    {
        _turnCounter = turnNumber;
        Log($"=== TURN {turnNumber} STARTED ===");

        if (DumpEveryTurn)
        {
            DumpGameState($"turn_{turnNumber:D4}_start");
        }
    }

    private void OnPlayerTurnEnded()
    {
        Log($"Player turn ended (turn {_turnCounter})");

        if (DumpEveryTurn)
        {
            DumpGameState($"turn_{_turnCounter:D4}_player_done");
        }
    }

    private void OnEntityMoved(Node entity, Vector2I from, Vector2I to)
    {
        string name = entity is Entity e ? e.EntityName : entity.Name;
        Log($"Entity moved: {name} from {from} to {to}");
    }

    private void OnEntityDamaged(Node entity, int damage, int remainingHealth)
    {
        string name = entity is Entity e ? e.EntityName : entity.Name;
        Log($"Entity damaged: {name} took {damage} damage, {remainingHealth} HP remaining");
    }

    private void OnAttackPerformed(Node attacker, Node target, int damage)
    {
        string attackerName = attacker is Entity ae ? ae.EntityName : attacker.Name;
        string targetName = target is Entity te ? te.EntityName : target.Name;
        Log($"Attack: {attackerName} -> {targetName} for {damage} damage");
    }

    public void DumpGameState(string label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== GAME STATE: {label} ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine();

        // Scheduler state
        sb.AppendLine("--- SCHEDULER ---");
        sb.AppendLine(TurnManager.Instance?.GetSchedulerDebugInfo() ?? "TurnManager not available");
        sb.AppendLine();

        // Player state
        sb.AppendLine("--- PLAYER ---");
        var player = GetTree().GetFirstNodeInGroup("Player") as Player;
        if (player != null)
        {
            sb.AppendLine($"  Name: {player.EntityName}");
            sb.AppendLine($"  Position: {player.GridPosition}");
            sb.AppendLine($"  Health: {player.CurrentHealth}/{player.MaxHealth}");
            sb.AppendLine($"  CanAct: {player.CanAct}");
            sb.AppendLine($"  IsActive: {player.IsActive}");
        }
        else
        {
            sb.AppendLine("  Player not found!");
        }
        sb.AppendLine();

        // Enemy states
        sb.AppendLine("--- ENEMIES ---");
        var enemies = GetTree().GetNodesInGroup("Enemies");
        sb.AppendLine($"  Count: {enemies.Count}");
        foreach (var node in enemies)
        {
            if (node is Enemy enemy)
            {
                sb.AppendLine($"  [{enemy.EntityName}]");
                sb.AppendLine($"    Position: {enemy.GridPosition}");
                sb.AppendLine($"    Health: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                sb.AppendLine($"    State: {enemy.CurrentState}");
                sb.AppendLine($"    Speed: {enemy.Speed}");
                sb.AppendLine($"    IsActive: {enemy.IsActive}");
                sb.AppendLine($"    CanAct: {enemy.CanAct}");
                sb.AppendLine($"    Behaviors: {enemy.Behaviors.GetBehaviors().Count}");
                foreach (var behavior in enemy.Behaviors.GetBehaviors())
                {
                    sb.AppendLine($"      - [{behavior.Priority}] {behavior.Name}");
                }
                sb.AppendLine($"    Memory.AlertLevel: {enemy.Memory.AlertLevel}");
                sb.AppendLine($"    Memory.LastKnownTargetPos: {enemy.Memory.LastKnownTargetPos?.ToString() ?? "null"}");
                sb.AppendLine($"    Memory.TurnsSinceTargetSeen: {enemy.Memory.TurnsSinceTargetSeen}");
            }
        }
        sb.AppendLine();

        // Write to file
        var filename = Path.Combine(DebugOutputPath, $"{label}.txt");
        try
        {
            File.WriteAllText(filename, sb.ToString());
            Log($"State dumped to {filename}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameDebugger] Failed to write state: {e.Message}");
        }

        // Also dump ASCII buffer if available
        if (DumpASCIIBuffer && _buffer != null)
        {
            DumpASCIIBufferToFile($"{label}_screen");
        }
    }

    public void DumpASCIIBufferToFile(string label)
    {
        if (_buffer == null)
            return;

        var sb = new StringBuilder();
        for (int y = 0; y < ASCIIBuffer.Height; y++)
        {
            for (int x = 0; x < ASCIIBuffer.Width; x++)
            {
                var cell = _buffer.GetCell(x, y);
                sb.Append(cell.Character);
            }
            sb.AppendLine();
        }

        var filename = Path.Combine(DebugOutputPath, $"{label}.txt");
        try
        {
            File.WriteAllText(filename, sb.ToString());
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameDebugger] Failed to write ASCII buffer: {e.Message}");
        }
    }

    public void DumpSchedulerState(string context)
    {
        Log($"Scheduler state ({context}):");
        Log(TurnManager.Instance?.GetSchedulerDebugInfo() ?? "  TurnManager not available");
    }

    private void WriteLogToFile()
    {
        var filename = Path.Combine(DebugOutputPath, "session_log.txt");
        try
        {
            File.WriteAllLines(filename, _turnLog);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GameDebugger] Failed to write log: {e.Message}");
        }
    }

    /// <summary>
    /// Force a debug dump - can be called from console or keybind.
    /// </summary>
    public void ForceDump()
    {
        DumpGameState($"manual_dump_{DateTime.Now:HHmmss}");
    }
}
