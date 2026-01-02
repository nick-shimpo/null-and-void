using System;
using System.Collections.Generic;
using Godot;
using NullAndVoid.Effects;
using NullAndVoid.Entities;
using NullAndVoid.Rendering;

namespace NullAndVoid.Destruction;

/// <summary>
/// Central manager for all destruction, fire, smoke, and collapse systems.
/// Coordinates between subsystems and handles entity interactions.
/// </summary>
public class DestructionManager
{
    // Tile data
    private readonly DestructibleTile[,] _tiles;
    private readonly int _width;
    private readonly int _height;

    // Subsystems
    public FireSimulation FireSim { get; }
    public SmokeSimulation SmokeSim { get; }
    public ExplosionSystem ExplosionSys { get; }
    public StructuralCollapseSystem CollapseSys { get; }
    public DestructionRenderer Renderer { get; }

    // Random for various effects
    private readonly Random _random = new();

    // Entity tracking for fire/knockback (Players and Enemies)
    private readonly List<Player> _trackedPlayers = new();
    private readonly List<Enemy> _trackedEnemies = new();

    // Events
    public event Action<Vector2I, int>? OnTileDamaged;
    public event Action<Vector2I>? OnTileDestroyed;
    public event Action<Vector2I, ExplosionData>? OnExplosion;
    public event Action<Entity, int>? OnEntityBurned;
    public event Action<Entity, Vector2I>? OnEntityKnocked;

    public DestructionManager(DestructibleTile[,] tiles, int width, int height, ASCIIBuffer buffer)
    {
        _tiles = tiles;
        _width = width;
        _height = height;

        FireSim = new FireSimulation(tiles, width, height);
        SmokeSim = new SmokeSimulation(width, height);
        ExplosionSys = new ExplosionSystem(tiles, width, height, FireSim);
        CollapseSys = new StructuralCollapseSystem(tiles, width, height);
        Renderer = new DestructionRenderer(buffer);
    }

    /// <summary>
    /// Register a player to track for fire/knockback effects.
    /// </summary>
    public void TrackPlayer(Player player)
    {
        if (!_trackedPlayers.Contains(player))
            _trackedPlayers.Add(player);
    }

    /// <summary>
    /// Register an enemy to track for fire/knockback effects.
    /// </summary>
    public void TrackEnemy(Enemy enemy)
    {
        if (!_trackedEnemies.Contains(enemy))
            _trackedEnemies.Add(enemy);
    }

    /// <summary>
    /// Unregister a player.
    /// </summary>
    public void UntrackPlayer(Player player)
    {
        _trackedPlayers.Remove(player);
    }

    /// <summary>
    /// Unregister an enemy.
    /// </summary>
    public void UntrackEnemy(Enemy enemy)
    {
        _trackedEnemies.Remove(enemy);
    }

    /// <summary>
    /// Process one game turn of destruction simulation.
    /// </summary>
    public void ProcessTurn()
    {
        // Process fire spread
        FireSim.ProcessTurn();

        // Generate smoke from fires
        SmokeSim.GenerateFromFires(
            FireSim.GetActiveFirePositions(),
            pos => _tiles[pos.X, pos.Y].Fire.Intensity
        );

        // Process smoke dispersal
        SmokeSim.ProcessTurn();

        // Process chain explosions
        while (ExplosionSys.ProcessChainReactions())
        {
            // Continue until all chain reactions complete
        }

        // Apply fire damage to entities on burning tiles
        ApplyFireDamageToEntities();
    }

    /// <summary>
    /// Update animations (call each frame).
    /// </summary>
    public void Update(float delta)
    {
        // Update fire animations in all burning tiles
        foreach (var pos in FireSim.GetActiveFirePositions())
        {
            _tiles[pos.X, pos.Y].Fire.UpdateAnimation(delta);
        }

        // Update smoke animations
        SmokeSim.UpdateAnimations(delta);

        // Update explosion visuals
        ExplosionSys.UpdateVisuals(delta);

        // Update collapse visuals
        CollapseSys.UpdateVisuals(delta);
    }

    /// <summary>
    /// Render all destruction effects.
    /// </summary>
    public void Render(int mapStartX, int mapStartY, int cameraX, int cameraY, int viewWidth, int viewHeight)
    {
        // Render fire on tiles
        Renderer.RenderFire(FireSim, mapStartX, mapStartY, cameraX, cameraY, viewWidth, viewHeight, _tiles);

        // Render smoke
        Renderer.RenderSmoke(SmokeSim, mapStartX, mapStartY, cameraX, cameraY, viewWidth, viewHeight);

        // Render explosion effects
        Renderer.RenderExplosions(ExplosionSys.ActiveVisuals, mapStartX, mapStartY, cameraX, cameraY);

        // Render collapse effects
        Renderer.RenderCollapses(CollapseSys.ActiveVisuals, mapStartX, mapStartY, cameraX, cameraY);
    }

    /// <summary>
    /// Damage a tile at the specified position.
    /// </summary>
    public bool DamageTile(int x, int y, int damage, DamageType damageType)
    {
        if (!IsInBounds(x, y))
            return false;

        ref var tile = ref _tiles[x, y];
        bool wasDestroyed = tile.TakeDamage(damage, damageType);

        OnTileDamaged?.Invoke(new Vector2I(x, y), damage);

        if (wasDestroyed)
        {
            // Check for structural collapse
            var collapseResults = CollapseSys.OnTileDestroyed(x, y);

            OnTileDestroyed?.Invoke(new Vector2I(x, y));

            // Apply collapse damage to entities
            foreach (var result in collapseResults)
            {
                ApplyCollapseDamage(result);
            }
        }

        return wasDestroyed;
    }

    /// <summary>
    /// Trigger an explosion at the specified position.
    /// Returns damage results for all affected tiles and entities.
    /// </summary>
    public List<ExplosionResult> TriggerExplosion(int x, int y, ExplosionData explosion)
    {
        var results = ExplosionSys.Explode(x, y, explosion);

        OnExplosion?.Invoke(new Vector2I(x, y), explosion);

        // Apply knockback to entities
        ApplyExplosionKnockback(x, y, explosion);

        // Apply explosion damage to entities
        ApplyExplosionDamageToEntities(x, y, explosion);

        // Check for structural collapses
        foreach (var result in results)
        {
            if (result.WasDestroyed)
            {
                var collapseResults = CollapseSys.OnTileDestroyed(result.Position.X, result.Position.Y);
                foreach (var collapse in collapseResults)
                {
                    ApplyCollapseDamage(collapse);
                }
            }
        }

        // Clear smoke in explosion radius
        SmokeSim.ClearRadius(x, y, explosion.Radius);

        return results;
    }

    /// <summary>
    /// Ignite a tile at the specified position.
    /// </summary>
    public bool IgniteTile(int x, int y, FireIntensity intensity = FireIntensity.Spark)
    {
        return FireSim.Ignite(x, y, intensity);
    }

    /// <summary>
    /// Extinguish fire at a position.
    /// </summary>
    public void ExtinguishFire(int x, int y)
    {
        FireSim.Extinguish(x, y);
    }

    /// <summary>
    /// Extinguish fires in a radius (water bomb, etc.).
    /// </summary>
    public void ExtinguishRadius(int x, int y, int radius)
    {
        FireSim.ExtinguishRadius(x, y, radius);
    }

    /// <summary>
    /// Apply fire damage to entities standing on burning tiles.
    /// </summary>
    private void ApplyFireDamageToEntities()
    {
        // Apply to players
        foreach (var player in _trackedPlayers)
        {
            if (player.CurrentHealth <= 0)
                continue;

            var pos = player.GridPosition;
            if (!IsInBounds(pos.X, pos.Y))
                continue;

            int fireDamage = FireSim.GetFireDamage(pos.X, pos.Y);
            if (fireDamage > 0)
            {
                player.TakeDamage(fireDamage);
                OnEntityBurned?.Invoke(player, fireDamage);
                GD.Print($"{player.EntityName} takes {fireDamage} fire damage from burning tile!");
            }
        }

        // Apply to enemies
        foreach (var enemy in _trackedEnemies)
        {
            if (!enemy.IsActive)
                continue;

            var pos = enemy.GridPosition;
            if (!IsInBounds(pos.X, pos.Y))
                continue;

            int fireDamage = FireSim.GetFireDamage(pos.X, pos.Y);
            if (fireDamage > 0)
            {
                enemy.TakeDamage(fireDamage);
                OnEntityBurned?.Invoke(enemy, fireDamage);
                GD.Print($"{enemy.EntityName} takes {fireDamage} fire damage from burning tile!");
            }
        }
    }

    /// <summary>
    /// Apply explosion knockback to entities.
    /// </summary>
    private void ApplyExplosionKnockback(int centerX, int centerY, ExplosionData explosion)
    {
        if (explosion.KnockbackForce <= 0)
            return;

        // Apply to players
        foreach (var player in _trackedPlayers)
        {
            if (player.CurrentHealth <= 0)
                continue;
            ApplyKnockbackToEntity(player, centerX, centerY, explosion, player.EntityName,
                (dmg) => player.TakeDamage(dmg));
        }

        // Apply to enemies
        foreach (var enemy in _trackedEnemies)
        {
            if (!enemy.IsActive)
                continue;
            ApplyKnockbackToEntity(enemy, centerX, centerY, explosion, enemy.EntityName,
                (dmg) => enemy.TakeDamage(dmg));
        }
    }

    /// <summary>
    /// Helper to apply knockback to an entity.
    /// </summary>
    private void ApplyKnockbackToEntity(Entity entity, int centerX, int centerY, ExplosionData explosion, string entityName, Action<int> takeDamage)
    {
        var pos = entity.GridPosition;
        float distance = pos.DistanceTo(new Vector2I(centerX, centerY));

        if (distance > explosion.Radius)
            return;

        int knockback = explosion.CalculateKnockbackAtDistance(distance);
        if (knockback <= 0)
            return;

        // Calculate knockback direction (away from explosion center)
        Vector2I direction = new Vector2I(
            pos.X - centerX,
            pos.Y - centerY
        );

        // Normalize to unit direction
        if (direction.X != 0 || direction.Y != 0)
        {
            int absX = Mathf.Abs(direction.X);
            int absY = Mathf.Abs(direction.Y);

            if (absX >= absY)
                direction = new Vector2I(Mathf.Sign(direction.X), 0);
            else
                direction = new Vector2I(0, Mathf.Sign(direction.Y));
        }
        else
        {
            // Entity is at center, random direction
            direction = new Vector2I(_random.Next(-1, 2), _random.Next(-1, 2));
        }

        // Try to knock back the entity
        for (int i = 0; i < knockback; i++)
        {
            Vector2I newPos = entity.GridPosition + direction;

            // Check if new position is valid (not blocked)
            if (IsInBounds(newPos.X, newPos.Y) &&
                !_tiles[newPos.X, newPos.Y].BlocksMovement)
            {
                entity.GridPosition = newPos;
            }
            else
            {
                // Hit a wall - take impact damage
                int impactDamage = (knockback - i) * 2;
                takeDamage(impactDamage);
                GD.Print($"{entityName} slammed into a wall for {impactDamage} damage!");
                break;
            }
        }

        OnEntityKnocked?.Invoke(entity, direction * knockback);
        GD.Print($"{entityName} was knocked back {knockback} tiles!");
    }

    /// <summary>
    /// Apply explosion damage to entities in radius.
    /// </summary>
    private void ApplyExplosionDamageToEntities(int centerX, int centerY, ExplosionData explosion)
    {
        // Apply to players
        foreach (var player in _trackedPlayers)
        {
            if (player.CurrentHealth <= 0)
                continue;

            var pos = player.GridPosition;
            float distance = pos.DistanceTo(new Vector2I(centerX, centerY));

            if (distance > explosion.Radius)
                continue;

            int damage = explosion.CalculateDamageAtDistance(distance);
            if (damage > 0)
            {
                player.TakeDamage(damage);
                GD.Print($"{player.EntityName} takes {damage} explosion damage!");
            }
        }

        // Apply to enemies
        foreach (var enemy in _trackedEnemies)
        {
            if (!enemy.IsActive)
                continue;

            var pos = enemy.GridPosition;
            float distance = pos.DistanceTo(new Vector2I(centerX, centerY));

            if (distance > explosion.Radius)
                continue;

            int damage = explosion.CalculateDamageAtDistance(distance);
            if (damage > 0)
            {
                enemy.TakeDamage(damage);
                GD.Print($"{enemy.EntityName} takes {damage} explosion damage!");
            }
        }
    }

    /// <summary>
    /// Apply collapse damage to entities.
    /// </summary>
    private void ApplyCollapseDamage(CollapseResult collapse)
    {
        // Apply to players
        foreach (var player in _trackedPlayers)
        {
            if (player.CurrentHealth <= 0)
                continue;

            if (player.GridPosition == collapse.Position)
            {
                player.TakeDamage(collapse.DebrisDamage);
                GD.Print($"{player.EntityName} is crushed by falling {collapse.MaterialName} for {collapse.DebrisDamage} damage!");
            }
        }

        // Apply to enemies
        foreach (var enemy in _trackedEnemies)
        {
            if (!enemy.IsActive)
                continue;

            if (enemy.GridPosition == collapse.Position)
            {
                enemy.TakeDamage(collapse.DebrisDamage);
                GD.Print($"{enemy.EntityName} is crushed by falling {collapse.MaterialName} for {collapse.DebrisDamage} damage!");
            }
        }
    }

    /// <summary>
    /// Get the destructible tile at a position.
    /// </summary>
    public ref DestructibleTile GetTile(int x, int y)
    {
        return ref _tiles[x, y];
    }

    /// <summary>
    /// Check if a position has active fire.
    /// </summary>
    public bool HasFire(int x, int y)
    {
        if (!IsInBounds(x, y))
            return false;
        return _tiles[x, y].Fire.IsActive;
    }

    /// <summary>
    /// Check if a position has smoke.
    /// </summary>
    public bool HasSmoke(int x, int y)
    {
        return SmokeSim.GetSmoke(x, y).IsActive;
    }

    /// <summary>
    /// Get visibility reduction from smoke at a position.
    /// </summary>
    public int GetVisibilityReduction(int x, int y)
    {
        return SmokeSim.GetVisibilityReduction(x, y);
    }

    /// <summary>
    /// Check if fire blocks AI pathfinding at this position.
    /// </summary>
    public bool IsFireBlocking(int x, int y)
    {
        return FireSim.IsFireBlocking(x, y);
    }

    /// <summary>
    /// Set wind for fire and smoke spread.
    /// </summary>
    public void SetWind(Vector2 direction, float strength)
    {
        FireSim.WindDirection = direction;
        FireSim.WindStrength = strength;
        SmokeSim.WindDirection = direction;
        SmokeSim.WindStrength = strength;
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }
}
