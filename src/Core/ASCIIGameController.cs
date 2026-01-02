using System.Collections.Generic;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Debug;
using NullAndVoid.Destruction;
using NullAndVoid.Effects;
using NullAndVoid.Entities;
using NullAndVoid.Items;
using NullAndVoid.Rendering;
using NullAndVoid.Targeting;
using NullAndVoid.UI;
using NullAndVoid.World;

namespace NullAndVoid.Core;

/// <summary>
/// Main game controller using pure ASCII rendering.
/// Integrates with existing game systems (TileMapManager, Player, Enemy, etc.)
/// but renders everything through the ASCII buffer system.
/// All controls are keyboard-driven with vim-style navigation support.
/// </summary>
public partial class ASCIIGameController : Control
{
    [Export] public int MapWidth { get; set; } = 80;
    [Export] public int MapHeight { get; set; } = 50;
    [Export] public int EnemyCount { get; set; } = 8;

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

    // UI Screens
    private ASCIIInventoryScreen? _inventoryScreen;

    // Combat UI
    private WeaponBar? _weaponBar;
    private TargetingOverlay? _targetingOverlay;

    // Game state
    private TileMapManager? _tileMapManager;
    private GameMap? _gameMap;  // New destructible terrain system
    private DestructionManager? _destructionManager;  // Central destruction coordinator
    private Player? _player;
    private Node2D? _entitiesNode;
    private float _pulsePhase = 0f;

    // Feature toggles
    [Export] public bool UseDestructibleTerrain { get; set; } = true;
    [Export] public bool FireSimulationEnabled { get; set; } = true;
    [Export] public bool SmokeSimulationEnabled { get; set; } = true;
    [Export] public bool StructuralCollapseEnabled { get; set; } = true;
    [Export] public bool DebugModeEnabled { get; set; } = true;

    // Debug
    private GameDebugger? _debugger;

    public override void _Ready()
    {
        // Create debug system first if enabled
        if (DebugModeEnabled)
        {
            _debugger = new GameDebugger
            {
                Enabled = true,
                DumpEveryTurn = true,
                DumpASCIIBuffer = true,
                DebugOutputPath = "debug_output"
            };
            AddChild(_debugger);
        }

        // Create the ASCII renderer (uses default font size 24 for 2560x1440)
        _renderer = new ASCIIRenderer();
        _renderer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_renderer);

        // Create sub-renderers
        _mapRenderer = new MapRenderer(_renderer.Buffer);
        _entityRenderer = new EntityRenderer(_renderer.Buffer, _mapRenderer);

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
            GD.Print("[ASCIIGameController] MapViewport connected to MapRenderer, EntityRenderer, and TargetingManager");
        }

        // Create visual effects system
        _particleSystem = new ParticleSystem();
        _combatAnimator = new CombatAnimator(_particleSystem);

        // Create combat UI
        _weaponBar = new WeaponBar(_renderer.Buffer);
        _targetingOverlay = new TargetingOverlay(_renderer.Buffer);

        // Give debugger access to buffer
        _debugger?.SetBuffer(_renderer.Buffer);
        _uiRenderer = new UIRenderer(_renderer.Buffer);

        // Create game world container (hidden - we render via ASCII)
        _entitiesNode = new Node2D { Name = "Entities", Visible = false };
        AddChild(_entitiesNode);

        // Create TileMapManager (hidden - we render via ASCII)
        _tileMapManager = new TileMapManager
        {
            Name = "TileMapManager",
            MapSize = new Vector2I(MapWidth, MapHeight),
            Visible = false
        };
        AddChild(_tileMapManager);

        // Create player
        CreatePlayer();

        // Generate map (needs player for spawn position)
        GenerateMap();

        // Start game - this resets the scheduler, so we need to spawn entities AFTER
        GameState.Instance.TransitionTo(GameState.State.Playing);
        TurnManager.Instance.StartNewGame();

        // Re-register player (was cleared by StartNewGame reset)
        if (_player != null)
        {
            TurnManager.Instance.RegisterScheduledActor(_player);
        }

        // Spawn enemies (after StartNewGame so they stay registered)
        SpawnEnemies();

        // Give starter items
        GiveStarterItems();

        // Initialize targeting with player reference
        if (_player != null)
        {
            _targetingManager?.Initialize(_player, GetTree());
            _weaponBar?.SetPlayer(_player);

            // Register player with destruction manager for fire/knockback effects
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
        _uiRenderer.AddMessage("System initialized. Welcome to Null & Void.", ASCIIColors.Primary);
        _uiRenderer.AddMessage("Move: WASD | Diagonals: QEZC | Wait: [.] | Fire: [1-9]", ASCIIColors.TextSecondary);

        GD.Print("[ASCIIGameController] Game initialized with ASCII rendering");

        // Debug: dump initial state
        _debugger?.DumpGameState("game_initialized");
        _debugger?.DumpSchedulerState("after_init");
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
                GD.Print($"[Game] Consuming {pendingCost} AP from inventory actions");
                TurnManager.Instance.EndPlayerTurnWithCost(pendingCost);
            }
        }

        // Force immediate redraw to update weapon bar and other UI
        _renderer?.ForceRender();
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

    private void GenerateMap()
    {
        if (_tileMapManager == null || _player == null)
            return;

        var mapData = SimpleMapGenerator.GenerateSimpleDungeon(MapWidth, MapHeight);
        _tileMapManager.LoadMap(mapData);

        // Create GameMap with destructible terrain
        if (UseDestructibleTerrain)
        {
            _gameMap = new GameMap(MapWidth, MapHeight);
            _gameMap.LoadFromTileTypes(mapData);

            // Subscribe to destruction events
            _gameMap.TileDestroyed += OnTileDestroyed;
            _gameMap.TileIgnited += OnTileIgnited;
            _gameMap.ExplosionTriggered += OnExplosionTriggered;

            // Create DestructionManager to coordinate smoke, collapse, and entity damage
            if (_renderer != null)
            {
                _destructionManager = new DestructionManager(
                    _gameMap.GetTilesArray(),
                    MapWidth,
                    MapHeight,
                    _renderer.Buffer
                );

                // Subscribe to destruction manager events
                _destructionManager.OnTileDamaged += OnDestructionTileDamaged;
                _destructionManager.OnTileDestroyed += OnDestructionTileDestroyed;
                _destructionManager.OnExplosion += OnDestructionExplosion;
                _destructionManager.OnEntityBurned += OnEntityBurned;
                _destructionManager.OnEntityKnocked += OnEntityKnocked;

                GD.Print("[ASCIIGameController] DestructionManager initialized");
            }
        }

        var spawnPos = SimpleMapGenerator.GetSpawnPosition(mapData);
        _player.GridPosition = spawnPos;

        // Center camera on player
        _mapRenderer?.CenterOn(_player.GridPosition);

        GD.Print($"[ASCIIGameController] Map generated, player at {spawnPos}");
        if (UseDestructibleTerrain)
        {
            GD.Print("[ASCIIGameController] Destructible terrain enabled");
        }
    }

    // Destruction event handlers
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

        // Play explosion particle effect
        _combatAnimator?.PlayExplosion(pos, explosion.Radius, ASCIIColors.ExplosionOuter);

        _renderer?.Buffer.Invalidate();
    }

    // DestructionManager event handlers
    private void OnDestructionTileDamaged(Vector2I pos, int damage)
    {
        // Optional: could show damage numbers
        _renderer?.Buffer.Invalidate();
    }

    private void OnDestructionTileDestroyed(Vector2I pos)
    {
        UpdateFOV(); // Destroyed walls may change visibility
        _renderer?.Buffer.Invalidate();
    }

    private void OnDestructionExplosion(Vector2I pos, ExplosionData explosion)
    {
        // DestructionManager handles entity damage, we just need visuals
        _combatAnimator?.PlayExplosion(pos, explosion.Radius, ASCIIColors.ExplosionOuter);
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityBurned(Entity entity, int damage)
    {
        string name = entity.EntityName;
        _uiRenderer?.AddMessage($"{name} burns! [{damage} DMG]", ASCIIColors.FireFlame);
        _entityRenderer?.TriggerFlash(entity);
        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityKnocked(Entity entity, Vector2I knockback)
    {
        string name = entity.EntityName;
        int distance = Mathf.Abs(knockback.X) + Mathf.Abs(knockback.Y);
        _uiRenderer?.AddMessage($"{name} knocked back {distance} tiles!", ASCIIColors.AlertWarning);
        _renderer?.Buffer.Invalidate();
    }

    /// <summary>
    /// Trigger an explosion at a position. Used by weapons and abilities.
    /// </summary>
    public List<ExplosionResult>? TriggerExplosion(Vector2I position, ExplosionData explosion)
    {
        if (_gameMap == null || !UseDestructibleTerrain)
            return null;

        // Use DestructionManager if available (handles knockback and entity damage)
        if (_destructionManager != null)
        {
            var results = _destructionManager.TriggerExplosion(position.X, position.Y, explosion);
            UpdateFOV(); // Destroyed walls may change visibility
            return results;
        }

        // Fallback: use GameMap directly
        var gameMapResults = _gameMap.TriggerExplosion(position, explosion);

        // Check for entity damage from explosion
        foreach (var result in gameMapResults)
        {
            // Damage player if in blast
            if (_player != null && _player.GridPosition == result.Position && result.Damage > 0)
            {
                _player.TakeDamage(result.Damage);
            }

            // Damage enemies in blast
            var enemies = GetTree().GetNodesInGroup("Enemies");
            foreach (var node in enemies)
            {
                if (node is Enemy enemy && enemy.IsActive && enemy.GridPosition == result.Position && result.Damage > 0)
                {
                    enemy.HealthComponent?.TakeDamage(result.Damage);
                }
            }
        }

        UpdateFOV(); // Destroyed walls may change visibility
        return gameMapResults;
    }

    /// <summary>
    /// Damage a tile at position. Used by weapons.
    /// </summary>
    public bool DamageTile(Vector2I position, int damage, Destruction.DamageType damageType)
    {
        if (_gameMap == null || !UseDestructibleTerrain)
            return false;

        bool destroyed = _gameMap.DamageTile(position, damage, damageType);
        if (destroyed)
        {
            UpdateFOV(); // Destroyed walls may change visibility
        }
        return destroyed;
    }

    /// <summary>
    /// Ignite a tile. Used by fire weapons.
    /// </summary>
    public bool IgniteTile(Vector2I position, FireIntensity intensity = FireIntensity.Spark)
    {
        if (_gameMap == null || !UseDestructibleTerrain)
            return false;
        return _gameMap.IgniteTile(position, intensity);
    }

    /// <summary>
    /// Get GameMap for external access (pathfinding, weapons, etc.)
    /// </summary>
    public GameMap? GetGameMap() => _gameMap;

    private void SpawnEnemies()
    {
        if (_tileMapManager == null || _player == null || _entitiesNode == null)
            return;

        // Define archetype spawn weights (roughly: common, medium, rare)
        var archetypeWeights = new (EnemyArchetype archetype, int weight)[]
        {
            (EnemyArchetype.PatrolGuard, 30),   // Most common
            (EnemyArchetype.ScoutDrone, 25),
            (EnemyArchetype.SwarmBot, 20),
            (EnemyArchetype.Hunter, 12),
            (EnemyArchetype.HeavySentry, 8),
            (EnemyArchetype.Ambusher, 4),
            (EnemyArchetype.Bomber, 1)         // Rare
        };

        int totalWeight = 0;
        foreach (var (_, weight) in archetypeWeights)
            totalWeight += weight;

        for (int i = 0; i < EnemyCount; i++)
        {
            var spawnPos = _tileMapManager.FindRandomWalkablePosition();
            if (spawnPos == null)
                continue;

            var dist = Mathf.Abs(spawnPos.Value.X - _player.GridPosition.X) +
                       Mathf.Abs(spawnPos.Value.Y - _player.GridPosition.Y);
            if (dist < 5)
            {
                i--;
                continue;
            }

            // Pick a weighted random archetype
            var archetype = PickWeightedArchetype(archetypeWeights, totalWeight);

            // Create enemy using factory
            var enemy = EnemyFactory.CreateEnemy(archetype, spawnPos.Value);
            enemy.AddToGroup("Enemies");
            _entitiesNode.AddChild(enemy);

            // Register enemy with destruction manager for fire/knockback effects
            _destructionManager?.TrackEnemy(enemy);
        }

        GD.Print($"[ASCIIGameController] Spawned {EnemyCount} enemies with varied archetypes");
    }

    /// <summary>
    /// Pick a random archetype based on weights.
    /// </summary>
    private static EnemyArchetype PickWeightedArchetype(
        (EnemyArchetype archetype, int weight)[] weights, int totalWeight)
    {
        int roll = GD.RandRange(1, totalWeight);
        int cumulative = 0;

        foreach (var (archetype, weight) in weights)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return archetype;
        }

        return EnemyArchetype.PatrolGuard; // Fallback
    }

    private void GiveStarterItems()
    {
        if (_player?.InventoryComponent == null || _player.EquipmentComponent == null)
            return;

        // Weapons go in Utility slots
        var meleeWeapon = ItemFactory.CreateStarterWeapon();      // Plasma Cutter
        var rangedWeapon = ItemFactory.CreateStarterRangedWeapon(); // Nail Gun

        // Armor goes in Base slot
        var plating = ItemFactory.CreateStarterArmor();

        // Equip weapons in utility slots (hotkeys 1 and 2)
        _player.EquipmentComponent.Equip(meleeWeapon, EquipmentSlotType.Utility, 0);
        _player.EquipmentComponent.Equip(rangedWeapon, EquipmentSlotType.Utility, 1);
        _player.EquipmentComponent.Equip(plating, EquipmentSlotType.Base, 0);

        // Add some random items to inventory
        for (int i = 0; i < 3; i++)
        {
            var randomItem = ItemFactory.CreateRandomItem();
            _player.InventoryComponent.AddItem(randomItem);
        }

        GD.Print($"[ASCIIGameController] Player equipped: {meleeWeapon.Name}, {rangedWeapon.Name}, {plating.Name}");
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
            _destructionManager.OnTileDamaged -= OnDestructionTileDamaged;
            _destructionManager.OnTileDestroyed -= OnDestructionTileDestroyed;
            _destructionManager.OnExplosion -= OnDestructionExplosion;
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

        // Update targeting animations
        _targetingManager?.Update(dt);

        // Update particle system
        _particleSystem?.Update(dt);
        _combatAnimator?.Update(dt);

        // Update GameMap animations (fire, dancing colors, explosions)
        if (_gameMap != null)
        {
            _gameMap.Update(dt);
        }

        // Update DestructionManager animations (smoke, collapse visuals)
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

        // Handle help screen
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Slash && keyEvent.ShiftPressed)
        {
            ShowHelp();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Handle weapon hotkeys (number keys 1-9) when player can act
        if (GameState.Instance.CurrentState == GameState.State.Playing && _player?.CanAct == true)
        {
            if (_targetingManager?.HandleWeaponHotkey(@event) == true)
            {
                GetViewport().SetInputAsHandled();
                _renderer?.Buffer.Invalidate();
                return;
            }
        }

        // Debug: Test explosions (can be removed for release)
        if (@event is InputEventKey debugKey && debugKey.Pressed)
        {
            switch (debugKey.Keycode)
            {
                case Key.F1: // Small blast at player
                    if (_player != null)
                    {
                        TriggerExplosion(_player.GridPosition + new Vector2I(2, 0), ExplosionData.SmallBlast);
                        GetViewport().SetInputAsHandled();
                    }
                    return;
                case Key.F2: // Grenade at player
                    if (_player != null)
                    {
                        TriggerExplosion(_player.GridPosition + new Vector2I(3, 0), ExplosionData.Grenade);
                        GetViewport().SetInputAsHandled();
                    }
                    return;
                case Key.F3: // Incendiary at player
                    if (_player != null)
                    {
                        TriggerExplosion(_player.GridPosition + new Vector2I(2, 2), ExplosionData.Incendiary);
                        GetViewport().SetInputAsHandled();
                    }
                    return;
                case Key.F4: // Ignite nearby tile
                    if (_player != null)
                    {
                        IgniteTile(_player.GridPosition + new Vector2I(1, 0), FireIntensity.Flame);
                        _uiRenderer?.AddMessage("DEBUG: Ignited nearby tile", ASCIIColors.TextSecondary);
                        GetViewport().SetInputAsHandled();
                    }
                    return;
                case Key.F5: // Debug dump
                    _debugger?.ForceDump();
                    _uiRenderer?.AddMessage("DEBUG: State dumped to debug_output/", ASCIIColors.TextSecondary);
                    GetViewport().SetInputAsHandled();
                    return;

                // Map zoom controls
                case Key.Equal: // + key (zoom in)
                case Key.Plus:
                case Key.KpAdd:
                    _renderer?.ZoomMapIn();
                    _uiRenderer?.AddMessage($"Map zoom: {_renderer?.GetMapZoom():F0}x", ASCIIColors.TextSecondary);
                    GetViewport().SetInputAsHandled();
                    _renderer?.Buffer.Invalidate();
                    return;

                case Key.Minus: // - key (zoom out)
                case Key.KpSubtract:
                    _renderer?.ZoomMapOut();
                    _uiRenderer?.AddMessage($"Map zoom: {_renderer?.GetMapZoom():F0}x", ASCIIColors.TextSecondary);
                    GetViewport().SetInputAsHandled();
                    _renderer?.Buffer.Invalidate();
                    return;

                case Key.Key0: // Reset zoom (when not targeting/playing)
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

        if (GameState.Instance.CurrentState != GameState.State.Playing)
            return;

        // Movement handled by Player class via input actions (but blocked during targeting)
        if (_targetingManager?.IsTargeting == true)
        {
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    private void ShowHelp()
    {
        _uiRenderer?.AddMessage("─── CONTROLS ───", ASCIIColors.PrimaryBright);
        _uiRenderer?.AddMessage("WASD: Move | QEZC: Diagonals | .: Wait", ASCIIColors.TextSecondary);
        _uiRenderer?.AddMessage("[1-9]: Select weapon | [Tab]: Cycle targets | [Enter]: Fire | [Esc]: Cancel", ASCIIColors.TextSecondary);
        _uiRenderer?.AddMessage("[I]: Inventory | [?]: Help | [+/-]: Map zoom | [0]: Reset zoom", ASCIIColors.TextSecondary);
        _renderer?.Buffer.Invalidate();
    }

    private void DrawFrame()
    {
        if (_renderer == null || _tileMapManager == null || _player == null)
            return;

        var buffer = _renderer.Buffer;
        buffer.Clear();

        // Update camera to follow player
        _mapRenderer?.CenterOn(_player.GridPosition);

        // Render layers in order
        if (_gameMap != null && UseDestructibleTerrain)
        {
            // Use new GameMap with destructible terrain
            _mapRenderer?.Render(_gameMap);
            _mapRenderer?.RenderExplosions(_gameMap);

            // Render destruction effects (fire, smoke, explosions, collapses)
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
        }
        else
        {
            // Fallback to simple TileMapManager
            _mapRenderer?.Render(_tileMapManager);
        }

        RenderEntities();

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

        // Show fire count in debug (could add to sidebar later)
        if (_gameMap != null && _gameMap.ActiveFireCount > 0)
        {
            // Fire status is shown via messages when fires start
        }
    }

    private void RenderEntities()
    {
        if (_entityRenderer == null || _tileMapManager == null || _player == null)
            return;

        // Get the correct FOV based on whether destructible terrain is enabled
        var fov = (_gameMap != null && UseDestructibleTerrain) ? _gameMap.FOV : _tileMapManager.FOV;

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

        // Update nearby list for sidebar
        UpdateNearbyList(enemyList);
    }

    private void UpdateNearbyList(List<Enemy> enemies)
    {
        if (_uiRenderer == null || _tileMapManager == null || _player == null)
            return;

        var nearby = new List<(char symbol, string name, Color color)>();

        // Use GameMap's FOV if available
        var fov = (_gameMap != null && UseDestructibleTerrain) ? _gameMap.FOV : _tileMapManager.FOV;

        // Add visible enemies
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

        // TODO: Add visible items

        // Add stairs if visible (would need to track stairs position)

        _uiRenderer.UpdateNearby(nearby);
    }

    private void UpdateFOV()
    {
        if (_tileMapManager == null || _player == null)
            return;

        // Update FOV using GameMap if available (uses destructible terrain for blocking)
        if (_gameMap != null && UseDestructibleTerrain)
        {
            _gameMap.UpdateFOV(_player.GridPosition, _player.SightRange);
        }
        else
        {
            _tileMapManager.UpdateFOV(_player.GridPosition, _player.SightRange);
        }
        _renderer?.Buffer.Invalidate();
    }

    // Event handlers
    private void OnTurnStarted(int turnNumber)
    {
        // Process destruction systems (fire, smoke, explosions, collapses)
        if (_gameMap != null && UseDestructibleTerrain)
        {
            // Process fire simulation through GameMap
            if (FireSimulationEnabled)
            {
                _gameMap.ProcessFireTurn();
            }

            // Process DestructionManager (handles smoke, collapses, and entity damage from fire)
            if (_destructionManager != null)
            {
                _destructionManager.ProcessTurn();
            }
            else
            {
                // Fallback: handle fire damage directly if no DestructionManager
                ApplyFireDamageToEntities();
            }
        }

        _renderer?.Buffer.Invalidate();
    }

    /// <summary>
    /// Fallback fire damage application when DestructionManager is not available.
    /// </summary>
    private void ApplyFireDamageToEntities()
    {
        if (_gameMap == null || _player == null)
            return;

        // Check for fire damage to player
        if (_gameMap.HasFireAt(_player.GridPosition))
        {
            int fireDamage = _gameMap.GetFireDamageAt(_player.GridPosition);
            if (fireDamage > 0)
            {
                _player.TakeDamage(fireDamage);
                _uiRenderer?.AddMessage($"You burn in the flames! [{fireDamage} DMG]", ASCIIColors.FireFlame);
            }
        }

        // Check for fire damage to enemies
        var enemies = GetTree().GetNodesInGroup("Enemies");
        foreach (var node in enemies)
        {
            if (node is Enemy enemy && enemy.IsActive && _gameMap.HasFireAt(enemy.GridPosition))
            {
                int fireDamage = _gameMap.GetFireDamageAt(enemy.GridPosition);
                if (fireDamage > 0 && enemy.HealthComponent != null)
                {
                    enemy.HealthComponent.TakeDamage(fireDamage);
                }
            }
        }
    }

    private void OnEntityDamaged(Node entity, int damage, int remainingHealth)
    {
        _entityRenderer?.TriggerFlash(entity);

        if (entity == _player)
        {
            _uiRenderer?.AddMessage($"You take {damage} damage! ({remainingHealth} HP remaining)", ASCIIColors.AlertDanger);
        }

        _renderer?.Buffer.Invalidate();
    }

    private void OnEntityMoved(Node entity, Vector2I from, Vector2I to)
    {
        if (entity == _player)
        {
            UpdateFOV();
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

    // Targeting event handlers
    private void OnTargetingStarted()
    {
        // Player input is blocked during targeting (handled in _UnhandledInput)
        _renderer?.Buffer.Invalidate();
    }

    private void OnTargetingEnded()
    {
        _renderer?.Buffer.Invalidate();
    }

    private void OnRangedAttackPerformed(CombatResult result)
    {
        // Complete the player's action with the attack cost
        if (_player != null)
        {
            _player.CompleteAction(result.ActionCost);
        }

        // Play combat animation (projectile + impact effects)
        _combatAnimator?.PlayAttackAnimation(result);

        // Log detailed results
        foreach (var attackResult in result.Results)
        {
            _uiRenderer?.AddMessage(attackResult.Message,
                attackResult.Hit ? ASCIIColors.AlertSuccess : ASCIIColors.AlertWarning);
        }

        // Trigger entity flash for hit targets
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
