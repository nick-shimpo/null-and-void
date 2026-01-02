using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Core;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.UI;
using NullAndVoid.World;

namespace NullAndVoid.Battlefield;

/// <summary>
/// Controller for the test battlefield scene.
/// Manages diverse enemies, item pickups, and terrain for combat testing.
/// </summary>
public partial class TestBattlefield : Node2D
{
    [Export] public int MapWidth { get; set; } = 80;
    [Export] public int MapHeight { get; set; } = 60;
    [Export] public int WeaponPickupCount { get; set; } = 8;
    [Export] public int AmmoPickupCount { get; set; } = 12;

    private Label? _turnLabel;
    private Label? _healthLabel;
    private Label? _positionLabel;
    private Label? _ammoLabel;
    private Panel? _topBar;
    private Player? _player;
    private TileMapManager? _tileMapManager;
    private Node2D? _entitiesNode;
    private Node2D? _itemsNode;
    private EquipmentBar? _equipmentBar;
    private InventoryScreen? _inventoryScreen;
    private MessageLog? _messageLog;

    private BattlefieldGenerator? _generator;
    private GameMap? _gameMap;

    public override void _Ready()
    {
        // Get references
        _topBar = GetNode<Panel>("UI/HUD/TopBar");
        _turnLabel = GetNode<Label>("UI/HUD/TopBar/TurnLabel");
        _healthLabel = GetNode<Label>("UI/HUD/TopBar/HealthLabel");
        _positionLabel = GetNodeOrNull<Label>("UI/HUD/TopBar/PositionLabel");
        _ammoLabel = GetNodeOrNull<Label>("UI/HUD/TopBar/AmmoLabel");
        _player = GetNode<Player>("Entities/Player");
        _tileMapManager = GetNode<TileMapManager>("TileMapManager");
        _entitiesNode = GetNode<Node2D>("Entities");
        _itemsNode = GetNode<Node2D>("Items");
        _equipmentBar = GetNodeOrNull<EquipmentBar>("UI/HUD/EquipmentBar");
        _inventoryScreen = GetNodeOrNull<InventoryScreen>("UI/InventoryScreen");
        _messageLog = GetNodeOrNull<MessageLog>("UI/HUD/MessageLog");

        // Apply terminal styling to HUD
        ApplyTerminalStyling();

        // Subscribe to events
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
        EventBus.Instance.EntityMoved += OnEntityMoved;

        // Generate and load battlefield
        GenerateBattlefield();

        // Spawn enemies
        SpawnDiverseEnemies();

        // Spawn pickups
        SpawnWeaponPickups();
        SpawnAmmoPickups();

        // Setup inventory and equipment
        SetupInventorySystem();

        // Initial FOV calculation
        UpdateFOV();

        // Start the game
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        UpdateUI();
        LogMessage("[BATTLEFIELD] Combat test initialized. 25 hostiles detected.");
    }

    private void ApplyTerminalStyling()
    {
        if (_topBar != null)
            TerminalTheme.StylePanel(_topBar, highlighted: true);

        if (_healthLabel != null)
            TerminalTheme.StyleLabelGlow(_healthLabel, TerminalTheme.Primary, 14);
        if (_turnLabel != null)
            TerminalTheme.StyleLabelGlow(_turnLabel, TerminalTheme.Primary, 14);
        if (_positionLabel != null)
            TerminalTheme.StyleLabel(_positionLabel, TerminalTheme.PrimaryDim, 14);
    }

    private void GenerateBattlefield()
    {
        if (_tileMapManager == null || _player == null)
            return;

        // Generate battlefield with zones
        _generator = new BattlefieldGenerator(MapWidth, MapHeight);
        _generator.Generate();

        // Create GameMap from generated tiles
        _gameMap = new GameMap(MapWidth, MapHeight);
        var tiles = _generator.GetTiles();
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                _gameMap.SetTile(x, y, tiles[x, y]);
            }
        }
        _gameMap.SetCeilingData(_generator.GetCeilingData());

        // Load map into tile manager
        LoadGameMapIntoTileManager(_gameMap);

        // Position player at spawn point
        _player.GridPosition = _generator.PlayerSpawn;
        _player.UpdateVisualPosition();

        GD.Print($"[Battlefield] Generated {MapWidth}x{MapHeight} map. Player at {_generator.PlayerSpawn}");
    }

    private void LoadGameMapIntoTileManager(GameMap gameMap)
    {
        if (_tileMapManager == null)
            return;

        // Create TileType array from GameMap for TileMapManager
        var mapData = new TileMapManager.TileType[MapWidth, MapHeight];
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                var tile = gameMap.GetTileSafe(x, y);
                if (tile.BlocksMovement)
                    mapData[x, y] = TileMapManager.TileType.Wall;
                else
                    mapData[x, y] = TileMapManager.TileType.Floor;
            }
        }
        _tileMapManager.LoadMap(mapData);
    }

    private void SpawnDiverseEnemies()
    {
        if (_generator == null || _entitiesNode == null)
            return;

        foreach (var (pos, archetype) in _generator.EnemySpawns)
        {
            var enemy = EnemyFactory.CreateEnemy(archetype, pos);
            ConfigureEnemyVisual(enemy, archetype);
            _entitiesNode.AddChild(enemy);
        }

        GD.Print($"[Battlefield] Spawned {_generator.EnemySpawns.Count} enemies");
    }

    private void ConfigureEnemyVisual(Enemy enemy, EnemyArchetype archetype)
    {
        // Enemies are rendered by the ASCII EntityRenderer based on their archetype
        // No visual nodes needed - just add to group and update position
        enemy.AddToGroup("Enemies");
        enemy.UpdateVisualPosition();
    }

    private void SpawnWeaponPickups()
    {
        if (_generator == null || _itemsNode == null)
            return;

        var weaponTypes = new Func<Item>[]
        {
            () => WeaponFactory.CreateNailGun(),
            () => WeaponFactory.CreateRivetCannon(),
            () => WeaponFactory.CreateLaserEmitter(),
            () => WeaponFactory.CreatePlasmaCaster(),
            () => WeaponFactory.CreateFlechetteLauncher(),
            () => WeaponFactory.CreateRailDriver(),
            () => WeaponFactory.CreatePlasmaCutter(),
            () => WeaponFactory.CreateEMPBlade()
        };

        var random = new Random();
        for (int i = 0; i < Math.Min(WeaponPickupCount, _generator.WeaponSpawns.Count); i++)
        {
            var pos = _generator.WeaponSpawns[i];
            var weaponFunc = weaponTypes[random.Next(weaponTypes.Length)];
            var weapon = weaponFunc();

            var pickup = new WorldItem(weapon, pos);
            _itemsNode.AddChild(pickup);
        }

        GD.Print($"[Battlefield] Spawned {Math.Min(WeaponPickupCount, _generator.WeaponSpawns.Count)} weapon pickups");
    }

    private void SpawnAmmoPickups()
    {
        if (_generator == null || _itemsNode == null)
            return;

        var ammoTypes = new Func<Ammunition>[]
        {
            () => AmmoFactory.CreateKineticRounds(20),
            () => AmmoFactory.CreateArmorPiercingRounds(10),
            () => AmmoFactory.CreatePowerCells(15),
            () => AmmoFactory.CreateSeekerFuel(5),
            () => AmmoFactory.CreateOrbitalRockets(2),
        };

        var random = new Random();
        for (int i = 0; i < Math.Min(AmmoPickupCount, _generator.AmmoSpawns.Count); i++)
        {
            var pos = _generator.AmmoSpawns[i];
            var ammoFunc = ammoTypes[random.Next(ammoTypes.Length)];
            var ammo = ammoFunc();

            var pickup = new WorldItem(ammo, pos);
            _itemsNode.AddChild(pickup);
        }

        GD.Print($"[Battlefield] Spawned {Math.Min(AmmoPickupCount, _generator.AmmoSpawns.Count)} ammo pickups");
    }

    private void SetupInventorySystem()
    {
        if (_player == null)
            return;

        // Connect equipment bar to player's equipment
        if (_equipmentBar != null && _player.EquipmentComponent != null)
        {
            _equipmentBar.SetEquipment(_player.EquipmentComponent);
        }

        // Setup inventory screen
        if (_inventoryScreen != null && _player.InventoryComponent != null && _player.EquipmentComponent != null)
        {
            _inventoryScreen.Setup(_player.InventoryComponent, _player.EquipmentComponent);
        }

        // Give player starter items plus extra ammo for testing
        GiveStarterItems();
    }

    private void GiveStarterItems()
    {
        if (_player?.InventoryComponent == null || _player.EquipmentComponent == null)
            return;

        // Weapons
        var meleeWeapon = ItemFactory.CreateStarterWeapon();       // Plasma Cutter
        var rangedWeapon = ItemFactory.CreateStarterRangedWeapon(); // Nail Gun
        var armor = ItemFactory.CreateStarterArmor();

        // Equip weapons
        _player.EquipmentComponent.Equip(meleeWeapon, EquipmentSlotType.Utility, 0);
        _player.EquipmentComponent.Equip(rangedWeapon, EquipmentSlotType.Utility, 1);
        _player.EquipmentComponent.Equip(armor, EquipmentSlotType.Base, 0);

        // Give extra ammo for testing
        _player.InventoryComponent.AddAmmo(AmmoFactory.CreateKineticRounds(50));
        _player.InventoryComponent.AddAmmo(AmmoFactory.CreatePowerCells(30));
        _player.InventoryComponent.AddAmmo(AmmoFactory.CreateSeekerFuel(10));

        // Add some random items to inventory
        for (int i = 0; i < 3; i++)
        {
            var randomItem = ItemFactory.CreateRandomItem();
            _player.InventoryComponent.AddItem(randomItem);
        }

        GD.Print("[Battlefield] Starter items equipped and extra ammo provided");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Toggle inventory on I or Tab
        if (@event.IsActionPressed("open_inventory"))
        {
            _inventoryScreen?.Toggle();
        }
    }

    private void UpdateFOV()
    {
        if (_tileMapManager == null || _player == null)
            return;

        _tileMapManager.UpdateFOV(_player.GridPosition, _player.SightRange);
        UpdateEnemyVisibility();
        UpdateItemVisibility();
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
                bool visible = _tileMapManager.FOV.IsVisible(enemy.GridPosition);
                enemy.Visible = visible;
            }
        }
    }

    private void UpdateItemVisibility()
    {
        if (_tileMapManager == null)
            return;

        var items = GetTree().GetNodesInGroup("WorldItems");
        foreach (var node in items)
        {
            if (node is WorldItem item)
            {
                bool visible = _tileMapManager.FOV.IsVisible(item.GridPosition);
                item.Visible = visible;
            }
        }
    }

    public override void _ExitTree()
    {
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
            CheckForPickups(to);
        }
    }

    private void CheckForPickups(Vector2I position)
    {
        var items = GetTree().GetNodesInGroup("WorldItems");
        foreach (var node in items)
        {
            if (node is WorldItem worldItem && worldItem.GridPosition == position)
            {
                // Auto-pickup ammo
                if (worldItem.IsAmmoPickup)
                {
                    var ammo = worldItem.PickupAmmo();
                    if (ammo != null && _player?.InventoryComponent != null)
                    {
                        _player.InventoryComponent.AddAmmo(ammo);
                        LogMessage($"[PICKUP] {ammo.Name} x{ammo.Quantity}");
                    }
                }
                // Log item pickups (manual pickup via inventory)
                else if (worldItem.IsItemPickup)
                {
                    LogMessage($"[ITEM] {worldItem.GetDescription()} - Press 'G' to pick up");
                }
            }
        }
    }

    private void LogMessage(string message)
    {
        _messageLog?.AddMessage(message);
        GD.Print(message);
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

        if (_ammoLabel != null && _player?.InventoryComponent != null)
        {
            int basicAmmo = _player.InventoryComponent.GetAmmoCount(AmmoType.Basic);
            int energyAmmo = _player.InventoryComponent.GetAmmoCount(AmmoType.Energy);
            _ammoLabel.Text = TerminalTheme.FormatStatus("AMMO", $"K:{basicAmmo} E:{energyAmmo}");
        }
    }
}
