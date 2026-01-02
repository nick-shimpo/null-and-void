using System;
using System.Threading.Tasks;
using Godot;
using NullAndVoid.Core;

namespace NullAndVoid.AI.Behaviors;

/// <summary>
/// Detect player noise and become alerted.
/// Sets investigate position when noise is detected.
/// This is a "sensor" behavior - it doesn't take an action but modifies memory.
/// </summary>
public class AlertOnNoiseBehavior : IBehavior
{
    public string Name => "AlertOnNoise";
    public int Priority { get; set; } = BehaviorPriorities.InvestigateNoise;

    /// <summary>
    /// Base detection range (modified by player noise level).
    /// </summary>
    public int BaseDetectionRange { get; set; } = 8;

    /// <summary>
    /// Noise threshold - player noise must exceed this to be detected.
    /// </summary>
    public int NoiseThreshold { get; set; } = 30;

    /// <summary>
    /// How many turns to investigate when noise is detected.
    /// </summary>
    public int InvestigateDuration { get; set; } = 8;

    /// <summary>
    /// Minimum alert level increase when noise is detected.
    /// </summary>
    public int MinAlertIncrease { get; set; } = 20;

    /// <summary>
    /// Whether this enemy can hear noise (some may be deaf/mechanical).
    /// </summary>
    public bool CanHear { get; set; } = true;

    private readonly Random _random = new();

    public AlertOnNoiseBehavior() { }

    public AlertOnNoiseBehavior(int baseRange, int threshold)
    {
        BaseDetectionRange = baseRange;
        NoiseThreshold = threshold;
    }

    public bool CanExecute(BehaviorContext context)
    {
        // Can't execute if we can't hear
        if (!CanHear)
            return false;

        // Don't need to detect if we can already see the target
        if (context.CanSeeTarget)
            return false;

        // Don't need to detect if we're already at max alert
        if (context.Memory.AlertLevel >= 100)
            return false;

        // Check if player is making noise within detection range
        if (context.Target == null)
            return false;

        // Get player noise level
        int playerNoise = context.Target.AttributesComponent?.Noise ?? 50;

        // Calculate detection range based on noise level
        // Higher noise = detected from further away
        int detectionRange = CalculateDetectionRange(playerNoise);

        // Check if player is within detection range
        return context.DistanceToTarget <= detectionRange && playerNoise > NoiseThreshold;
    }

    public async Task<BehaviorResult> Execute(BehaviorContext context)
    {
        if (context.Target == null)
        {
            await Task.CompletedTask;
            return new BehaviorResult(false, 0, "No target");
        }

        int playerNoise = context.Target.AttributesComponent?.Noise ?? 50;
        int detectionRange = CalculateDetectionRange(playerNoise);

        // Calculate how "loud" the noise seems (based on distance and noise level)
        float noiseIntensity = (float)(detectionRange - context.DistanceToTarget) / detectionRange;
        noiseIntensity = Mathf.Clamp(noiseIntensity, 0.1f, 1.0f);

        // Add some randomness to the perceived direction
        // Higher noise = more accurate, lower noise = might investigate wrong direction
        Vector2I perceivedPosition = CalculatePerceivedPosition(
            context.Self.GridPosition,
            context.Target.GridPosition,
            playerNoise
        );

        // Set investigate position in memory
        context.Memory.AlertToPosition(perceivedPosition, InvestigateDuration);

        // Increase alert level based on noise intensity
        int alertIncrease = (int)(MinAlertIncrease + (30 * noiseIntensity));
        context.Memory.AlertLevel = Mathf.Min(100, context.Memory.AlertLevel + alertIncrease);

        GD.Print($"{context.Self.EntityName} heard something! (noise: {playerNoise}, range: {detectionRange}, alert: {context.Memory.AlertLevel})");

        // This behavior doesn't take an action - it just updates memory
        // Return success with 0 cost so the next behavior can execute
        await Task.CompletedTask;
        return new BehaviorResult(true, 0, $"Heard noise at {perceivedPosition}");
    }

    /// <summary>
    /// Calculate detection range based on player noise level.
    /// </summary>
    private int CalculateDetectionRange(int playerNoise)
    {
        // Base range + bonus from noise
        // Every 10 noise above threshold adds 1 tile of detection range
        int noiseBonus = (playerNoise - NoiseThreshold) / 10;
        return BaseDetectionRange + noiseBonus;
    }

    /// <summary>
    /// Calculate perceived position of noise source.
    /// May be inaccurate for quiet noises.
    /// </summary>
    private Vector2I CalculatePerceivedPosition(Vector2I selfPos, Vector2I targetPos, int playerNoise)
    {
        // Higher noise = more accurate perception
        // At max noise (200), always accurate
        // At low noise, might be off by several tiles

        float accuracy = Mathf.Clamp((float)playerNoise / 150f, 0.3f, 1.0f);

        if (accuracy >= 0.9f || _random.NextDouble() < accuracy)
        {
            // Accurate perception
            return targetPos;
        }

        // Inaccurate - add some random offset
        int maxOffset = (int)((1.0f - accuracy) * 5);
        int offsetX = _random.Next(-maxOffset, maxOffset + 1);
        int offsetY = _random.Next(-maxOffset, maxOffset + 1);

        return new Vector2I(targetPos.X + offsetX, targetPos.Y + offsetY);
    }
}
