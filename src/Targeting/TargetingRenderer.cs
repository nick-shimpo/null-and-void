using System.Collections.Generic;
using System.Linq;
using Godot;
using NullAndVoid.Combat;
using NullAndVoid.Entities;
using NullAndVoid.Rendering;
using NullAndVoid.World;

namespace NullAndVoid.Targeting;

/// <summary>
/// Renders targeting interface elements: cursor, line of fire, AoE radius.
/// </summary>
public class TargetingRenderer
{
    private readonly ASCIIBuffer _buffer;
    private readonly MapRenderer _mapRenderer;
    private MapViewport? _mapViewport;
    private SceneTree? _sceneTree;
    private Vector2I _playerPosition;

    // Targeting colors
    private static readonly Color _cursorGreen = new(0.3f, 1.0f, 0.3f);    // Valid target
    private static readonly Color _cursorYellow = new(1.0f, 0.9f, 0.3f);   // Partial cover
    private static readonly Color _cursorRed = new(1.0f, 0.3f, 0.3f);      // Blocked/invalid
    private static readonly Color _lineOfFireColor = new(0.5f, 0.5f, 0.5f);
    private static readonly Color _aoEHighlight = new(0.3f, 0.3f, 0.5f);   // AoE radius background
    private static readonly Color _aoECenter = new(1.0f, 0.6f, 0.2f);      // AoE center
    private static readonly Color _aoEEnemyHighlight = new(1.0f, 0.3f, 0.3f); // Enemy in AoE
    private static readonly Color _aoEFriendlyFire = new(1.0f, 1.0f, 0.2f);   // Friendly fire warning

    // Animation state
    private float _cursorPulse = 0f;
    private const float PULSE_SPEED = 4.0f;

    // Cached AoE info
    private AoEResult? _cachedAoEResult;
    private Vector2I _last_aoECenter;
    private int _lastAoERadius;

    public TargetingRenderer(ASCIIBuffer buffer, MapRenderer mapRenderer)
    {
        _buffer = buffer;
        _mapRenderer = mapRenderer;
    }

    /// <summary>
    /// Set the MapViewport for zoomed rendering.
    /// </summary>
    public void SetMapViewport(MapViewport? viewport)
    {
        _mapViewport = viewport;
    }

    /// <summary>
    /// Set a cell in the appropriate render target (MapViewport or main buffer).
    /// Coordinates come from MapToBuffer which returns screen coords when using MapViewport.
    /// </summary>
    private void SetTargetingCell(int x, int y, char character, Color color)
    {
        if (_mapRenderer.UseMapViewport && _mapViewport != null)
        {
            _mapViewport.SetCell(x, y, character, color);
        }
        else
        {
            _buffer.SetCell(x, y, character, color);
        }
    }

    /// <summary>
    /// Set a cell's background color in the appropriate render target.
    /// </summary>
    private void SetTargetingCellBackground(int x, int y, Color bgColor)
    {
        if (_mapRenderer.UseMapViewport && _mapViewport != null)
        {
            // MapViewport doesn't support background colors directly - use dim foreground
            // The targeting overlay effect will be less visible but still functional
            var cell = _mapViewport.GetCell(x, y);
            if (cell.HasValue)
            {
                _mapViewport.SetCell(x, y, cell.Value.character, cell.Value.color.Lerp(bgColor, 0.5f));
            }
        }
        else
        {
            _buffer.SetCellBackground(x, y, bgColor);
        }
    }

    /// <summary>
    /// Set the scene tree for entity lookups.
    /// </summary>
    public void SetSceneTree(SceneTree sceneTree)
    {
        _sceneTree = sceneTree;
    }

    /// <summary>
    /// Set the player position for friendly fire checks.
    /// </summary>
    public void SetPlayerPosition(Vector2I position)
    {
        _playerPosition = position;
    }

    /// <summary>
    /// Update animation state.
    /// </summary>
    public void Update(float delta)
    {
        _cursorPulse += delta * PULSE_SPEED;
        if (_cursorPulse > Mathf.Tau)
            _cursorPulse -= Mathf.Tau;
    }

    /// <summary>
    /// Render the targeting interface.
    /// </summary>
    public void Render(TargetingSystem targeting)
    {
        if (!targeting.IsActive)
            return;

        // Update player position for friendly fire checks
        _playerPosition = targeting.AttackerPosition;

        // Render AoE radius first (below everything)
        if (targeting.WeaponData?.AreaRadius > 0)
        {
            RenderAoERadius(targeting.CursorPosition, targeting.WeaponData.AreaRadius, targeting.AttackerPosition);
        }
        else
        {
            // Clear cache when not rendering AoE
            ClearAoECache();
        }

        // Render line of fire
        RenderLineOfFire(targeting.AttackerPosition, targeting.CursorPosition, targeting.CurrentLineOfFire);

        // Render cursor last (on top)
        RenderCursor(targeting.CursorPosition, targeting.CurrentLineOfFire.Result);

        // Render targeting info overlay
        RenderTargetingInfo(targeting);
    }

    /// <summary>
    /// Render the targeting cursor.
    /// </summary>
    private void RenderCursor(Vector2I position, LineOfFireResult lofResult)
    {
        var bufferPos = _mapRenderer.MapToBuffer(position);
        if (!bufferPos.HasValue)
            return;

        int x = bufferPos.Value.x;
        int y = bufferPos.Value.y;

        // Determine cursor color based on line of fire result
        Color cursorColor = lofResult switch
        {
            LineOfFireResult.Clear => _cursorGreen,
            LineOfFireResult.PartialCover => _cursorYellow,
            LineOfFireResult.Blocked => _cursorRed,
            LineOfFireResult.OutOfRange => _cursorRed,
            _ => _cursorYellow
        };

        // Apply pulse animation
        float pulse = 0.7f + 0.3f * Mathf.Sin(_cursorPulse);
        cursorColor = cursorColor * pulse;

        // Get view dimensions for bounds checking
        var (viewW, viewH) = _mapRenderer.GetViewDimensions();

        // Draw cursor brackets: [ X ]
        if (x > 0)
            SetTargetingCell(x - 1, y, '[', cursorColor);
        if (x < viewW - 1)
            SetTargetingCell(x + 1, y, ']', cursorColor);

        // Draw corner markers for more visibility
        if (y > 0)
        {
            SetTargetingCell(x, y - 1, '|', cursorColor * 0.7f);
        }
        if (y < viewH - 1)
        {
            SetTargetingCell(x, y + 1, '|', cursorColor * 0.7f);
        }
    }

    /// <summary>
    /// Render the line of fire from attacker to cursor.
    /// </summary>
    private void RenderLineOfFire(Vector2I from, Vector2I to, LineOfFireInfo lineOfFire)
    {
        if (lineOfFire.Path == null || lineOfFire.Path.Count < 2)
            return;

        // Skip first (attacker) and last (target) positions
        for (int i = 1; i < lineOfFire.Path.Count - 1; i++)
        {
            var pos = lineOfFire.Path[i];
            var bufferPos = _mapRenderer.MapToBuffer(pos);
            if (!bufferPos.HasValue)
                continue;

            int x = bufferPos.Value.x;
            int y = bufferPos.Value.y;

            // Determine line character based on direction
            char lineChar = GetLineChar(lineOfFire.Path[i - 1], pos, lineOfFire.Path[i + 1]);

            // Color based on whether this is the blocking position
            Color lineColor = _lineOfFireColor;
            if (lineOfFire.BlockingPosition.HasValue && pos == lineOfFire.BlockingPosition.Value)
            {
                lineColor = _cursorRed;
                lineChar = 'X';  // Mark blocking position
            }

            SetTargetingCell(x, y, lineChar, lineColor);
        }
    }

    /// <summary>
    /// Get the appropriate line character based on direction.
    /// </summary>
    private char GetLineChar(Vector2I prev, Vector2I current, Vector2I next)
    {
        var dir1 = current - prev;
        var dir2 = next - current;

        // Horizontal
        if (dir1.Y == 0 && dir2.Y == 0)
            return '-';

        // Vertical
        if (dir1.X == 0 && dir2.X == 0)
            return '|';

        // Diagonal
        if ((dir1.X != 0 && dir1.Y != 0) || (dir2.X != 0 && dir2.Y != 0))
        {
            // Main diagonal
            if ((dir1.X > 0 && dir1.Y > 0) || (dir1.X < 0 && dir1.Y < 0))
                return '\\';
            return '/';
        }

        // Corner
        return '+';
    }

    /// <summary>
    /// Render AoE radius highlight with entity information.
    /// </summary>
    private void RenderAoERadius(Vector2I center, int radius, Vector2I origin)
    {
        // Use cached result if position hasn't changed
        if (_cachedAoEResult == null || center != _last_aoECenter || radius != _lastAoERadius)
        {
            _cachedAoEResult = AoECalculator.CalculateCircle(center, radius, origin);
            _last_aoECenter = center;
            _lastAoERadius = radius;

            // Update entity info if we have scene tree
            if (_sceneTree != null)
            {
                AoECalculator.UpdateEntityInfo(_cachedAoEResult, _playerPosition, _sceneTree);
            }
        }

        foreach (var tile in _cachedAoEResult.AffectedTiles)
        {
            var bufferPos = _mapRenderer.MapToBuffer(tile.Position);
            if (!bufferPos.HasValue)
                continue;

            int x = bufferPos.Value.x;
            int y = bufferPos.Value.y;

            // Determine background color based on tile contents
            Color bgColor;
            if (tile.IsCenter)
            {
                // Center gets special highlight
                bgColor = _aoECenter;
            }
            else if (tile.HasPlayer)
            {
                // Friendly fire warning - pulsing yellow
                float pulse = 0.6f + 0.4f * Mathf.Sin(_cursorPulse * 2);
                bgColor = _aoEFriendlyFire * pulse;
            }
            else if (tile.HasEnemy && !tile.IsBlocked)
            {
                // Enemy in blast radius - highlight red
                bgColor = _aoEEnemyHighlight * tile.DamageMultiplier;
            }
            else if (tile.IsBlocked)
            {
                // Blocked tile - dim
                bgColor = _aoEHighlight * 0.3f;
            }
            else
            {
                // Normal AoE tile - fade based on damage falloff
                bgColor = _aoEHighlight * tile.DamageMultiplier;
            }

            SetTargetingCellBackground(x, y, bgColor);
        }
    }

    /// <summary>
    /// Get count of enemies in current AoE.
    /// </summary>
    public int GetEnemiesInAoE()
    {
        return _cachedAoEResult?.EnemyCount ?? 0;
    }

    /// <summary>
    /// Check if player is in current AoE (friendly fire).
    /// </summary>
    public bool IsFriendlyFireRisk()
    {
        return _cachedAoEResult?.HitsPlayer ?? false;
    }

    /// <summary>
    /// Clear cached AoE result.
    /// </summary>
    public void ClearAoECache()
    {
        _cachedAoEResult = null;
    }

    /// <summary>
    /// Render targeting information overlay.
    /// </summary>
    private void RenderTargetingInfo(TargetingSystem targeting)
    {
        // Draw info box in sidebar area
        int infoX = ASCIIBuffer.SidebarX + 1;
        int infoY = 25;  // Below other sidebar content

        // Clear area
        for (int y = infoY; y < infoY + 14; y++)
        {
            for (int x = infoX; x < ASCIIBuffer.Width - 1; x++)
            {
                _buffer.SetCell(x, y, ' ', ASCIIColors.TextNormal);
            }
        }

        // Header
        _buffer.DrawString(infoX, infoY, "=TARGETING=", ASCIIColors.TargetingCursor);

        // Weapon info
        if (targeting.ActiveWeapon != null)
        {
            var weapon = targeting.ActiveWeapon;
            var weaponData = weapon.WeaponData!;

            _buffer.DrawString(infoX, infoY + 2, weapon.Name, weapon.DisplayColor);
            _buffer.DrawString(infoX, infoY + 3, $"DMG {weaponData.DamageString}", ASCIIColors.TextNormal);
            _buffer.DrawString(infoX, infoY + 4, $"RNG {weaponData.Range}", ASCIIColors.TextNormal);
        }

        // Target info
        var target = targeting.CurrentTarget;
        int currentLine = infoY + 6;

        if (target != null)
        {
            _buffer.DrawString(infoX, currentLine, "Target:", ASCIIColors.TextDim);
            currentLine++;
            _buffer.DrawString(infoX, currentLine, target.Name, ASCIIColors.AlertDanger);
            currentLine++;

            // Health bar
            int barWidth = 12;
            int filledWidth = (int)(barWidth * target.HealthPercent);
            string healthBar = new string('#', filledWidth) + new string('-', barWidth - filledWidth);
            _buffer.DrawString(infoX, currentLine, $"[{healthBar}]", GetHealthColor(target.HealthPercent));
            currentLine++;

            // Accuracy
            Color accColor = AccuracyCalculator.GetAccuracyColor(target.Accuracy);
            _buffer.DrawString(infoX, currentLine, $"ACC {target.Accuracy}%", accColor);
            currentLine++;
        }
        else if (targeting.WeaponData?.AreaRadius > 0)
        {
            _buffer.DrawString(infoX, currentLine, "AoE Attack", _aoECenter);
            currentLine++;
            _buffer.DrawString(infoX, currentLine, $"Radius: {targeting.WeaponData.AreaRadius}", ASCIIColors.TextNormal);
            currentLine++;

            // Show enemies in blast radius
            int enemyCount = GetEnemiesInAoE();
            if (enemyCount > 0)
            {
                Color enemyColor = enemyCount > 2 ? _cursorGreen : _cursorYellow;
                _buffer.DrawString(infoX, currentLine, $"Enemies: {enemyCount}", enemyColor);
                currentLine++;
            }
            else
            {
                _buffer.DrawString(infoX, currentLine, "No enemies", ASCIIColors.TextDim);
                currentLine++;
            }
        }
        else
        {
            _buffer.DrawString(infoX, currentLine, "No target", ASCIIColors.TextDim);
            currentLine++;
        }

        // Friendly fire warning
        if (IsFriendlyFireRisk())
        {
            currentLine++;
            // Pulsing warning
            float pulse = 0.7f + 0.3f * Mathf.Sin(_cursorPulse * 2);
            Color warningColor = _aoEFriendlyFire * pulse;
            _buffer.DrawString(infoX, currentLine, "!! FRIENDLY FIRE !!", warningColor);
        }

        // Controls hint
        _buffer.DrawString(infoX, infoY + 13, "[Enter]Fire [Esc]Cancel", ASCIIColors.TextDim);
    }

    /// <summary>
    /// Get health bar color based on percentage.
    /// </summary>
    private Color GetHealthColor(float percent)
    {
        if (percent > 0.6f)
            return new Color(0.3f, 1.0f, 0.3f);  // Green
        if (percent > 0.3f)
            return new Color(1.0f, 0.8f, 0.3f);  // Yellow
        return new Color(1.0f, 0.3f, 0.3f);      // Red
    }
}
