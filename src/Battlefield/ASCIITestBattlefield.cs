using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Core;
using NullAndVoid.Destruction;
using NullAndVoid.Effects;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Rendering;
using NullAndVoid.Targeting;
using NullAndVoid.UI;
using NullAndVoid.World;

namespace NullAndVoid.Battlefield;

/// <summary>
/// ASCII-based test battlefield controller.
/// Uses BattlefieldGenerator for diverse terrain with ceilings,
/// spawns multiple enemy archetypes and item pickups for testing
/// the advanced combat mechanics (ammunition, seekers, artillery, etc.).
/// </summary>
public partial class ASCIITestBattlefield : Control
{
    [Export] public int MapWidth { get; set; } = 80;
    [Export] public int MapHeight { get; set; } = 60;
    [Export] public int WeaponPickupCount { get; set; } = 8;
    [Export] public int AmmoPickupCount { get; set; } = 12;

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
    private BattlefieldGenerator? _generator;
    private Player? _player;
    private Node2D? _entitiesNode;
    private Node2D? _itemsNode;
    private float _pulsePhase = 0f;

    public override void _Ready()
    {
        // Create the ASCII renderer (uses default font size 24 for 2560x1440)
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

        // Connect MapViewport to MapRenderer, EntityRenderer, and TargetingManager for zoom support
        if (_renderer.MapViewport != null)
        {
            _mapRenderer.SetMapViewport(_renderer.MapViewport);
            _mapRenderer.UseMapViewport = true;
            _entityRenderer.SetMapViewport(_renderer.MapViewport);
            _targetingManager.SetMapViewport(_renderer.MapViewport);
            GD.Print("[ASCIITestBattlefield] MapViewport connected to MapRenderer, EntityRenderer, and TargetingManager");
        }

        // Create visual effects system
        _particleSystem = new ParticleSystem();
        _combatAnimator = new CombatAnimator(_particleSystem);

        // Create combat UI
        _weaponBar = new WeaponBar(_renderer.Buffer);
        _targetingOverlay = new TargetingOverlay(_renderer.Buffer);

        // Create game world containers (hidden - we render via ASCII)
        _entitiesNode = new Node2D { Name = "Entities", Visible = false };
        AddChild(_entitiesNode);

        _itemsNode = new Node2D { Name = "Items", Visible = false };
        AddChild(_itemsNode);

        // Create TileMapManager (hidden - used for collision/pathfinding)
        _tileMapManager = new TileMapManager
        {
            Name = "TileMapManager",
            MapSize = new Vector2I(MapWidth, MapHeight),
            Visible = false
        };
        AddChild(_tileMapManager);

        // Create player
        CreatePlayer();

        // Generate battlefield
        GenerateBattlefield();

        // Start game
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        // Re-register player (was cleared by StartNewGame reset)
        if (_player != null)
        {
            TurnManager.Instance.RegisterScheduledActor(_player);
        }

        // Spawn enemies (after StartNewGame so they stay registered)
        SpawnEnemies();

        // Spawn pickups
        SpawnWeaponPickups();
        SpawnAmmoPickups();

        // Give starter items
        GiveStarterItems();

        // Initialize targeting with player reference
        if (_player != null)
        {
            _targetingManager?.Initialize(_player, GetTree());
            _weaponBar?.SetPlayer(_player);
            _destructionManager?.TrackPlayer(_player);
        }

        // Subscribe to events
        SubscribeToEvents();

        // Initial FOV
        UpdateFOV();

        // Hook rendering
        _renderer.OnDraw += DrawFrame;

        // Create inventory screen
        CreateInventoryScreen();

        // Initial messages
        _uiRenderer.AddMessage("[BATTLEFIELD] Combat test initialized.", ASCIIColors.Primary);
        _uiRenderer.AddMessage($"[TACTICAL] {_generator?.EnemySpawns.Count ?? 0} hostiles detected.", ASCIIColors.AlertWarning);
        _uiRenderer.AddMessage("Move: WASD | Fire: [1-9] | Inventory: [I]", ASCIIColors.TextSecondary);

        GD.Print($"[ASCIITestBattlefield] Initialized with {MapWidth}x{MapHeight} battlefield");
    }

    private void CreatePlayer()
    {
        _player = new Player
        {
            EntityName = "Null",
            GridPosition = new Vector2I(MapWidth / 2, MapHeight / 2)
        };
        _player.AddToGroup("Player");
        _entitiesNode?.AddChild(_player);
    }

    private void GenerateBattlefield()
    {
        if (_tileMapManager == null || _player == null)
            return;

        // Generate battlefield with diverse zones
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

        // Subscribe to destruction events
        _gameMap.TileDestroyed += OnTileDestroyed;
        _gameMap.TileIgnited += OnTileIgnited;
        _gameMap.ExplosionTriggered += OnExplosionTriggered;

        // Create DestructionManager
        if (_renderer != null)
        {
            _destructionManager = new DestructionManager(
                _gameMap.GetTilesArray(),
                MapWidth,
                MapHeight,
                _renderer.Buffer
            );
            _destructionManager.OnEntityBurned += OnEntityBurned;
            _destructionManager.OnEntityKnocked += OnEntityKnocked;
        }

        // Load into TileMapManager for collision checks
        LoadGameMapIntoTileManager(_gameMap);

        // Position player at spawn point
        _player.GridPosition = _generator.PlayerSpawn;

        // Center camera on player
        _mapRenderer?.CenterOn(_player.GridPosition);

        GD.Print($"[ASCIITestBattlefield] Generated {MapWidth}x{MapHeight} map. Player at {_generator.PlayerSpawn}");
    }

    private void LoadGameMapIntoTileManager(GameMap gameMap)
    {
        if (_tileMapManager == null)
            return;

        var mapData = new TileMapManager.TileType[MapWidth, MapHeight];
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                var tile = gameMap.GetTileSafe(x, y);
                mapData[x, y] = tile.BlocksMovement
                    ? TileMapManager.TileType.Wall
                    : TileMapManager.TileType.Floor;
            }
        }
        _tileMapManager.LoadMap(mapData);
    }

    private void SpawnEnemies()
    {
        if (_generator == null || _entitiesNode == null)
            return;

        foreach (var (pos, archetype) in _generator.EnemySpawns)
        {
            var enemy = EnemyFactory.CreateEnemy(archetype, pos);
            enemy.AddToGroup("Enemies");
            _entitiesNode.AddChild(enemy);
            _destructionManager?.TrackEnemy(enemy);
        }

        GD.Print($"[ASCIITestBattlefield] Spawned {_generator.EnemySpawns.Count} enemies");
    }

    private void SpawnWeaponPickups()
    {
        if (_generator == null || _itemsNode == null)
            return;

        var weaponTypes = new System.Func<Item>[]
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

        var random = new System.Random();
        for (int i = 0; i < System.Math.Min(WeaponPickupCount, _generator.WeaponSpawns.Count); i++)
        {
            var pos = _generator.WeaponSpawns[i];
            var weaponFunc = weaponTypes[random.Next(weaponTypes.Length)];
            var weapon = weaponFunc();

            var pickup = new WorldItem(weapon, pos);
            _itemsNode.AddChild(pickup);
        }

        GD.Print($"[ASCIITestBattlefield] Spawned {System.Math.Min(WeaponPickupCount, _generator.WeaponSpawns.Count)} weapon pickups");
    }

    private void SpawnAmmoPickups()
    {
        if (_generator == null || _itemsNode == null)
            return;

        var ammoTypes = new System.Func<Ammunition>[]
        {
            () => AmmoFactory.CreateKineticRounds(20),
            () => AmmoFactory.CreateArmorPiercingRounds(10),
            () => AmmoFactory.CreatePowerCells(15),
            () => AmmoFactory.CreateSeekerFuel(5),
            () => AmmoFactory.CreateOrbitalRockets(2),
        };

        var random = new System.Random();
        for (int i = 0; i < System.Math.Min(AmmoPickupCount, _generator.AmmoSpawns.Count); i++)
        {
            var pos = _generator.AmmoSpawns[i];
            var ammoFunc = ammoTypes[random.Next(ammoTypes.Length)];
            var ammo = ammoFunc();

            var pickup = new WorldItem(ammo, pos);
            _itemsNode.AddChild(pickup);
        }

        GD.Print($"[ASCIITestBattlefield] Spawned {System.Math.Min(AmmoPickupCount, _generator.AmmoSpawns.Count)} ammo pickups");
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

        GD.Print("[ASCIITestBattlefield] Starter items equipped and extra ammo provided");
    }

    private void CreateInventoryScreen()
    {
        _inventoryScreen = new ASCIIInventoryScreen();
        _inventoryScreen.Closed += OnInventoryClosed;

        if (_player != null)
        {
            _inventoryScreen.Setup(_player.InventoryComponent!, _player.EquipmentComponent!, _player);
        }

        AddChild(_inventoryScreen);
    }

    private void OnInventoryClosed()
    {
        // Check for and consume any pending action costs from inventory operations
        if (_inventoryScreen != null)
        {
            int pendingCost = _inventoryScreen.GetAndClearPendingActionCost();
            if (pendingCost > 0)
            {
                GD.Print($"[Battlefield] Consuming {pendingCost} AP from inventory actions");
                TurnManager.Instance.EndPlayerTurnWithCost(pendingCost);
            }
        }

        _renderer?.ForceRender();
    }

    private void SubscribeToEvents()
    {
        EventBus.Instance.TurnStarted += OnTurnStarted;
        EventBus.Instance.EntityDamaged += OnEntityDamaged;
        EventBus.Instance.EntityMoved += OnEntityMoved;
        EventBus.Instance.AttackPerformed += OnAttackPerformed;
        EventBus.Instance.EntityDied += OnEntityDied;
    }

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
        // Handle targeting input FIRST (if in targeting mode) - before inventory/other UI
        // This ensures Tab cycles targets instead of opening inventory
        if (_targetingManager?.IsTargeting == true)
        {
            if (_targetingManager.HandleTargetingInput(@event))
            {
                GetViewport().SetInputAsHandled();
                _renderer?.Buffer.Invalidate();
                return;
            }
        }

        // Handle inventory toggle (but not during targeting - handled above)
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

        // Handle zoom controls
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Equal:
                case Key.Plus:
                case Key.KpAdd:
                    _renderer?.ZoomMapIn();
                    _uiRenderer?.AddMessage($"Map zoom: {_renderer?.GetMapZoom():F0}x", ASCIIColors.TextSecondary);
                    GetViewport().SetInputAsHandled();
                    _renderer?.Buffer.Invalidate();
                    return;

                case Key.Minus:
                case Key.KpSubtract:
                    _renderer?.ZoomMapOut();
                    _uiRenderer?.AddMessage($"Map zoom: {_renderer?.GetMapZoom():F0}x", ASCIIColors.TextSecondary);
                    GetViewport().SetInputAsHandled();
                    _renderer?.Buffer.Invalidate();
                    return;

                case Key.Key0:
                case Key.Kp0:
                    if (_targetingManager?.IsTargeting != true)
                    {
                        _renderer?.ResetMapZoom();
                        _uiRenderer?.AddMessage("Map zoom reset", ASCIIColors.TextSecondary);
                        GetViewport().SetInputAsHandled();
                        _renderer?.Buffer.Invalidate();
                    }
                    return;
            }
        }

        // Block movement during targeting
        if (_targetingManager?.IsTargeting == true)
        {
            GetViewport().SetInputAsHandled();
            return;
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

        // Render targeting interface BEFORE finalizing viewport (so it appears on screen)
        _targetingManager?.Render();

        // Finalize the map viewport (convert buffer to display) after all map content including targeting
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

        _uiRenderer?.Render(_player, TurnManager.Instance.CurrentTurn);
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
        if (_renderer == null || _mapRenderer == null || _gameMap == null)
            return;

        var buffer = _renderer.Buffer;
        var fov = _gameMap.FOV;
        var items = GetTree().GetNodesInGroup("WorldItems");

        foreach (var node in items)
        {
            if (node is WorldItem item && fov.IsVisible(item.GridPosition))
            {
                int screenX = ASCIIBuffer.MapStartX + (item.GridPosition.X - _mapRenderer.CameraPosition.X);
                int screenY = ASCIIBuffer.MapStartY + (item.GridPosition.Y - _mapRenderer.CameraPosition.Y);

                if (screenX >= ASCIIBuffer.MapStartX && screenX < ASCIIBuffer.MapStartX + ASCIIBuffer.MapWidth &&
                    screenY >= ASCIIBuffer.MapStartY && screenY < ASCIIBuffer.MapStartY + ASCIIBuffer.MapHeight)
                {
                    buffer.SetCell(screenX, screenY, item.DisplayChar, item.DisplayColor);
                }
            }
        }
    }

    private void UpdateNearbyList(List<Enemy> enemies)
    {
        if (_uiRenderer == null || _gameMap == null)
            return;

        var nearby = new List<(char symbol, string name, Color color)>();
        var fov = _gameMap.FOV;

        foreach (var enemy in enemies)
        {
            if (fov.IsVisible(enemy.GridPosition))
            {
                var healthPercent = enemy.HealthComponent != null
                    ? enemy.HealthComponent.CurrentHealth / (float)enemy.MaxHealth
                    : 1f;
                string healthDesc = healthPercent < 0.3f ? " (critical)" :
                                   healthPercent < 0.6f ? " (wounded)" : "";

                char symbol = enemy.EntityName.ToLower().Contains("guard") ? ASCIIChars.Guard :
                             enemy.EntityName.ToLower().Contains("sentry") ? ASCIIChars.Sentry :
                             ASCIIChars.Drone;

                nearby.Add((symbol, $"{enemy.EntityName}{healthDesc}", ASCIIColors.Enemy));
            }
        }

        _uiRenderer.UpdateNearby(nearby);
    }

    private void UpdateFOV()
    {
        if (_player == null)
            return;

        _gameMap?.UpdateFOV(_player.GridPosition, _player.SightRange);
        _renderer?.Buffer.Invalidate();
    }

    // Event handlers
    private void OnTurnStarted(int turnNumber)
    {
        if (_gameMap != null)
        {
            _gameMap.ProcessFireTurn();
            _destructionManager?.ProcessTurn();
        }
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
        if (entity is Entity e)
        {
            _uiRenderer?.AddMessage($"[DESTROYED] {e.EntityName}", ASCIIColors.AlertInfo);
            _entityRenderer?.RemoveEntity(entity);
        }
        _renderer?.Buffer.Invalidate();
    }

    private void OnTileDestroyed(Vector2I pos, DestructibleTile tile)
    {
        _uiRenderer?.AddMessage($"Terrain destroyed at {pos}!", ASCIIColors.AlertWarning);
        _renderer?.Buffer.Invalidate();
    }

    private void OnTileIgnited(Vector2I pos)
    {
        _uiRenderer?.AddMessage($"Fire started at {pos}!", ASCIIColors.FireFlame);
        _renderer?.Buffer.Invalidate();
    }

    private void OnExplosionTriggered(Vector2I pos, ExplosionData explosion)
    {
        _uiRenderer?.AddMessage($"{explosion.Name} detonates!", ASCIIColors.AlertDanger);
        _combatAnimator?.PlayExplosion(pos, explosion.Radius, ASCIIColors.ExplosionOuter);
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityBurned(Entity entity, int damage)
    {
        _uiRenderer?.AddMessage($"{entity.EntityName} burns! [{damage} DMG]", ASCIIColors.FireFlame);
        _entityRenderer?.TriggerFlash(entity);
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityKnocked(Entity entity, Vector2I knockback)
    {
        int distance = Mathf.Abs(knockback.X) + Mathf.Abs(knockback.Y);
        _uiRenderer?.AddMessage($"{entity.EntityName} knocked back {distance} tiles!", ASCIIColors.AlertWarning);
        _renderer?.Buffer.Invalidate();
    }

    // Targeting event handlers
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

        _combatAnimator?.PlayAttackAnimation(result);

        foreach (var attackResult in result.Results)
        {
            _uiRenderer?.AddMessage(attackResult.Message,
                attackResult.Hit ? ASCIIColors.AlertSuccess : ASCIIColors.AlertWarning);
        }

        foreach (var attackResult in result.Results)
        {
            if (attackResult.Hit && attackResult.Target != null)
            {
                _entityRenderer?.TriggerFlash(attackResult.Target);
            }
        }

        _renderer?.Buffer.Invalidate();
    }

    private void OnTargetingMessage(string message, Color color)
    {
        _uiRenderer?.AddMessage(message, color);
        _renderer?.Buffer.Invalidate();
    }
}
