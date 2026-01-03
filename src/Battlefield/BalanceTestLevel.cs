using System.Collections.Generic;
using Godot;
using NullAndVoid.AI;
using NullAndVoid.Combat;
using NullAndVoid.Core;
using NullAndVoid.Destruction;
using NullAndVoid.Effects;
using NullAndVoid.Entities;
using NullAndVoid.Entities.Boss;
using NullAndVoid.Items;
using NullAndVoid.Rendering;
using NullAndVoid.Targeting;
using NullAndVoid.UI;
using NullAndVoid.World;

namespace NullAndVoid.Battlefield;

/// <summary>
/// Balance Test Level - Three-zone level with boss encounter.
/// Zone 1: Outskirts - Open field with light enemies
/// Zone 2: Fortress Approach - Defensive positions with heavy resistance
/// Zone 3: Final Fortress - Boss arena with SENTINEL PRIME
/// </summary>
public partial class BalanceTestLevel : Control
{
    // Rendering components
    private ASCIIRenderer? _renderer;
    private MapRenderer? _mapRenderer;
    private EntityRenderer? _entityRenderer;
    private UIRenderer? _uiRenderer;

    // Targeting
    private TargetingManager? _targetingManager;

    // Visual Effects
    private ParticleSystem? _particleSystem;
    private CombatAnimator? _combatAnimator;

    // Combat UI
    private WeaponBar? _weaponBar;
    private TargetingOverlay? _targetingOverlay;

    // UI Screens
    private ASCIIInventoryScreen? _inventoryScreen;

    // Game state
    private TileMapManager? _tileMapManager;
    private GameMap? _gameMap;
    private DestructionManager? _destructionManager;
    private BalanceTestLevelGenerator? _levelGenerator;
    private ItemPlacement? _itemPlacement;
    private Player? _player;
    private SentinelPrime? _boss;
    private Node2D? _entitiesNode;
    private Node2D? _itemsNode;
    private float _pulsePhase = 0f;

    // Zone tracking
    private ZoneDefinition? _currentZone;
    private int _enemiesDefeated = 0;
    private int _totalEnemies = 0;
    private bool _bossSpawned = false;

    // Alert system
    private AlertManager? _alertManager;

    public override void _Ready()
    {
        // Create the ASCII renderer
        _renderer = new ASCIIRenderer();
        _renderer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_renderer);

        // Create sub-renderers
        _mapRenderer = new MapRenderer(_renderer.Buffer);
        _entityRenderer = new EntityRenderer(_renderer.Buffer, _mapRenderer);
        _uiRenderer = new UIRenderer(_renderer.Buffer);

        // Create targeting manager
        _targetingManager = new TargetingManager(_renderer.Buffer, _mapRenderer);
        _targetingManager.TargetingStarted += OnTargetingStarted;
        _targetingManager.TargetingEnded += OnTargetingEnded;
        _targetingManager.AttackPerformed += OnRangedAttackPerformed;
        _targetingManager.MessageRequested += OnTargetingMessage;

        // Connect MapViewport
        if (_renderer.MapViewport != null)
        {
            _mapRenderer.SetMapViewport(_renderer.MapViewport);
            _mapRenderer.UseMapViewport = true;
            _entityRenderer.SetMapViewport(_renderer.MapViewport);
            _targetingManager.SetMapViewport(_renderer.MapViewport);
        }

        // Create visual effects system
        _particleSystem = new ParticleSystem();
        _combatAnimator = new CombatAnimator(_particleSystem);

        // Create combat UI
        _weaponBar = new WeaponBar(_renderer.Buffer);
        _targetingOverlay = new TargetingOverlay(_renderer.Buffer);

        // Create entity containers
        _entitiesNode = new Node2D { Name = "Entities", Visible = false };
        AddChild(_entitiesNode);

        _itemsNode = new Node2D { Name = "Items", Visible = false };
        AddChild(_itemsNode);

        // Create TileMapManager
        _tileMapManager = new TileMapManager
        {
            Name = "TileMapManager",
            Visible = false
        };
        AddChild(_tileMapManager);

        // Generate level
        GenerateLevel();

        // Create player
        CreatePlayer();

        // Spawn enemies
        SpawnEnemies();

        // Spawn items
        SpawnItems();

        // Start game
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        // Re-register player after StartNewGame reset
        if (_player != null)
        {
            TurnManager.Instance.RegisterScheduledActor(_player);
        }

        // Initialize targeting
        if (_player != null)
        {
            _targetingManager.Initialize(_player, GetTree());
            _weaponBar?.SetPlayer(_player);
            _destructionManager?.TrackPlayer(_player);
        }

        // Initialize alert manager
        _alertManager = new AlertManager();
        _alertManager.CreateDefaultZone(_levelGenerator?.Width ?? 150, _levelGenerator?.Height ?? 55);

        // Subscribe to events
        SubscribeToEvents();

        // Initial FOV
        UpdateFOV();

        // Hook rendering
        _renderer.OnDraw += DrawFrame;

        // Create inventory screen
        CreateInventoryScreen();

        // Display zone message
        if (_currentZone != null)
        {
            _uiRenderer?.AddMessage(_currentZone.EnterMessage ?? $"Entering {_currentZone.Name}", ASCIIColors.AlertWarning);
        }

        // Initial messages
        _uiRenderer?.AddMessage("[BALANCE TEST] Level initialized.", ASCIIColors.Primary);
        _uiRenderer?.AddMessage($"[TACTICAL] {_totalEnemies} hostiles detected across 3 zones.", ASCIIColors.AlertWarning);
        _uiRenderer?.AddMessage("Move: WASD | Fire: [1-9] | Inventory: [I]", ASCIIColors.TextSecondary);

        GD.Print($"[BalanceTestLevel] Level initialized with {_totalEnemies} enemies across 3 zones");
    }

    #region Level Generation

    private void GenerateLevel()
    {
        // Create level generator
        _levelGenerator = new BalanceTestLevelGenerator();
        _levelGenerator.Generate();

        // Update TileMapManager size
        if (_tileMapManager != null)
        {
            _tileMapManager.MapSize = new Vector2I(_levelGenerator.Width, _levelGenerator.Height);
        }

        // Create game map from generated tiles
        _gameMap = new GameMap(_levelGenerator.Width, _levelGenerator.Height);
        var tiles = _levelGenerator.GetTiles();
        var ceilings = _levelGenerator.GetCeilingData();

        for (int y = 0; y < _levelGenerator.Height; y++)
        {
            for (int x = 0; x < _levelGenerator.Width; x++)
            {
                _gameMap.SetTile(x, y, tiles[x, y]);
            }
        }
        _gameMap.SetCeilingData(ceilings);

        // Subscribe to destruction events
        _gameMap.TileDestroyed += OnTileDestroyed;
        _gameMap.TileIgnited += OnTileIgnited;
        _gameMap.ExplosionTriggered += OnExplosionTriggered;

        // Create DestructionManager
        if (_renderer != null)
        {
            _destructionManager = new DestructionManager(
                _gameMap.GetTilesArray(),
                _levelGenerator.Width,
                _levelGenerator.Height,
                _renderer.Buffer
            );
            _destructionManager.OnEntityBurned += OnEntityBurned;
            _destructionManager.OnEntityKnocked += OnEntityKnocked;
        }

        // Load map data into TileMapManager
        LoadGameMapIntoTileManager(_gameMap);

        // Initialize item placement
        _itemPlacement = new ItemPlacement();
        _itemPlacement.PlaceItems(_levelGenerator);

        // Set initial zone
        _currentZone = _levelGenerator.Zones[0];

        GD.Print($"[BalanceTestLevel] Level generated: {_levelGenerator.Width}x{_levelGenerator.Height}");
        GD.Print($"[BalanceTestLevel] Zones: {_levelGenerator.Zones.Count}");
    }

    private void LoadGameMapIntoTileManager(GameMap gameMap)
    {
        if (_tileMapManager == null || _levelGenerator == null)
            return;

        var mapData = new TileMapManager.TileType[_levelGenerator.Width, _levelGenerator.Height];
        for (int y = 0; y < _levelGenerator.Height; y++)
        {
            for (int x = 0; x < _levelGenerator.Width; x++)
            {
                var tile = gameMap.GetTileSafe(x, y);
                mapData[x, y] = tile.BlocksMovement
                    ? TileMapManager.TileType.Wall
                    : TileMapManager.TileType.Floor;
            }
        }
        _tileMapManager.LoadMap(mapData);
    }

    #endregion

    #region Entity Spawning

    private void CreatePlayer()
    {
        if (_levelGenerator == null || _entitiesNode == null)
            return;

        _player = new Player
        {
            GridPosition = _levelGenerator.PlayerSpawn,
            EntityName = "OPERATOR"
        };

        _player.AddToGroup("Player");
        _entitiesNode.AddChild(_player);

        // Give starting equipment
        GiveStartingEquipment(_player);

        // Center camera on player
        _mapRenderer?.CenterOn(_player.GridPosition);

        GD.Print($"[BalanceTestLevel] Player spawned at {_player.GridPosition}");
    }

    private void GiveStartingEquipment(Player player)
    {
        if (player.InventoryComponent == null || player.EquipmentComponent == null)
            return;

        // Starting weapons
        var meleeWeapon = ItemFactory.CreateStarterWeapon();
        var rangedWeapon = ItemFactory.CreateStarterRangedWeapon();
        var armor = ItemFactory.CreateStarterArmor();

        // Equip weapons
        player.EquipmentComponent.Equip(meleeWeapon, EquipmentSlotType.Utility, 0);
        player.EquipmentComponent.Equip(rangedWeapon, EquipmentSlotType.Utility, 1);
        player.EquipmentComponent.Equip(armor, EquipmentSlotType.Base, 0);

        // Starting ammo
        player.InventoryComponent.AddAmmo(AmmoFactory.CreateKineticRounds(100));
        player.InventoryComponent.AddAmmo(AmmoFactory.CreatePowerCells(50));

        // Starting consumables
        player.InventoryComponent.AddItem(ItemFactory.CreatePortableRepairKit());
        player.InventoryComponent.AddItem(ItemFactory.CreateEnergyCell());
    }

    private void SpawnEnemies()
    {
        if (_levelGenerator == null || _entitiesNode == null)
            return;

        foreach (var (pos, archetype, tier) in _levelGenerator.EnemySpawns)
        {
            var enemy = EnemyFactory.CreateEnemy(archetype, pos, tier);
            enemy.AddToGroup("Enemies");
            _entitiesNode.AddChild(enemy);
            _destructionManager?.TrackEnemy(enemy);
            TurnManager.Instance.RegisterScheduledActor(enemy);
            _totalEnemies++;
        }

        GD.Print($"[BalanceTestLevel] Spawned {_totalEnemies} enemies");
    }

    private void SpawnBoss()
    {
        if (_levelGenerator?.BossSpawn == null || _bossSpawned || _entitiesNode == null)
            return;

        _boss = new SentinelPrime
        {
            GridPosition = _levelGenerator.BossSpawn.Value
        };

        _boss.AddToGroup("Enemies");
        _entitiesNode.AddChild(_boss);
        TurnManager.Instance.RegisterScheduledActor(_boss);
        _bossSpawned = true;

        _uiRenderer?.AddMessage("★ SENTINEL PRIME DETECTED ★", ASCIIColors.AlertDanger);

        GD.Print($"[BalanceTestLevel] SENTINEL PRIME spawned at {_boss.GridPosition}");

        // Spawn boss guards
        SpawnBossGuards();
    }

    private void SpawnBossGuards()
    {
        if (_boss == null || _entitiesNode == null || _levelGenerator == null)
            return;

        var zone = _levelGenerator.Zones[2];

        // Spawn elite guards in boss arena
        foreach (var (archetype, count, tier) in zone.EnemyConfig.Spawns)
        {
            for (int i = 0; i < count; i++)
            {
                var pos = FindNearbySpawnPosition(_boss.GridPosition, 5, 10);
                if (pos.HasValue)
                {
                    var guard = EnemyFactory.CreateEnemy(archetype, pos.Value, tier);
                    guard.AddToGroup("Enemies");
                    _entitiesNode.AddChild(guard);
                    _destructionManager?.TrackEnemy(guard);
                    TurnManager.Instance.RegisterScheduledActor(guard);
                    _boss.AddGuard(guard);
                }
            }
        }
    }

    private Vector2I? FindNearbySpawnPosition(Vector2I center, int minDist, int maxDist)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int dx = GD.RandRange(-maxDist, maxDist);
            int dy = GD.RandRange(-maxDist, maxDist);
            var pos = new Vector2I(center.X + dx, center.Y + dy);

            int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
            if (dist < minDist || dist > maxDist)
                continue;

            if (_tileMapManager?.IsWalkable(pos) == true &&
                !EntityGrid.Instance.IsOccupied(pos))
            {
                return pos;
            }
        }
        return null;
    }

    private void SpawnItems()
    {
        if (_itemPlacement == null || _itemsNode == null)
            return;

        foreach (var placedItem in _itemPlacement.PlacedItems)
        {
            var worldItem = new WorldItem(placedItem.Item, placedItem.Position);
            worldItem.AddToGroup("WorldItems");
            _itemsNode.AddChild(worldItem);
        }

        // Also spawn ammo pickups
        SpawnAmmoPickups();

        GD.Print($"[BalanceTestLevel] Spawned {_itemPlacement.PlacedItems.Count} items");
    }

    private void SpawnAmmoPickups()
    {
        if (_levelGenerator == null || _itemsNode == null)
            return;

        var ammoTypes = new System.Func<Ammunition>[]
        {
            () => AmmoFactory.CreateKineticRounds(20),
            () => AmmoFactory.CreateArmorPiercingRounds(10),
            () => AmmoFactory.CreatePowerCells(15),
            () => AmmoFactory.CreateSeekerFuel(5),
        };

        var random = new System.Random();

        // Spawn ammo at designated ammo spawn locations from zones
        foreach (var zone in _levelGenerator.Zones)
        {
            foreach (var pos in zone.ItemConfig.AmmoSpawns)
            {
                var ammoFunc = ammoTypes[random.Next(ammoTypes.Length)];
                var ammo = ammoFunc();

                var pickup = new WorldItem(ammo, pos);
                pickup.AddToGroup("WorldItems");
                _itemsNode.AddChild(pickup);
            }
        }
    }

    private void CreateInventoryScreen()
    {
        _inventoryScreen = new ASCIIInventoryScreen();
        _inventoryScreen.Closed += OnInventoryClosed;

        if (_player != null && _player.InventoryComponent != null && _player.EquipmentComponent != null)
        {
            _inventoryScreen.Setup(_player.InventoryComponent, _player.EquipmentComponent, _player);
        }

        AddChild(_inventoryScreen);
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
        EventBus.Instance.EntityMoved += OnEntityMoved;
        EventBus.Instance.AttackPerformed += OnAttackPerformed;
        EventBus.Instance.EntityDied += OnEntityDied;
    }

    #endregion

    #region Zone Tracking

    private void UpdateCurrentZone()
    {
        if (_player == null || _levelGenerator == null)
            return;

        var newZone = _levelGenerator.GetZoneAt(_player.GridPosition);
        if (newZone != null && newZone != _currentZone)
        {
            OnZoneChanged(_currentZone, newZone);
            _currentZone = newZone;
        }
    }

    private void OnZoneChanged(ZoneDefinition? oldZone, ZoneDefinition newZone)
    {
        GD.Print($"[BalanceTestLevel] Entering zone: {newZone.Name}");

        // Display zone message
        if (newZone.EnterMessage != null)
        {
            _uiRenderer?.AddMessage(newZone.EnterMessage, ASCIIColors.AlertWarning);
        }

        // Spawn boss when entering Zone 3
        if (newZone.Type == ZoneType.FinalFortress && !_bossSpawned)
        {
            SpawnBoss();
        }
    }

    #endregion

    #region Update Loop

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _pulsePhase += dt;
        _entityRenderer?.Update(dt);
        _targetingManager?.Update(dt);
        _particleSystem?.Update(dt);
        _combatAnimator?.Update(dt);
        _gameMap?.Update(dt);
        _destructionManager?.Update(dt);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Handle targeting input first
        if (_targetingManager?.IsTargeting == true)
        {
            if (_targetingManager.HandleTargetingInput(@event))
            {
                GetViewport().SetInputAsHandled();
                _renderer?.Buffer.Invalidate();
                return;
            }
        }

        // Handle inventory toggle
        if (@event.IsActionPressed("open_inventory"))
        {
            _inventoryScreen?.Toggle();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Handle weapon hotkeys when player can act
        if (GameState.Instance.CurrentState == GameState.State.Playing && _player?.CanAct == true)
        {
            if (_targetingManager?.HandleWeaponHotkey(@event) == true)
            {
                GetViewport().SetInputAsHandled();
                _renderer?.Buffer.Invalidate();
                return;
            }
        }

        // Block movement during targeting
        if (_targetingManager?.IsTargeting == true)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void DrawFrame()
    {
        if (_renderer == null || _player == null || _gameMap == null)
            return;

        var buffer = _renderer.Buffer;
        buffer.Clear();

        // Update camera to follow player
        _mapRenderer?.CenterOn(_player.GridPosition);

        // Render map
        _mapRenderer?.Render(_gameMap);
        _mapRenderer?.RenderExplosions(_gameMap);

        // Render destruction effects
        if (_destructionManager != null && _mapRenderer != null)
        {
            _destructionManager.Render(
                ASCIIBuffer.MapStartX,
                ASCIIBuffer.MapStartY,
                _mapRenderer.CameraPosition.X,
                _mapRenderer.CameraPosition.Y,
                ASCIIBuffer.MapWidth,
                ASCIIBuffer.MapHeight
            );
        }

        // Render entities
        RenderEntities();

        // Render world items
        RenderWorldItems();

        // Render particle effects
        if (_particleSystem != null && _mapRenderer != null)
        {
            _particleSystem.Render(_renderer.Buffer, _mapRenderer);
        }

        // Render targeting interface
        _targetingManager?.Render();

        // Finalize the map viewport
        _mapRenderer?.FinalizeViewport();

        // Render combat UI
        if (_targetingManager?.IsTargeting == true)
        {
            _weaponBar?.SetSelectedSlot(_targetingManager.SelectedWeaponSlot, true);
            _targetingOverlay?.Render(_targetingManager.TargetingSystem);
        }
        else
        {
            _weaponBar?.SetSelectedSlot(0, false);
        }
        _weaponBar?.Render();

        // Render sidebar
        bool isTargeting = _targetingManager?.IsTargeting == true;
        _uiRenderer?.Render(_player, TurnManager.Instance.CurrentTurn, skipSidebar: isTargeting);

        // Render zone indicator
        RenderZoneIndicator();

        // Render boss health bar
        if (_boss != null && GodotObject.IsInstanceValid(_boss) && !_boss.IsQueuedForDeletion())
        {
            RenderBossHealthBar();
        }
    }

    private void RenderEntities()
    {
        if (_entityRenderer == null || _player == null || _gameMap == null || _tileMapManager == null)
            return;

        var fov = _gameMap.FOV;

        // Render enemies
        var enemies = GetTree().GetNodesInGroup("Enemies");
        var enemyList = new List<Enemy>();
        foreach (var node in enemies)
        {
            if (node is Enemy enemy && enemy.IsActive)
            {
                enemyList.Add(enemy);
            }
        }
        _entityRenderer.RenderEnemies(enemyList, fov);

        // Render player last (on top)
        _entityRenderer.RenderPlayer(_player, _tileMapManager, _pulsePhase);

        // Update nearby list
        UpdateNearbyList(enemyList);
    }

    private void RenderWorldItems()
    {
        if (_entityRenderer == null || _gameMap == null)
            return;

        var items = GetTree().GetNodesInGroup("WorldItems");
        var worldItems = new List<WorldItem>();
        foreach (var node in items)
        {
            if (node is WorldItem item)
                worldItems.Add(item);
        }

        _entityRenderer.RenderWorldItems(worldItems, _gameMap.FOV);
    }

    private void UpdateNearbyList(List<Enemy> enemies)
    {
        if (_uiRenderer == null || _gameMap == null || _player == null)
            return;

        var fov = _gameMap.FOV;

        var visibleEnemies = new List<Enemy>();
        foreach (var enemy in enemies)
        {
            if (fov.IsVisible(enemy.GridPosition))
            {
                visibleEnemies.Add(enemy);
            }
        }

        _uiRenderer.UpdateNearbyEnemies(visibleEnemies, _player.GridPosition);
    }

    private void RenderZoneIndicator()
    {
        if (_renderer?.Buffer == null || _currentZone == null)
            return;

        string zoneText = $"[{_currentZone.Name}]";
        int x = ASCIIBuffer.Width - zoneText.Length - 2;
        _renderer.Buffer.DrawString(x, 1, zoneText, ASCIIColors.TechTerminal);
    }

    private void RenderBossHealthBar()
    {
        if (_renderer?.Buffer == null || _boss == null)
            return;

        int barWidth = 30;
        int barX = (ASCIIBuffer.Width - barWidth) / 2;
        int barY = 2;

        // Boss name
        string name = $"★ {_boss.EntityName} ★";
        int nameX = (ASCIIBuffer.Width - name.Length) / 2;
        _renderer.Buffer.DrawString(nameX, barY, name, ASCIIColors.AlertDanger);

        // Health bar
        float hpPercent = (float)_boss.CurrentHealth / _boss.MaxHealth;
        int filledWidth = (int)(barWidth * hpPercent);

        string hpBar = new string('█', filledWidth) + new string('░', barWidth - filledWidth);
        _renderer.Buffer.DrawString(barX, barY + 1, $"[{hpBar}]", ASCIIColors.AlertDanger);

        // Shield bar (if applicable)
        if (_boss.MaxShieldValue > 0)
        {
            float shieldPercent = (float)_boss.CurrentShield / _boss.MaxShieldValue;
            int shieldWidth = (int)(barWidth * shieldPercent);
            string shieldBar = new string('▓', shieldWidth) + new string('░', barWidth - shieldWidth);
            _renderer.Buffer.DrawString(barX, barY + 2, $"[{shieldBar}]", ASCIIColors.Energy);
        }

        // Phase indicator
        string phase = $"Phase {_boss.CurrentPhaseNumber}/3";
        int phaseX = (ASCIIBuffer.Width - phase.Length) / 2;
        _renderer.Buffer.DrawString(phaseX, barY + 3, phase, ASCIIColors.TechTerminal);
    }

    private void UpdateFOV()
    {
        if (_player == null)
            return;

        _gameMap?.UpdateFOV(_player.GridPosition, _player.SightRange);
        _renderer?.Buffer.Invalidate();
    }

    #endregion

    #region Event Handlers

    private void OnTargetingStarted()
    {
        _renderer?.Buffer.Invalidate();
    }

    private void OnTargetingEnded()
    {
        _renderer?.Buffer.Invalidate();
    }

    private void OnRangedAttackPerformed(CombatResult result)
    {
        if (_player != null)
        {
            _player.CompleteAction(result.ActionCost);
        }

        // Raise alert for nearby enemies based on attacker position
        if (result.Attacker is Entity attacker)
        {
            _alertManager?.RaiseAlert(attacker.GridPosition, 50);
        }

        _renderer?.Buffer.Invalidate();
    }

    private void OnTargetingMessage(string message, Color color)
    {
        _uiRenderer?.AddMessage(message, color);
        _renderer?.Buffer.Invalidate();
    }

    private void OnTurnStarted(int turnNumber)
    {
        if (_gameMap != null)
        {
            _gameMap.ProcessFireTurn();
            _destructionManager?.ProcessTurn();
        }

        // Zone check
        UpdateCurrentZone();

        // Alert system
        _alertManager?.ProcessTurn();

        CheckForPickups();
        _renderer?.Buffer.Invalidate();
    }

    private void CheckForPickups()
    {
        if (_player == null)
            return;

        var items = GetTree().GetNodesInGroup("WorldItems");
        foreach (var node in items)
        {
            if (node is WorldItem worldItem && worldItem.GridPosition == _player.GridPosition)
            {
                // Auto-pickup ammo
                if (worldItem.IsAmmoPickup)
                {
                    var ammo = worldItem.PickupAmmo();
                    if (ammo != null && _player.InventoryComponent != null)
                    {
                        _player.InventoryComponent.AddAmmo(ammo);
                        _uiRenderer?.AddMessage($"[PICKUP] {ammo.Name} x{ammo.Quantity}", ASCIIColors.AlertSuccess);
                    }
                }
                // Log item pickups
                else if (worldItem.IsItemPickup)
                {
                    _uiRenderer?.AddMessage($"[ITEM] {worldItem.GetDescription()} - Press 'G' to pick up", ASCIIColors.AlertInfo);
                }
            }
        }
    }

    private void OnEntityDamaged(Node entity, int damage, int remainingHealth)
    {
        _entityRenderer?.TriggerFlash(entity);
        if (entity == _player)
        {
            _uiRenderer?.AddMessage($"You take {damage} damage! ({remainingHealth} HP)", ASCIIColors.AlertDanger);
        }
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityMoved(Node entity, Vector2I from, Vector2I to)
    {
        if (entity == _player)
        {
            UpdateFOV();
            UpdateCurrentZone();
            CheckForPickups();
        }
        _renderer?.Buffer.Invalidate();
    }

    private void OnAttackPerformed(Node attacker, Node target, int damage)
    {
        string attackerName = attacker is Entity e1 ? e1.EntityName : "Unknown";
        string targetName = target is Entity e2 ? e2.EntityName : "Unknown";
        _uiRenderer?.AddMessage($"{attackerName} attacks {targetName}! [{damage} DMG]", ASCIIColors.AlertDanger);
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityDied(Node entity)
    {
        if (entity is Enemy enemy)
        {
            _enemiesDefeated++;
            _uiRenderer?.AddMessage($"[DESTROYED] {enemy.EntityName}", ASCIIColors.AlertInfo);

            // Check for boss death
            if (enemy is SentinelPrime)
            {
                OnBossDefeated();
            }
        }
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityBurned(Entity entity, int damage)
    {
        _uiRenderer?.AddMessage($"{entity.EntityName} burns for {damage} damage!", ASCIIColors.AlertDanger);
    }

    private void OnEntityKnocked(Entity entity, Vector2I newPos)
    {
        _uiRenderer?.AddMessage($"{entity.EntityName} is knocked back!", ASCIIColors.AlertWarning);
    }

    private void OnTileDestroyed(Vector2I pos, DestructibleTile tile)
    {
        _uiRenderer?.AddMessage($"Terrain destroyed at {pos}!", ASCIIColors.AlertWarning);
        _renderer?.Buffer.Invalidate();
    }

    private void OnTileIgnited(Vector2I pos)
    {
        _uiRenderer?.AddMessage($"Fire started at {pos}!", ASCIIColors.AlertDanger);
        _renderer?.Buffer.Invalidate();
    }

    private void OnExplosionTriggered(Vector2I pos, ExplosionData explosion)
    {
        _uiRenderer?.AddMessage($"{explosion.Name} detonates!", ASCIIColors.AlertDanger);
        _combatAnimator?.PlayExplosion(pos, explosion.Radius, ASCIIColors.ExplosionOuter);
        _renderer?.Buffer.Invalidate();
    }

    private void OnInventoryClosed()
    {
        // Check for pending action costs from inventory operations
        if (_inventoryScreen != null)
        {
            int pendingCost = _inventoryScreen.GetAndClearPendingActionCost();
            if (pendingCost > 0)
            {
                TurnManager.Instance.EndPlayerTurnWithCost(pendingCost);
            }
        }

        _renderer?.ForceRender();
    }

    private void OnBossDefeated()
    {
        GD.Print("[BalanceTestLevel] SENTINEL PRIME DEFEATED!");
        GD.Print("[BalanceTestLevel] BALANCE TEST COMPLETE");
        _uiRenderer?.AddMessage("★★★ SENTINEL PRIME DESTROYED ★★★", ASCIIColors.AlertInfo);
        _uiRenderer?.AddMessage("BALANCE TEST COMPLETE - VICTORY!", ASCIIColors.Primary);
    }

    #endregion

    public override void _ExitTree()
    {
        if (_renderer != null)
            _renderer.OnDraw -= DrawFrame;

        if (_inventoryScreen != null)
            _inventoryScreen.Closed -= OnInventoryClosed;

        if (_gameMap != null)
        {
            _gameMap.TileDestroyed -= OnTileDestroyed;
            _gameMap.TileIgnited -= OnTileIgnited;
            _gameMap.ExplosionTriggered -= OnExplosionTriggered;
        }

        if (_destructionManager != null)
        {
            _destructionManager.OnEntityBurned -= OnEntityBurned;
            _destructionManager.OnEntityKnocked -= OnEntityKnocked;
        }

        if (_targetingManager != null)
        {
            _targetingManager.TargetingStarted -= OnTargetingStarted;
            _targetingManager.TargetingEnded -= OnTargetingEnded;
            _targetingManager.AttackPerformed -= OnRangedAttackPerformed;
            _targetingManager.MessageRequested -= OnTargetingMessage;
        }

        if (EventBus.Instance != null)
        {
            EventBus.Instance.TurnStarted -= OnTurnStarted;
            EventBus.Instance.EntityDamaged -= OnEntityDamaged;
            EventBus.Instance.EntityMoved -= OnEntityMoved;
            EventBus.Instance.AttackPerformed -= OnAttackPerformed;
            EventBus.Instance.EntityDied -= OnEntityDied;
        }
    }
}
