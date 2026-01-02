using System.Collections.Generic;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.UI;

/// <summary>
/// Terminal-styled message log displaying game messages to the player.
/// </summary>
public partial class MessageLog : Control
{
    [Export] public int MaxMessages { get; set; } = 15;

    private Panel? _panel;
    private VBoxContainer? _container;
    private readonly Queue<Label> _messageLabels = new();

    public override void _Ready()
    {
        _panel = GetNode<Panel>("Panel");
        _container = GetNode<VBoxContainer>("VBoxContainer");

        // Apply terminal styling
        if (_panel != null)
            TerminalTheme.StylePanel(_panel);

        // Subscribe to events
        EventBus.Instance.AttackPerformed += OnAttackPerformed;
        EventBus.Instance.EntityDied += OnEntityDied;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;

        // Add initial message
        AddMessage("System initialized. Ready for input.", TerminalTheme.PrimaryDim);
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.AttackPerformed -= OnAttackPerformed;
            EventBus.Instance.EntityDied -= OnEntityDied;
            EventBus.Instance.EntityDamaged -= OnEntityDamaged;
        }
    }

    public void AddMessage(string text, Color? color = null)
    {
        if (_container == null)
            return;

        var label = new Label
        {
            Text = $"> {text}",
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Apply terminal styling with glow
        TerminalTheme.StyleLabelGlow(label, color ?? TerminalTheme.Primary, 12);

        _container.AddChild(label);
        _messageLabels.Enqueue(label);

        // Remove old messages
        while (_messageLabels.Count > MaxMessages)
        {
            var oldLabel = _messageLabels.Dequeue();
            oldLabel.QueueFree();
        }
    }

    private void OnAttackPerformed(Node attacker, Node target, int damage)
    {
        string attackerName = attacker is Entities.Entity e1 ? e1.EntityName : "Unknown";
        string targetName = target is Entities.Entity e2 ? e2.EntityName : "unknown";

        AddMessage($"{attackerName} >> {targetName} [{damage} DMG]", TerminalTheme.AlertDanger);
    }

    private void OnEntityDied(Node entity)
    {
        string entityName = entity is Entities.Entity e ? e.EntityName : "Unknown";
        AddMessage($"[DESTROYED] {entityName}", TerminalTheme.AlertInfo);
    }

    private void OnEntityDamaged(Node entity, int damage, int remainingHealth)
    {
        // This is handled by OnAttackPerformed for combat
        // Could add other damage sources here
    }
}
