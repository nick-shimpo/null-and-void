using Godot;
using System.Collections.Generic;
using NullAndVoid.Core;

namespace NullAndVoid.UI;

/// <summary>
/// Displays game messages to the player (combat, events, etc.)
/// </summary>
public partial class MessageLog : Control
{
    [Export] public int MaxMessages { get; set; } = 5;
    [Export] public Color DefaultColor { get; set; } = new Color(0.8f, 0.8f, 0.8f);
    [Export] public Color DamageColor { get; set; } = new Color(1.0f, 0.3f, 0.3f);
    [Export] public Color HealColor { get; set; } = new Color(0.3f, 1.0f, 0.3f);
    [Export] public Color InfoColor { get; set; } = new Color(0.3f, 0.7f, 1.0f);

    private VBoxContainer? _container;
    private readonly Queue<Label> _messageLabels = new();

    public override void _Ready()
    {
        _container = GetNode<VBoxContainer>("VBoxContainer");

        // Subscribe to events
        EventBus.Instance.AttackPerformed += OnAttackPerformed;
        EventBus.Instance.EntityDied += OnEntityDied;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
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
            Text = text,
            Modulate = color ?? DefaultColor,
            HorizontalAlignment = HorizontalAlignment.Left
        };

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
        string attackerName = attacker is Entities.Entity e1 ? e1.EntityName : "Something";
        string targetName = target is Entities.Entity e2 ? e2.EntityName : "something";

        AddMessage($"{attackerName} hits {targetName} for {damage} damage!", DamageColor);
    }

    private void OnEntityDied(Node entity)
    {
        string entityName = entity is Entities.Entity e ? e.EntityName : "Something";
        AddMessage($"{entityName} is destroyed!", InfoColor);
    }

    private void OnEntityDamaged(Node entity, int damage, int remainingHealth)
    {
        // This is handled by OnAttackPerformed for combat
        // Could add other damage sources here
    }
}
