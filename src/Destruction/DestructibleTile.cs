using System;
using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Destruction;

/// <summary>
/// A terrain tile that can be damaged, destroyed, and set on fire.
/// Extends the basic TerrainTile with destruction and fire simulation.
/// </summary>
public struct DestructibleTile
{
    // Base terrain data
    public char Character;
    public char DamagedChar;
    public char DestroyedChar;
    public char DebrisChar;

    public Color BaseColor;
    public Color DamagedColor;
    public Color DestroyedColor;

    public DancingColor Foreground;
    public DancingColor Background;

    public bool BlocksMovement;
    public bool BlocksSight;

    // Material and destruction
    public MaterialProperties Material;
    public int CurrentHP;
    public DestructionState State;

    // Fire state
    public FireState Fire;

    // Animation
    public float AnimationTimer;
    public bool UseAltChar;

    private static readonly Random _random = new();

    /// <summary>
    /// Create a destructible tile with specified material.
    /// </summary>
    public static DestructibleTile Create(
        char character,
        Color color,
        MaterialProperties material,
        bool blocksMovement = false,
        bool blocksSight = false,
        char? damagedChar = null,
        char? destroyedChar = null,
        char? debrisChar = null)
    {
        return new DestructibleTile
        {
            Character = character,
            DamagedChar = damagedChar ?? character,
            DestroyedChar = destroyedChar ?? ASCIIChars.Rubble,
            DebrisChar = debrisChar ?? ASCIIChars.Gravel,

            BaseColor = color,
            DamagedColor = ASCIIColors.Dimmed(color, 0.7f),
            DestroyedColor = ASCIIColors.Dimmed(color, 0.5f),

            Foreground = DancingColor.Static(color),
            Background = DancingColor.Static(ASCIIColors.BgDark),

            BlocksMovement = blocksMovement,
            BlocksSight = blocksSight,

            Material = material,
            CurrentHP = material.MaxHitPoints,
            State = DestructionState.Intact,

            Fire = FireState.None,

            AnimationTimer = 0,
            UseAltChar = false
        };
    }

    /// <summary>
    /// Apply damage to the tile. Returns true if destroyed.
    /// </summary>
    public bool TakeDamage(int amount, DamageType damageType)
    {
        if (State == DestructionState.Destroyed)
            return false;

        int actualDamage = Material.CalculateDamage(amount, damageType);
        CurrentHP -= actualDamage;

        // Update destruction state
        float hpPercent = (float)CurrentHP / Material.MaxHitPoints;
        State = DestructionStateExtensions.FromHPPercent(hpPercent);

        // Update visual based on damage
        UpdateVisualForDamage();

        // Check for fire ignition from fire damage
        if (damageType == DamageType.Fire && Material.Flammability > 0)
        {
            TryIgnite(0.3f); // 30% base chance from fire damage
        }

        // Check for fire ignition from explosive damage
        if (damageType == DamageType.Explosive && Material.Flammability > 0)
        {
            TryIgnite(0.5f); // 50% base chance from explosions
        }

        return State == DestructionState.Destroyed;
    }

    /// <summary>
    /// Try to ignite the tile.
    /// </summary>
    public bool TryIgnite(float bonusChance = 0f)
    {
        if (Fire.IsActive || Material.Flammability <= 0)
            return false;

        float igniteChance = Material.Flammability + bonusChance;
        if (_random.NextDouble() < igniteChance)
        {
            Fire = FireState.Create(FireIntensity.Spark);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Set fire to a specific intensity.
    /// </summary>
    public void SetFire(FireIntensity intensity)
    {
        if (Material.Flammability > 0 || intensity == FireIntensity.Ash)
        {
            Fire = FireState.Create(intensity);
        }
    }

    /// <summary>
    /// Extinguish fire on this tile.
    /// </summary>
    public void Extinguish()
    {
        Fire = FireState.None;
    }

    /// <summary>
    /// Update animation timer.
    /// </summary>
    public void Update(float delta)
    {
        Foreground.Update(delta);
        Background.Update(delta);
        Fire.UpdateAnimation(delta);

        // Base tile animation
        AnimationTimer += delta;
        if (AnimationTimer >= 0.5f)
        {
            AnimationTimer = 0;
            UseAltChar = !UseAltChar;
        }
    }

    /// <summary>
    /// Advance fire state by one turn. Returns true if fire spread should be checked.
    /// </summary>
    public bool AdvanceFireTurn()
    {
        if (!Fire.IsActive)
            return false;

        bool stateChanged = Fire.AdvanceTurn();

        // Apply burn damage to tile
        if (Fire.IsBurning && Material.Flammability > 0)
        {
            int burnDamage = Fire.Data.DamagePerTurn;
            TakeDamage(burnDamage, DamageType.Fire);
        }

        return Fire.IsActive;
    }

    /// <summary>
    /// Get the current display character.
    /// </summary>
    public readonly char GetCurrentChar()
    {
        // Fire takes visual priority
        if (Fire.IsActive)
            return Fire.GetCharacter();

        // Otherwise based on destruction state
        return State switch
        {
            DestructionState.Intact => Character,
            DestructionState.Damaged => DamagedChar,
            DestructionState.HeavilyDamaged => DestroyedChar,
            DestructionState.Destroyed => DebrisChar,
            _ => Character
        };
    }

    /// <summary>
    /// Get the current foreground color.
    /// </summary>
    public readonly Color GetCurrentForeground()
    {
        // Fire takes visual priority
        if (Fire.IsActive)
            return Fire.GetColor();

        // Apply damage darkening
        float mult = State.GetColorMultiplier();
        return new Color(
            Foreground.CurrentColor.R * mult,
            Foreground.CurrentColor.G * mult,
            Foreground.CurrentColor.B * mult,
            Foreground.CurrentColor.A
        );
    }

    /// <summary>
    /// Get the current background color.
    /// </summary>
    public readonly Color GetCurrentBackground()
    {
        // Fire glow
        if (Fire.IsActive)
            return Fire.GetBackgroundColor();

        return Background.CurrentColor;
    }

    /// <summary>
    /// Update visual properties based on damage state.
    /// </summary>
    private void UpdateVisualForDamage()
    {
        // Could add particle effects, shake, etc. here
        if (State == DestructionState.Destroyed)
        {
            BlocksMovement = false;
            BlocksSight = false;
        }
    }

    // Factory methods for common destructible tiles

    public static DestructibleTile WoodWall()
    {
        return Create(
            ASCIIChars.Wall,
            ASCIIColors.Door,
            MaterialProperties.Wood,
            blocksMovement: true,
            blocksSight: true,
            damagedChar: '╫',
            destroyedChar: '╪',
            debrisChar: ASCIIChars.Rubble
        );
    }

    public static DestructibleTile StoneWall()
    {
        return Create(
            ASCIIChars.Wall,
            ASCIIColors.Wall,
            MaterialProperties.Stone,
            blocksMovement: true,
            blocksSight: true,
            damagedChar: '▓',
            destroyedChar: '▒',
            debrisChar: ASCIIChars.Rubble
        );
    }

    public static DestructibleTile MetalWall()
    {
        return Create(
            ASCIIChars.ShadeFull,
            ASCIIColors.TechWall,
            MaterialProperties.Metal,
            blocksMovement: true,
            blocksSight: true,
            damagedChar: '▓',
            destroyedChar: '▒',
            debrisChar: '*'
        );
    }

    public static DestructibleTile Tree(bool evergreen = false)
    {
        char treeChar = evergreen ? ASCIIChars.TreeEvergreen : ASCIIChars.TreeDeciduous;
        Color treeColor = evergreen ? ASCIIColors.TreePine : ASCIIColors.TreeCanopy;

        var tile = Create(
            treeChar,
            treeColor,
            MaterialProperties.Vegetation,
            blocksMovement: true,
            blocksSight: true,
            damagedChar: ASCIIChars.TreeDead,
            destroyedChar: '|',
            debrisChar: ASCIIChars.Floor
        );
        tile.Foreground = DancingColor.ForestCanopy(treeColor);
        return tile;
    }

    public static DestructibleTile GrassTile()
    {
        var tile = Create(
            ASCIIChars.Grass,
            ASCIIColors.Grass,
            MaterialProperties.Grass,
            blocksMovement: false,
            blocksSight: false,
            damagedChar: ASCIIChars.Grass,
            destroyedChar: ':',
            debrisChar: '.'
        );
        tile.Foreground = DancingColor.GrassWave(ASCIIColors.Grass);
        return tile;
    }

    public static DestructibleTile TechTerminal()
    {
        var tile = Create(
            ASCIIChars.Terminal,
            ASCIIColors.TechTerminal,
            MaterialProperties.Tech,
            blocksMovement: false,
            blocksSight: false,
            damagedChar: '¤',
            destroyedChar: '*',
            debrisChar: '·'
        );
        tile.Foreground = DancingColor.TechFlicker(ASCIIColors.TechTerminal);
        return tile;
    }

    public static DestructibleTile FuelTank()
    {
        return Create(
            'O',
            Color.Color8(200, 150, 50),
            new MaterialProperties
            {
                Name = "Fuel",
                Hardness = 30,
                MaxHitPoints = 15,
                Flammability = 1.0f,
                BurnDuration = 3,
                FireResistance = 0.0f,
                ConductsFire = true,
                DebrisType = "None",
                BurnedType = "Ash"
            },
            blocksMovement: true,
            blocksSight: false,
            damagedChar: 'O',
            destroyedChar: '○',
            debrisChar: '·'
        );
    }
}
