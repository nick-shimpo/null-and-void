using Godot;
using NullAndVoid.Entities;
using NullAndVoid.World;

namespace NullAndVoid.Core;

/// <summary>
/// Main game controller that manages the gameplay scene.
/// </summary>
public partial class Game : Node2D
{
    [Export] public int MapWidth { get; set; } = 40;
    [Export] public int MapHeight { get; set; } = 30;

    private Label? _turnLabel;
    private Label? _healthLabel;
    private Label? _positionLabel;
    private Player? _player;
    private TileMapManager? _tileMapManager;

    public override void _Ready()
    {
        // Get references
        _turnLabel = GetNode<Label>("UI/HUD/TopBar/TurnLabel");
        _healthLabel = GetNode<Label>("UI/HUD/TopBar/HealthLabel");
        _positionLabel = GetNodeOrNull<Label>("UI/HUD/TopBar/PositionLabel");
        _player = GetNode<Player>("Entities/Player");
        _tileMapManager = GetNode<TileMapManager>("TileMapManager");

        // Subscribe to events
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
        EventBus.Instance.EntityMoved += OnEntityMoved;

        // Generate and load map
        GenerateMap();

        // Start the game
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        UpdateUI();
    }

    private void GenerateMap()
    {
        if (_tileMapManager == null || _player == null)
            return;

        // Generate a simple dungeon
        var mapData = SimpleMapGenerator.GenerateSimpleDungeon(MapWidth, MapHeight);
        _tileMapManager.LoadMap(mapData);

        // Position player at spawn point
        var spawnPos = SimpleMapGenerator.GetSpawnPosition(mapData);
        _player.GridPosition = spawnPos;
        _player.UpdateVisualPosition();

        GD.Print($"Map generated. Player spawned at {spawnPos}");
    }

    public override void _ExitTree()
    {
        // Unsubscribe from events
        if (EventBus.Instance != null)
        {
            EventBus.Instance.TurnStarted -= OnTurnStarted;
            EventBus.Instance.EntityDamaged -= OnEntityDamaged;
            EventBus.Instance.EntityMoved -= OnEntityMoved;
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

    private void OnEntityMoved(Node entity, Vector2I from, Vector2I to)
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

        if (_positionLabel != null && _player != null)
        {
            _positionLabel.Text = $"Pos: {_player.GridPosition}";
        }
    }
}
