using NullAndVoid.Core;

namespace NullAndVoid.Tests.TestHelpers;

/// <summary>
/// Simple test implementation of IGridEntity for unit testing.
/// </summary>
public class TestEntity : IGridEntity
{
    public string Id { get; }

    public TestEntity(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
    }
}
