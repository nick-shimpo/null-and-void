using NullAndVoid.Tests.TestHelpers;

namespace NullAndVoid.Tests.Core;

/// <summary>
/// Unit tests for EntityGrid spatial hash grid.
/// </summary>
public class EntityGridTests : IDisposable
{
    private readonly EntityGrid _grid;

    public EntityGridTests()
    {
        // Reset singleton before each test
        EntityGrid.Reset();
        _grid = EntityGrid.Instance;
    }

    public void Dispose()
    {
        // Clean up after each test
        EntityGrid.Reset();
    }

    #region Registration Tests

    [Fact]
    public void Register_AddsEntityToGrid()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);

        // Act
        _grid.Register(entity, position);

        // Assert
        _grid.GetAt(position).Should().Be(entity);
        _grid.Count.Should().Be(1);
    }

    [Fact]
    public void Register_OverwritesExistingEntity()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var position = new Vector2I(5, 5);

        // Act
        _grid.Register(entity1, position);
        _grid.Register(entity2, position);

        // Assert
        _grid.GetAt(position).Should().Be(entity2);
        _grid.Count.Should().Be(1);
    }

    [Fact]
    public void Register_MultiplePositions_TracksAll()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var pos1 = new Vector2I(0, 0);
        var pos2 = new Vector2I(10, 10);

        // Act
        _grid.Register(entity1, pos1);
        _grid.Register(entity2, pos2);

        // Assert
        _grid.GetAt(pos1).Should().Be(entity1);
        _grid.GetAt(pos2).Should().Be(entity2);
        _grid.Count.Should().Be(2);
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Unregister_RemovesEntityFromGrid()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        _grid.Unregister(position);

        // Assert
        _grid.GetAt(position).Should().BeNull();
        _grid.Count.Should().Be(0);
    }

    [Fact]
    public void Unregister_WithEntityCheck_OnlyRemovesMatching()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var position = new Vector2I(5, 5);
        _grid.Register(entity2, position); // entity2 is at position

        // Act - try to unregister entity1 (not actually there)
        _grid.Unregister(position, entity1);

        // Assert - entity2 should still be there
        _grid.GetAt(position).Should().Be(entity2);
        _grid.Count.Should().Be(1);
    }

    [Fact]
    public void Unregister_WithEntityCheck_RemovesIfMatches()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        _grid.Unregister(position, entity);

        // Assert
        _grid.GetAt(position).Should().BeNull();
        _grid.Count.Should().Be(0);
    }

    [Fact]
    public void Unregister_EmptyPosition_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var action = () => _grid.Unregister(new Vector2I(100, 100));
        action.Should().NotThrow();
    }

    #endregion

    #region Move Tests

    [Fact]
    public void Move_UpdatesEntityPosition()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var from = new Vector2I(0, 0);
        var to = new Vector2I(5, 5);
        _grid.Register(entity, from);

        // Act
        _grid.Move(entity, from, to);

        // Assert
        _grid.GetAt(from).Should().BeNull();
        _grid.GetAt(to).Should().Be(entity);
        _grid.Count.Should().Be(1);
    }

    [Fact]
    public void Move_OnlyRemovesOwnPosition()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var pos1 = new Vector2I(0, 0);
        var pos2 = new Vector2I(5, 5);
        var pos3 = new Vector2I(10, 10);

        _grid.Register(entity1, pos1);
        _grid.Register(entity2, pos2);

        // Act - move entity1, but try to claim entity2's old position was ours
        _grid.Move(entity1, pos2, pos3); // entity1 wasn't at pos2

        // Assert - entity2 should still be at pos2, entity1 at pos3
        _grid.GetAt(pos2).Should().Be(entity2);
        _grid.GetAt(pos3).Should().Be(entity1);
        _grid.GetAt(pos1).Should().Be(entity1); // Old position not cleared
    }

    [Fact]
    public void Move_ToSamePosition_Works()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        _grid.Move(entity, position, position);

        // Assert
        _grid.GetAt(position).Should().Be(entity);
        _grid.Count.Should().Be(1);
    }

    #endregion

    #region GetAt Tests

    [Fact]
    public void GetAt_EmptyPosition_ReturnsNull()
    {
        // Act
        var result = _grid.GetAt(new Vector2I(999, 999));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAt_OccupiedPosition_ReturnsEntity()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        var result = _grid.GetAt(position);

        // Assert
        result.Should().Be(entity);
    }

    [Fact]
    public void GetAt_NegativeCoordinates_Works()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(-10, -10);
        _grid.Register(entity, position);

        // Act
        var result = _grid.GetAt(position);

        // Assert
        result.Should().Be(entity);
    }

    #endregion

    #region IsOccupied Tests

    [Fact]
    public void IsOccupied_EmptyPosition_ReturnsFalse()
    {
        // Act
        var result = _grid.IsOccupied(new Vector2I(999, 999));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsOccupied_OccupiedPosition_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        var result = _grid.IsOccupied(position);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsOccupiedByOther Tests

    [Fact]
    public void IsOccupiedByOther_EmptyPosition_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity("test-1");

        // Act
        var result = _grid.IsOccupiedByOther(new Vector2I(999, 999), entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsOccupiedByOther_SameEntity_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        var result = _grid.IsOccupiedByOther(position, entity);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsOccupiedByOther_DifferentEntity_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var position = new Vector2I(5, 5);
        _grid.Register(entity1, position);

        // Act
        var result = _grid.IsOccupiedByOther(position, entity2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOccupiedByOther_NullExclude_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var position = new Vector2I(5, 5);
        _grid.Register(entity, position);

        // Act
        var result = _grid.IsOccupiedByOther(position, null);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetEntitiesInRadius Tests

    [Fact]
    public void GetEntitiesInRadius_EmptyGrid_ReturnsEmpty()
    {
        // Act
        var result = _grid.GetEntitiesInRadius(new Vector2I(5, 5), 3).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetEntitiesInRadius_SingleEntity_InRadius_Returns()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var entityPos = new Vector2I(5, 5);
        var center = new Vector2I(6, 6);
        _grid.Register(entity, entityPos);

        // Act
        var result = _grid.GetEntitiesInRadius(center, 2).ToList();

        // Assert
        result.Should().Contain(entity);
    }

    [Fact]
    public void GetEntitiesInRadius_SingleEntity_OutOfRadius_ReturnsEmpty()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var entityPos = new Vector2I(0, 0);
        var center = new Vector2I(10, 10);
        _grid.Register(entity, entityPos);

        // Act
        var result = _grid.GetEntitiesInRadius(center, 2).ToList();

        // Assert
        result.Should().NotContain(entity);
    }

    [Fact]
    public void GetEntitiesInRadius_MultipleEntities_ReturnsAllInRange()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var entity3 = new TestEntity("test-3");
        var center = new Vector2I(5, 5);

        _grid.Register(entity1, new Vector2I(5, 5)); // At center
        _grid.Register(entity2, new Vector2I(6, 5)); // 1 tile away
        _grid.Register(entity3, new Vector2I(10, 10)); // 5 tiles away

        // Act
        var result = _grid.GetEntitiesInRadius(center, 2).ToList();

        // Assert
        result.Should().Contain(entity1);
        result.Should().Contain(entity2);
        result.Should().NotContain(entity3);
    }

    [Fact]
    public void GetEntitiesInRadius_ZeroRadius_OnlyReturnsAtCenter()
    {
        // Arrange
        var entity1 = new TestEntity("test-1");
        var entity2 = new TestEntity("test-2");
        var center = new Vector2I(5, 5);

        _grid.Register(entity1, center);
        _grid.Register(entity2, new Vector2I(6, 5));

        // Act
        var result = _grid.GetEntitiesInRadius(center, 0).ToList();

        // Assert
        result.Should().Contain(entity1);
        result.Should().NotContain(entity2);
    }

    [Fact]
    public void GetEntitiesInRadius_IncludesDiagonals()
    {
        // Arrange
        var entity = new TestEntity("test-1");
        var center = new Vector2I(5, 5);
        var diagonalPos = new Vector2I(6, 6); // Diagonal from center
        _grid.Register(entity, diagonalPos);

        // Act
        var result = _grid.GetEntitiesInRadius(center, 1).ToList();

        // Assert
        result.Should().Contain(entity);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEntities()
    {
        // Arrange
        _grid.Register(new TestEntity("test-1"), new Vector2I(0, 0));
        _grid.Register(new TestEntity("test-2"), new Vector2I(5, 5));
        _grid.Register(new TestEntity("test-3"), new Vector2I(10, 10));

        // Act
        _grid.Clear();

        // Assert
        _grid.Count.Should().Be(0);
        _grid.GetAt(new Vector2I(0, 0)).Should().BeNull();
        _grid.GetAt(new Vector2I(5, 5)).Should().BeNull();
        _grid.GetAt(new Vector2I(10, 10)).Should().BeNull();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_CreatesNewInstance()
    {
        // Arrange
        var instance1 = EntityGrid.Instance;
        instance1.Register(new TestEntity("test-1"), new Vector2I(0, 0));

        // Act
        EntityGrid.Reset();
        var instance2 = EntityGrid.Instance;

        // Assert
        instance2.Should().NotBeSameAs(instance1);
        instance2.Count.Should().Be(0);
    }

    #endregion

    #region Singleton Tests

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Act
        var instance1 = EntityGrid.Instance;
        var instance2 = EntityGrid.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    #endregion
}
