namespace NullAndVoid.Tests.AI;

using NullAndVoid.AI;

/// <summary>
/// Unit tests for EnemyMemory class.
/// Tests the memory and alerting system that helps enemies track targets.
/// </summary>
public class EnemyMemoryTests
{
    #region Initialization Tests

    [Fact]
    public void Initialize_SetsHomeAndGuardPosition()
    {
        // Arrange
        var memory = new EnemyMemory();
        var spawnPos = new Vector2I(10, 20);

        // Act
        memory.Initialize(spawnPos);

        // Assert
        memory.HomePosition.Should().Be(spawnPos);
        memory.GuardPosition.Should().Be(spawnPos);
    }

    [Fact]
    public void Default_AlertLevelIsZero()
    {
        // Arrange & Act
        var memory = new EnemyMemory();

        // Assert
        memory.AlertLevel.Should().Be(0);
    }

    [Fact]
    public void Default_LastKnownTargetPosIsNull()
    {
        // Arrange & Act
        var memory = new EnemyMemory();

        // Assert
        memory.LastKnownTargetPos.Should().BeNull();
    }

    #endregion

    #region Alert Tests

    [Fact]
    public void AlertToPosition_SetsInvestigatePosition()
    {
        // Arrange
        var memory = new EnemyMemory();
        var alertPos = new Vector2I(5, 10);

        // Act
        memory.AlertToPosition(alertPos);

        // Assert
        memory.InvestigatePosition.Should().Be(alertPos);
    }

    [Fact]
    public void AlertToPosition_SetsAlertedByAlly()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        memory.AlertToPosition(new Vector2I(5, 10));

        // Assert
        memory.AlertedByAlly.Should().BeTrue();
    }

    [Fact]
    public void AlertToPosition_IncreasesAlertLevel()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        memory.AlertToPosition(new Vector2I(5, 10));

        // Assert
        memory.AlertLevel.Should().Be(50);
    }

    [Fact]
    public void AlertToPosition_CapsAlertLevelAt100()
    {
        // Arrange
        var memory = new EnemyMemory { AlertLevel = 80 };

        // Act
        memory.AlertToPosition(new Vector2I(5, 10));

        // Assert
        memory.AlertLevel.Should().Be(100);
    }

    [Fact]
    public void AlertToPosition_SetsInvestigateTurns()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        memory.AlertToPosition(new Vector2I(5, 10), investigateTurns: 15);

        // Assert
        memory.InvestigateTurnsRemaining.Should().Be(15);
    }

    #endregion

    #region LastKnownTargetPos Tests

    [Fact]
    public void LastKnownTargetPos_CanBeSetAndRead()
    {
        // Arrange
        var memory = new EnemyMemory();
        var targetPos = new Vector2I(15, 25);

        // Act
        memory.LastKnownTargetPos = targetPos;

        // Assert
        memory.LastKnownTargetPos.Should().NotBeNull();
        memory.LastKnownTargetPos.Should().Be(targetPos);
    }

    [Fact]
    public void LastKnownTargetPos_HasValue_WhenSet()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        memory.LastKnownTargetPos = new Vector2I(10, 10);

        // Assert
        memory.LastKnownTargetPos.HasValue.Should().BeTrue();
    }

    [Fact]
    public void LastKnownTargetPos_HasValue_IsFalseWhenNull()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Assert
        memory.LastKnownTargetPos.HasValue.Should().BeFalse();
    }

    #endregion

    #region Turn Processing Tests

    [Fact]
    public void OnTurnStart_IncrementsTimeCounters()
    {
        // Arrange
        var memory = new EnemyMemory { TurnsSinceTargetSeen = 5 };

        // Act
        memory.OnTurnStart();

        // Assert
        memory.TurnsSinceTargetSeen.Should().Be(6);
    }

    [Fact]
    public void OnTurnStart_DecrementsInvestigateTurns()
    {
        // Arrange
        var memory = new EnemyMemory
        {
            InvestigateTurnsRemaining = 5,
            InvestigatePosition = new Vector2I(10, 10)
        };

        // Act
        memory.OnTurnStart();

        // Assert
        memory.InvestigateTurnsRemaining.Should().Be(4);
        memory.InvestigatePosition.Should().NotBeNull();
    }

    [Fact]
    public void OnTurnStart_ClearsInvestigatePositionWhenTurnsExpire()
    {
        // Arrange
        var memory = new EnemyMemory
        {
            InvestigateTurnsRemaining = 1,
            InvestigatePosition = new Vector2I(10, 10)
        };

        // Act
        memory.OnTurnStart();

        // Assert
        memory.InvestigateTurnsRemaining.Should().Be(0);
        memory.InvestigatePosition.Should().BeNull();
    }

    [Fact]
    public void OnTurnStart_ClearsAlertedByAlly()
    {
        // Arrange
        var memory = new EnemyMemory { AlertedByAlly = true };

        // Act
        memory.OnTurnStart();

        // Assert
        memory.AlertedByAlly.Should().BeFalse();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsLastKnownTargetPos()
    {
        // Arrange
        var memory = new EnemyMemory { LastKnownTargetPos = new Vector2I(10, 10) };

        // Act
        memory.Reset();

        // Assert
        memory.LastKnownTargetPos.Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsAlertLevel()
    {
        // Arrange
        var memory = new EnemyMemory { AlertLevel = 100 };

        // Act
        memory.Reset();

        // Assert
        memory.AlertLevel.Should().Be(0);
    }

    #endregion

    #region Ambush Tests

    [Fact]
    public void SetupAmbush_SetsAmbushPosition()
    {
        // Arrange
        var memory = new EnemyMemory();
        var ambushPos = new Vector2I(8, 12);

        // Act
        memory.SetupAmbush(ambushPos);

        // Assert
        memory.AmbushPosition.Should().Be(ambushPos);
    }

    [Fact]
    public void SetupAmbush_SetsDefaultDuration()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        memory.SetupAmbush(new Vector2I(8, 12));

        // Assert
        memory.AmbushTurnsRemaining.Should().Be(20);
    }

    [Fact]
    public void SetupAmbush_SetsCustomDuration()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        memory.SetupAmbush(new Vector2I(8, 12), duration: 30);

        // Assert
        memory.AmbushTurnsRemaining.Should().Be(30);
    }

    #endregion

    #region Patrol Tests

    [Fact]
    public void GetNextWaypoint_ReturnsNullWhenNoWaypoints()
    {
        // Arrange
        var memory = new EnemyMemory();

        // Act
        var waypoint = memory.GetNextWaypoint();

        // Assert
        waypoint.Should().BeNull();
    }

    [Fact]
    public void GetNextWaypoint_ReturnsCurrentWaypoint()
    {
        // Arrange
        var memory = new EnemyMemory
        {
            PatrolWaypoints = new()
            {
                new Vector2I(0, 0),
                new Vector2I(10, 0),
                new Vector2I(10, 10)
            }
        };

        // Act
        var waypoint = memory.GetNextWaypoint();

        // Assert
        waypoint.Should().Be(new Vector2I(0, 0));
    }

    [Fact]
    public void AdvanceWaypoint_CyclesWaypoints()
    {
        // Arrange
        var memory = new EnemyMemory
        {
            PatrolWaypoints = new()
            {
                new Vector2I(0, 0),
                new Vector2I(10, 0),
                new Vector2I(10, 10)
            },
            CurrentWaypointIndex = 2
        };

        // Act
        memory.AdvanceWaypoint();

        // Assert
        memory.CurrentWaypointIndex.Should().Be(0);
    }

    #endregion

    #region Path Caching Tests

    [Fact]
    public void IsPathValid_ReturnsTrueForValidPath()
    {
        // Arrange
        var destination = new Vector2I(20, 20);
        var memory = new EnemyMemory
        {
            CachedPath = new() { new Vector2I(0, 0), new Vector2I(10, 10), destination },
            CachedPathDestination = destination
        };

        // Act
        var isValid = memory.IsPathValid(destination);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsPathValid_ReturnsFalseForDifferentDestination()
    {
        // Arrange
        var memory = new EnemyMemory
        {
            CachedPath = new() { new Vector2I(0, 0), new Vector2I(10, 10) },
            CachedPathDestination = new Vector2I(10, 10)
        };

        // Act
        var isValid = memory.IsPathValid(new Vector2I(20, 20));

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidatePath_ClearsPathCache()
    {
        // Arrange
        var memory = new EnemyMemory
        {
            CachedPath = new() { new Vector2I(0, 0) },
            CachedPathDestination = new Vector2I(10, 10)
        };

        // Act
        memory.InvalidatePath();

        // Assert
        memory.CachedPath.Should().BeNull();
        memory.CachedPathDestination.Should().BeNull();
    }

    #endregion
}
