using Godot;

namespace NullAndVoid.Effects;

/// <summary>
/// How particles blend with existing buffer content.
/// </summary>
public enum ParticleBlendMode
{
    /// <summary>Replace the cell entirely.</summary>
    Replace,

    /// <summary>Only change foreground, keep background.</summary>
    Foreground,

    /// <summary>Only change background color.</summary>
    Background,

    /// <summary>Render behind existing content (for trails).</summary>
    Behind
}

/// <summary>
/// Type of particle for behavior/rendering variations.
/// </summary>
public enum ParticleType
{
    /// <summary>Fast linear projectile (bullets, nails).</summary>
    Bullet,

    /// <summary>Instant line effect (lasers, rails).</summary>
    Beam,

    /// <summary>Medium speed glowing projectile.</summary>
    Plasma,

    /// <summary>Curved electrical arc.</summary>
    Arc,

    /// <summary>Expanding ring explosion.</summary>
    Explosion,

    /// <summary>Random scatter sparks on impact.</summary>
    Sparks,

    /// <summary>Rising, fading smoke.</summary>
    Smoke,

    /// <summary>Falling debris fragments.</summary>
    Debris,

    /// <summary>Floating damage number.</summary>
    DamageNumber,

    /// <summary>Generic effect particle.</summary>
    Generic
}

/// <summary>
/// A single particle in the visual effects system.
/// Inspired by Cogmind's ASCII particle effects.
/// </summary>
public struct Particle
{
    /// <summary>
    /// Current position (sub-tile precision for smooth movement).
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Velocity in tiles per second.
    /// </summary>
    public Vector2 Velocity;

    /// <summary>
    /// Current display character.
    /// </summary>
    public char Character;

    /// <summary>
    /// Sequence of characters to animate through.
    /// If null, uses Character only.
    /// </summary>
    public char[]? CharacterSequence;

    /// <summary>
    /// Time between character changes (if using sequence).
    /// </summary>
    public float CharacterInterval;

    /// <summary>
    /// Current character sequence index.
    /// </summary>
    public int CharacterIndex;

    /// <summary>
    /// Time until next character change.
    /// </summary>
    public float CharacterTimer;

    /// <summary>
    /// Current particle color.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Color to fade toward over lifetime.
    /// </summary>
    public Color FadeColor;

    /// <summary>
    /// Whether to fade color over lifetime.
    /// </summary>
    public bool FadeEnabled;

    /// <summary>
    /// Total particle lifetime in seconds.
    /// </summary>
    public float Lifetime;

    /// <summary>
    /// Current age of particle.
    /// </summary>
    public float Age;

    /// <summary>
    /// Downward acceleration for arcing particles.
    /// </summary>
    public float Gravity;

    /// <summary>
    /// How particle blends with buffer.
    /// </summary>
    public ParticleBlendMode BlendMode;

    /// <summary>
    /// Type of particle for behavior variations.
    /// </summary>
    public ParticleType Type;

    /// <summary>
    /// Optional text for damage numbers.
    /// </summary>
    public string? Text;

    /// <summary>
    /// Whether this particle is still active.
    /// </summary>
    public bool IsAlive => Age < Lifetime;

    /// <summary>
    /// Progress through lifetime (0-1).
    /// </summary>
    public float Progress => Lifetime > 0 ? Age / Lifetime : 1f;

    /// <summary>
    /// Get current grid position (rounded).
    /// </summary>
    public Vector2I GridPosition => new((int)Mathf.Round(Position.X), (int)Mathf.Round(Position.Y));

    /// <summary>
    /// Get the current display character, handling animation.
    /// </summary>
    public char CurrentCharacter
    {
        get
        {
            if (CharacterSequence != null && CharacterSequence.Length > 0)
            {
                return CharacterSequence[CharacterIndex % CharacterSequence.Length];
            }
            return Character;
        }
    }

    /// <summary>
    /// Get the current color, handling fade.
    /// </summary>
    public Color CurrentColor
    {
        get
        {
            if (FadeEnabled)
            {
                return Color.Lerp(FadeColor, Progress);
            }
            return Color;
        }
    }

    /// <summary>
    /// Update particle state.
    /// </summary>
    public void Update(float delta)
    {
        Age += delta;

        // Apply velocity
        Position += Velocity * delta;

        // Apply gravity
        if (Gravity != 0)
        {
            Velocity += new Vector2(0, Gravity * delta);
        }

        // Update character animation
        if (CharacterSequence != null && CharacterSequence.Length > 1)
        {
            CharacterTimer -= delta;
            if (CharacterTimer <= 0)
            {
                CharacterTimer = CharacterInterval;
                CharacterIndex = (CharacterIndex + 1) % CharacterSequence.Length;
            }
        }
    }

    /// <summary>
    /// Create a basic particle.
    /// </summary>
    public static Particle Create(Vector2 position, char character, Color color, float lifetime)
    {
        return new Particle
        {
            Position = position,
            Velocity = Vector2.Zero,
            Character = character,
            Color = color,
            FadeColor = color,
            FadeEnabled = false,
            Lifetime = lifetime,
            Age = 0,
            Gravity = 0,
            BlendMode = ParticleBlendMode.Foreground,
            Type = ParticleType.Generic,
            CharacterInterval = 0.1f
        };
    }

    /// <summary>
    /// Create a moving particle.
    /// </summary>
    public static Particle CreateMoving(
        Vector2 position,
        Vector2 velocity,
        char character,
        Color color,
        float lifetime)
    {
        var p = Create(position, character, color, lifetime);
        p.Velocity = velocity;
        return p;
    }

    /// <summary>
    /// Create a fading particle.
    /// </summary>
    public static Particle CreateFading(
        Vector2 position,
        char character,
        Color startColor,
        Color endColor,
        float lifetime)
    {
        var p = Create(position, character, startColor, lifetime);
        p.FadeColor = endColor;
        p.FadeEnabled = true;
        return p;
    }

    /// <summary>
    /// Create an animated particle with character sequence.
    /// </summary>
    public static Particle CreateAnimated(
        Vector2 position,
        char[] characters,
        Color color,
        float lifetime,
        float characterInterval = 0.1f)
    {
        var p = Create(position, characters[0], color, lifetime);
        p.CharacterSequence = characters;
        p.CharacterInterval = characterInterval;
        p.CharacterTimer = characterInterval;
        return p;
    }
}
