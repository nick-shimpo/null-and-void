using Godot;
using NullAndVoid.Entities;
using NullAndVoid.World;
using NullAndVoid.UI;
using NullAndVoid.Items;

namespace NullAndVoid.Core;

/// <summary>
/// Main game controller that manages the gameplay scene.
/// </summary>
public partial class Game : Node2D
{
    [Export] public int MapWidth { get; set; } = 40;
    [Export] public int MapHeight { get; set; } = 30;
    [Export] public int EnemyCount { get; set; } = 5;

    private Label? _turnLabel;
    private Label? _healthLabel;
    private Label? _positionLabel;
    private Panel? _topBar;
    private Player? _player;
    private TileMapManager? _tileMapManager;
    private Node2D? _entitiesNode;
    private EquipmentBar? _equipmentBar;
    private InventoryScreen? _inventoryScreen;

    public override void _Ready()
    {
        // Get references
        _topBar = GetNode<Panel>("UI/HUD/TopBar");
        _turnLabel = GetNode<Label>("UI/HUD/TopBar/TurnLabel");
        _healthLabel = GetNode<Label>("UI/HUD/TopBar/HealthLabel");
        _positionLabel = GetNodeOrNull<Label>("UI/HUD/TopBar/PositionLabel");
        _player = GetNode<Player>("Entities/Player");
        _tileMapManager = GetNode<TileMapManager>("TileMapManager");
        _entitiesNode = GetNode<Node2D>("Entities");
        _equipmentBar = GetNode<EquipmentBar>("UI/HUD/EquipmentBar");
        _inventoryScreen = GetNode<InventoryScreen>("UI/InventoryScreen");

        // Apply terminal styling to HUD
        ApplyTerminalStyling();

        // Subscribe to events
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
        EventBus.Instance.EntityMoved += OnEntityMoved;

        // Generate and load map
        GenerateMap();

        // Spawn enemies
        SpawnEnemies();

        // Setup inventory and equipment UI
        SetupInventorySystem();

        // Initial FOV calculation
        UpdateFOV();

        // Start the game
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        UpdateUI();
    }

    private void ApplyTerminalStyling()
    {
        // Style TopBar with glow
        if (_topBar != null)
            TerminalTheme.StylePanel(_topBar, highlighted: true);

        // Style labels with glow effects
        if (_healthLabel != null)
            TerminalTheme.StyleLabelGlow(_healthLabel, TerminalTheme.Primary, 14);
        if (_turnLabel != null)
            TerminalTheme.StyleLabelGlow(_turnLabel, TerminalTheme.Primary, 14);
        if (_positionLabel != null)
            TerminalTheme.StyleLabel(_positionLabel, TerminalTheme.PrimaryDim, 14);
    }

    private void SetupInventorySystem()
    {
        if (_player == null || _equipmentBar == null || _inventoryScreen == null)
            return;

        // Connect equipment bar to player's equipment
        if (_player.EquipmentComponent != null)
        {
            _equipmentBar.SetEquipment(_player.EquipmentComponent);
        }

        // Setup inventory screen
        if (_player.InventoryComponent != null && _player.EquipmentComponent != null)
        {
            _inventoryScreen.Setup(_player.InventoryComponent, _player.EquipmentComponent);
        }

        // Give player some starter items
        GiveStarterItems();
    }

    private void GiveStarterItems()
    {
        if (_player?.InventoryComponent == null || _player.EquipmentComponent == null)
            return;

        // Give 2 starter items and equip them
        var blaster = ItemFactory.CreateStarterWeapon();
        var plating = ItemFactory.CreateStarterArmor();

        // Equip starter items
        _player.EquipmentComponent.Equip(blaster, EquipmentSlotType.Core, 0);
        _player.EquipmentComponent.Equip(plating, EquipmentSlotType.Base, 0);

        // Add a few random items to inventory for testing
        for (int i = 0; i < 3; i++)
        {
            var randomItem = ItemFactory.CreateRandomItem();
            _player.InventoryComponent.AddItem(randomItem);
        }

        GD.Print("Starter items equipped and inventory populated");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Toggle inventory on I or Tab
        if (@event.IsActionPressed("open_inventory"))
        {
            _inventoryScreen?.Toggle();
        }
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

    private void SpawnEnemies()
    {
        if (_tileMapManager == null || _player == null || _entitiesNode == null)
            return;

        for (int i = 0; i < EnemyCount; i++)
        {
            var spawnPos = _tileMapManager.FindRandomWalkablePosition();
            if (spawnPos == null)
                continue;

            // Don't spawn too close to player
            var dist = Mathf.Abs(spawnPos.Value.X - _player.GridPosition.X) +
                       Mathf.Abs(spawnPos.Value.Y - _player.GridPosition.Y);
            if (dist < 5)
            {
                i--; // Try again
                continue;
            }

            // Create enemy
            var enemy = CreateEnemy(spawnPos.Value, $"Drone {i + 1}");
            _entitiesNode.AddChild(enemy);
        }

        GD.Print($"Spawned {EnemyCount} enemies");
    }

    private Enemy CreateEnemy(Vector2I position, string name)
    {
        var enemy = new Enemy
        {
            EntityName = name,
            GridPosition = position,
            MaxHealth = 15,
            AttackDamage = 5,
            SightRange = 6
        };

        // Add visual representation
        var sprite = new ColorRect
        {
            Color = new Color(1.0f, 0.2f, 0.2f),
            Size = new Vector2(20, 20),
            Position = new Vector2(-10, -10)
        };
        enemy.AddChild(sprite);

        // Add to enemies group
        enemy.AddToGroup("Enemies");

        // Set initial position
        enemy.UpdateVisualPosition();

        return enemy;
    }

    private void UpdateFOV()
    {
        if (_tileMapManager == null || _player == null)
            return;

        _tileMapManager.UpdateFOV(_player.GridPosition, _player.SightRange);
        UpdateEnemyVisibility();
    }

    private void UpdateEnemyVisibility()
    {
        if (_tileMapManager == null)
            return;

        var enemies = GetTree().GetNodesInGroup("Enemies");
        foreach (var node in enemies)
        {
            if (node is Enemy enemy)
            {
                // Hide enemies not in FOV
                bool visible = _tileMapManager.FOV.IsVisible(enemy.GridPosition);
                enemy.Visible = visible;
            }
        }
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
            UpdateFOV();
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (_turnLabel != null)
        {
            _turnLabel.Text = TerminalTheme.FormatStatus("TURN", TurnManager.Instance.CurrentTurn.ToString());
        }

        if (_healthLabel != null && _player != null)
        {
            _healthLabel.Text = TerminalTheme.FormatStatus("HP", $"{_player.CurrentHealth}/{_player.MaxHealth}");

            // Color based on health percentage - update glow color
            float healthPercent = (float)_player.CurrentHealth / _player.MaxHealth;
            Color healthColor = healthPercent > 0.5f ? TerminalTheme.Primary :
                               healthPercent > 0.25f ? TerminalTheme.AlertWarning :
                               TerminalTheme.AlertDanger;
            TerminalTheme.StyleLabelGlow(_healthLabel, healthColor, 14);
        }

        if (_positionLabel != null && _player != null)
        {
            _positionLabel.Text = TerminalTheme.FormatStatus("POS", $"{_player.GridPosition.X},{_player.GridPosition.Y}");
        }
    }
}
