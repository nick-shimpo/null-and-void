using Godot;
using NullAndVoid.Rendering;

namespace NullAndVoid.Destruction;

/// <summary>
/// Represents the state of fire on a tile.
/// Based on Brogue and CDDA fire mechanics research.
/// Fire evolves: Spark → Smolder → Flame → Blaze → Inferno → Dying → Ash
/// </summary>
public enum FireIntensity
{
    None = 0,
    Spark = 1,
    Smolder = 2,
    Flame = 3,
    Blaze = 4,
    Inferno = 5,
    Dying = 6,
    Ash = 7
}

/// <summary>
/// Complete fire state for a tile including animation and spread data.
/// </summary>
public struct FireState
{
    public FireIntensity Intensity;
    public int RemainingDuration;    // Turns until next state transition
    public float AnimationTimer;     // For character/color cycling
    public bool UseAltChar;          // Toggle for character animation

    // Fire behavior parameters by intensity
    public static readonly FireStateData[] StateData = new[]
    {
        // None
        new FireStateData { DamagePerTurn = 0, SpreadChance = 0f, LightRadius = 0, Duration = 0 },
        // Spark
        new FireStateData { DamagePerTurn = 0, SpreadChance = 0.1f, LightRadius = 1, Duration = 2 },
        // Smolder
        new FireStateData { DamagePerTurn = 1, SpreadChance = 0.2f, LightRadius = 2, Duration = 4 },
        // Flame
        new FireStateData { DamagePerTurn = 2, SpreadChance = 0.4f, LightRadius = 3, Duration = 6 },
        // Blaze
        new FireStateData { DamagePerTurn = 4, SpreadChance = 0.6f, LightRadius = 4, Duration = 8 },
        // Inferno
        new FireStateData { DamagePerTurn = 8, SpreadChance = 0.8f, LightRadius = 5, Duration = 10 },
        // Dying
        new FireStateData { DamagePerTurn = 1, SpreadChance = 0.1f, LightRadius = 2, Duration = 3 },
        // Ash
        new FireStateData { DamagePerTurn = 0, SpreadChance = 0f, LightRadius = 0, Duration = -1 } // Permanent
    };

    public readonly FireStateData Data => StateData[(int)Intensity];
    public readonly bool IsActive => Intensity > FireIntensity.None && Intensity < FireIntensity.Ash;
    public readonly bool IsBurning => Intensity >= FireIntensity.Smolder && Intensity <= FireIntensity.Inferno;

    /// <summary>
    /// Create a new fire at the specified intensity.
    /// </summary>
    public static FireState Create(FireIntensity intensity)
    {
        var data = StateData[(int)intensity];
        return new FireState
        {
            Intensity = intensity,
            RemainingDuration = data.Duration,
            AnimationTimer = 0,
            UseAltChar = false
        };
    }

    /// <summary>
    /// Create no fire state.
    /// </summary>
    public static FireState None => new() { Intensity = FireIntensity.None };

    /// <summary>
    /// Update animation timer and return true if character should swap.
    /// </summary>
    public bool UpdateAnimation(float delta)
    {
        if (!IsActive)
            return false;

        // Faster animation for more intense fires
        float animSpeed = Intensity switch
        {
            FireIntensity.Spark => 0.3f,
            FireIntensity.Smolder => 0.25f,
            FireIntensity.Flame => 0.15f,
            FireIntensity.Blaze => 0.1f,
            FireIntensity.Inferno => 0.08f,
            FireIntensity.Dying => 0.3f,
            _ => 0.2f
        };

        AnimationTimer += delta;
        if (AnimationTimer >= animSpeed)
        {
            AnimationTimer = 0;
            UseAltChar = !UseAltChar;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Advance fire to next turn. Returns true if fire state changed.
    /// </summary>
    public bool AdvanceTurn()
    {
        if (!IsActive)
            return false;

        RemainingDuration--;

        if (RemainingDuration <= 0)
        {
            // Transition to next state
            Intensity = Intensity switch
            {
                FireIntensity.Spark => FireIntensity.Smolder,
                FireIntensity.Smolder => FireIntensity.Flame,
                FireIntensity.Flame => FireIntensity.Blaze,
                FireIntensity.Blaze => FireIntensity.Inferno,
                FireIntensity.Inferno => FireIntensity.Dying,
                FireIntensity.Dying => FireIntensity.Ash,
                _ => Intensity
            };

            RemainingDuration = StateData[(int)Intensity].Duration;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Intensify the fire (fuel added).
    /// </summary>
    public void Intensify()
    {
        if (Intensity < FireIntensity.Inferno)
        {
            Intensity++;
            RemainingDuration = StateData[(int)Intensity].Duration;
        }
        else
        {
            // Already at max, just extend duration
            RemainingDuration = Mathf.Max(RemainingDuration, StateData[(int)Intensity].Duration);
        }
    }

    /// <summary>
    /// Get the display character for this fire state.
    /// </summary>
    public readonly char GetCharacter()
    {
        return Intensity switch
        {
            FireIntensity.Spark => UseAltChar ? ',' : '·',
            FireIntensity.Smolder => UseAltChar ? '▒' : '░',
            FireIntensity.Flame => UseAltChar ? '▓' : '▒',
            FireIntensity.Blaze => UseAltChar ? '█' : '▓',
            FireIntensity.Inferno => UseAltChar ? '▓' : '█',
            FireIntensity.Dying => UseAltChar ? '·' : '░',
            FireIntensity.Ash => '·',
            _ => ' '
        };
    }

    /// <summary>
    /// Get the display color for this fire state.
    /// </summary>
    public readonly Color GetColor()
    {
        return Intensity switch
        {
            FireIntensity.Spark => Color.Color8(255, 180, 50),
            FireIntensity.Smolder => UseAltChar ? Color.Color8(200, 80, 30) : Color.Color8(180, 60, 20),
            FireIntensity.Flame => UseAltChar ? Color.Color8(255, 150, 30) : Color.Color8(255, 120, 20),
            FireIntensity.Blaze => UseAltChar ? Color.Color8(255, 220, 80) : Color.Color8(255, 180, 50),
            FireIntensity.Inferno => UseAltChar ? Color.Color8(255, 255, 200) : Color.Color8(255, 240, 150),
            FireIntensity.Dying => UseAltChar ? Color.Color8(150, 50, 20) : Color.Color8(180, 70, 30),
            FireIntensity.Ash => Color.Color8(60, 60, 60),
            _ => ASCIIColors.BgDark
        };
    }

    /// <summary>
    /// Get background color (glow effect).
    /// </summary>
    public readonly Color GetBackgroundColor()
    {
        return Intensity switch
        {
            FireIntensity.Spark => Color.Color8(40, 20, 5),
            FireIntensity.Smolder => Color.Color8(50, 20, 10),
            FireIntensity.Flame => Color.Color8(80, 30, 10),
            FireIntensity.Blaze => Color.Color8(100, 50, 15),
            FireIntensity.Inferno => Color.Color8(120, 60, 20),
            FireIntensity.Dying => Color.Color8(40, 15, 5),
            FireIntensity.Ash => Color.Color8(20, 20, 20),
            _ => ASCIIColors.BgDark
        };
    }
}

/// <summary>
/// Static data for each fire intensity level.
/// </summary>
public struct FireStateData
{
    public int DamagePerTurn;
    public float SpreadChance;
    public int LightRadius;
    public int Duration;
}
