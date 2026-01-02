using System;
using Godot;

namespace NullAndVoid.Rendering;

/// <summary>
/// Animated color system inspired by Brogue's "dancing colors".
/// Colors shift within deviation bounds on a timer, with base colors
/// never changing to prevent drift.
/// </summary>
public struct DancingColor
{
    public Color BaseColor;
    public Color Deviation;      // Max RGB deviation (0-1 range)
    public float Period;         // Seconds between color changes
    public float Timer;          // Current countdown
    public Color CurrentColor;   // The rendered color

    private static readonly Random _random = new();

    /// <summary>
    /// Create a static (non-dancing) color.
    /// </summary>
    public static DancingColor Static(Color color)
    {
        return new DancingColor
        {
            BaseColor = color,
            Deviation = new Color(0, 0, 0, 0),
            Period = 0f,
            Timer = 0f,
            CurrentColor = color
        };
    }

    /// <summary>
    /// Create a dancing color with specified deviation and period.
    /// </summary>
    public static DancingColor Dancing(Color baseColor, Color deviation, float period)
    {
        var dc = new DancingColor
        {
            BaseColor = baseColor,
            Deviation = deviation,
            Period = period,
            Timer = (float)_random.NextDouble() * period, // Randomize start phase
            CurrentColor = baseColor
        };
        dc.Recalculate();
        return dc;
    }

    /// <summary>
    /// Create a subtle dancing color (small deviations, slow period).
    /// </summary>
    public static DancingColor Subtle(Color baseColor, float intensity = 0.1f)
    {
        return Dancing(
            baseColor,
            new Color(intensity, intensity, intensity, 0),
            0.8f + (float)_random.NextDouble() * 0.4f
        );
    }

    /// <summary>
    /// Create a water-style dancing color (blue shifts, medium period).
    /// </summary>
    public static DancingColor Water(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.02f, 0.05f, 0.1f, 0),
            0.6f + (float)_random.NextDouble() * 0.4f
        );
    }

    /// <summary>
    /// Create a forest canopy dancing color (green-biased, slow organic movement).
    /// </summary>
    public static DancingColor ForestCanopy(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.03f, 0.08f, 0.03f, 0),
            1.2f + (float)_random.NextDouble() * 0.6f
        );
    }

    /// <summary>
    /// Create a grass wave dancing color (very subtle movement).
    /// </summary>
    public static DancingColor GrassWave(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.02f, 0.04f, 0.02f, 0),
            0.8f + (float)_random.NextDouble() * 0.4f
        );
    }

    /// <summary>
    /// Create a heat haze effect for mountains/hot terrain (subtle shimmer).
    /// </summary>
    public static DancingColor HeatHaze(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.02f, 0.02f, 0.03f, 0),
            0.5f + (float)_random.NextDouble() * 0.3f
        );
    }

    /// <summary>
    /// Create a marsh/swamp dancing color (murky, occasional bubble effect).
    /// </summary>
    public static DancingColor Marsh(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.03f, 0.05f, 0.04f, 0),
            1.0f + (float)_random.NextDouble() * 0.5f
        );
    }

    /// <summary>
    /// Create a tech flicker effect (rapid brightness variation).
    /// </summary>
    public static DancingColor TechFlicker(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.1f, 0.1f, 0.1f, 0),
            0.15f + (float)_random.NextDouble() * 0.1f
        );
    }

    /// <summary>
    /// Create a gentle pulse effect for important elements.
    /// </summary>
    public static DancingColor Pulse(Color baseColor)
    {
        return Dancing(
            baseColor,
            new Color(0.15f, 0.15f, 0.15f, 0),
            0.8f + (float)_random.NextDouble() * 0.4f
        );
    }

    /// <summary>
    /// Update the dancing color. Call each frame.
    /// </summary>
    public void Update(float delta)
    {
        if (Period <= 0)
            return;

        Timer -= delta;
        if (Timer <= 0)
        {
            Timer = Period;
            Recalculate();
        }
    }

    /// <summary>
    /// Force recalculation of current color.
    /// </summary>
    public void Recalculate()
    {
        if (Deviation.R == 0 && Deviation.G == 0 && Deviation.B == 0)
        {
            CurrentColor = BaseColor;
            return;
        }

        CurrentColor = new Color(
            Mathf.Clamp(BaseColor.R + RandomDeviation(Deviation.R), 0, 1),
            Mathf.Clamp(BaseColor.G + RandomDeviation(Deviation.G), 0, 1),
            Mathf.Clamp(BaseColor.B + RandomDeviation(Deviation.B), 0, 1),
            BaseColor.A
        );
    }

    private static float RandomDeviation(float max)
    {
        return (float)(_random.NextDouble() * 2 - 1) * max;
    }

    /// <summary>
    /// Apply a lighting multiplier to the current color.
    /// </summary>
    public readonly Color WithLighting(float multiplier)
    {
        return new Color(
            CurrentColor.R * multiplier,
            CurrentColor.G * multiplier,
            CurrentColor.B * multiplier,
            CurrentColor.A
        );
    }

    /// <summary>
    /// Lerp between this color and another for smooth transitions.
    /// </summary>
    public readonly Color LerpTo(Color target, float t)
    {
        return CurrentColor.Lerp(target, t);
    }

    /// <summary>
    /// Get the current color as a hex string for BBCode.
    /// </summary>
    public readonly string ToHex()
    {
        return CurrentColor.ToHtml(false);
    }

    /// <summary>
    /// Get the current color with lighting as a hex string for BBCode.
    /// </summary>
    public readonly string ToHex(float lightMultiplier)
    {
        return WithLighting(lightMultiplier).ToHtml(false);
    }
}
