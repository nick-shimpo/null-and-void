using Godot;
using NullAndVoid.Entities;

namespace NullAndVoid.Core;

/// <summary>
/// Main game controller that manages the gameplay scene.
/// </summary>
public partial class Game : Node2D
{
    private Label? _turnLabel;
    private Label? _healthLabel;
    private Player? _player;

    public override void _Ready()
    {
        // Get UI references
        _turnLabel = GetNode<Label>("UI/HUD/TurnLabel");
        _healthLabel = GetNode<Label>("UI/HUD/HealthLabel");
        _player = GetNode<Player>("Entities/Player");

        // Subscribe to events
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;

        // Start the game
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        UpdateUI();
    }

    public override void _ExitTree()
    {
        // Unsubscribe from events
        if (EventBus.Instance != null)
        {
            EventBus.Instance.TurnStarted -= OnTurnStarted;
            EventBus.Instance.EntityDamaged -= OnEntityDamaged;
        }
    }

    private void OnTurnStarted(int turnNumber)
    {
        UpdateUI();
    }

    private void OnEntityDamaged(Node entity, int damage, int remainingHealth)
    {
        if (entity == _player)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (_turnLabel != null)
        {
            _turnLabel.Text = $"Turn: {TurnManager.Instance.CurrentTurn}";
        }

        if (_healthLabel != null && _player != null)
        {
            _healthLabel.Text = $"HP: {_player.CurrentHealth}/{_player.MaxHealth}";
        }
    }
}
