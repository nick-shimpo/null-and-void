namespace NullAndVoid.Tests.TestHelpers;

/// <summary>
/// A deterministic Random implementation for unit tests.
/// Allows specifying exact sequences of values to return.
/// </summary>
public class TestRandom : Random
{
    private readonly Queue<int> _nextIntValues = new();
    private readonly Queue<double> _nextDoubleValues = new();
    private int _defaultInt;
    private double _defaultDouble;

    /// <summary>
    /// Create a TestRandom that returns specified int values in order.
    /// </summary>
    public TestRandom(params int[] values)
    {
        foreach (var v in values)
            _nextIntValues.Enqueue(v);
    }

    /// <summary>
    /// Create a TestRandom that returns specified double values in order.
    /// </summary>
    public TestRandom(params double[] values)
    {
        foreach (var v in values)
            _nextDoubleValues.Enqueue(v);
    }

    /// <summary>
    /// Create a TestRandom with both int and double sequences.
    /// </summary>
    public TestRandom(int[] intValues, double[] doubleValues)
    {
        foreach (var v in intValues)
            _nextIntValues.Enqueue(v);
        foreach (var v in doubleValues)
            _nextDoubleValues.Enqueue(v);
    }

    /// <summary>
    /// Set a default value to return when the queue is exhausted.
    /// </summary>
    public TestRandom WithDefault(int defaultInt, double defaultDouble = 0.5)
    {
        _defaultInt = defaultInt;
        _defaultDouble = defaultDouble;
        return this;
    }

    /// <summary>
    /// Queue additional int values to return.
    /// </summary>
    public TestRandom ThenReturn(params int[] values)
    {
        foreach (var v in values)
            _nextIntValues.Enqueue(v);
        return this;
    }

    /// <summary>
    /// Queue additional double values to return.
    /// </summary>
    public TestRandom ThenReturnDouble(params double[] values)
    {
        foreach (var v in values)
            _nextDoubleValues.Enqueue(v);
        return this;
    }

    public override int Next()
    {
        return _nextIntValues.Count > 0 ? _nextIntValues.Dequeue() : _defaultInt;
    }

    public override int Next(int maxValue)
    {
        return _nextIntValues.Count > 0 ? _nextIntValues.Dequeue() : _defaultInt;
    }

    public override int Next(int minValue, int maxValue)
    {
        if (_nextIntValues.Count > 0)
        {
            var value = _nextIntValues.Dequeue();
            // Clamp to valid range for safety
            return Math.Clamp(value, minValue, maxValue - 1);
        }
        return Math.Clamp(_defaultInt, minValue, maxValue - 1);
    }

    public override double NextDouble()
    {
        return _nextDoubleValues.Count > 0 ? _nextDoubleValues.Dequeue() : _defaultDouble;
    }

    /// <summary>
    /// Create a TestRandom that always returns the same int value.
    /// </summary>
    public static TestRandom AlwaysReturns(int value)
    {
        return new TestRandom(new int[] { }).WithDefault(value);
    }

    /// <summary>
    /// Create a TestRandom that always returns the same double value.
    /// </summary>
    public static TestRandom AlwaysReturnsDouble(double value)
    {
        return new TestRandom(new int[] { }).WithDefault(0, value);
    }

    /// <summary>
    /// Create a TestRandom configured to always hit (for accuracy rolls).
    /// Returns 0.0 which is below any reasonable accuracy threshold.
    /// </summary>
    public static TestRandom AlwaysHits()
    {
        return new TestRandom(new int[] { }).WithDefault(0, 0.0);
    }

    /// <summary>
    /// Create a TestRandom configured to always miss (for accuracy rolls).
    /// Returns 1.0 which is above any reasonable accuracy threshold.
    /// </summary>
    public static TestRandom AlwaysMisses()
    {
        return new TestRandom(new int[] { }).WithDefault(0, 1.0);
    }
}
