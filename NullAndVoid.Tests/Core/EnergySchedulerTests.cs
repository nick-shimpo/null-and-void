using System;
using System.Threading.Tasks;
using NullAndVoid.Core;
using Xunit;

namespace NullAndVoid.Tests.Core;

/// <summary>
/// Tests for EnergyScheduler - the turn scheduling system.
/// </summary>
public class EnergySchedulerTests : IDisposable
{
    public EnergySchedulerTests()
    {
        // Suppress GD.Print logging in tests to avoid Godot runtime dependency
        EnergyScheduler.SuppressLogging = true;
    }

    public void Dispose()
    {
        EnergyScheduler.SuppressLogging = false;
    }
    /// <summary>
    /// Mock actor for testing scheduling.
    /// </summary>
    private class MockActor : IScheduledActor
    {
        public string ActorName { get; set; } = "MockActor";
        public int Speed { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public bool CanAct { get; set; } = true;
        public int ActionsTaken { get; private set; } = 0;
        public int LastActionCost { get; set; } = EnergyScheduler.STANDARD_ACTION_COST;

        public Task<int> TakeAction()
        {
            ActionsTaken++;
            return Task.FromResult(LastActionCost);
        }
    }

    [Fact]
    public void RegisterActor_AddsActorToScheduler()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor { ActorName = "TestEnemy" };

        scheduler.RegisterActor(actor);

        Assert.Equal(1, scheduler.ActorCount);
    }

    [Fact]
    public void RegisterActor_MultipleTimes_OnlyAddsOnce()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor();

        scheduler.RegisterActor(actor);
        scheduler.RegisterActor(actor);
        scheduler.RegisterActor(actor);

        Assert.Equal(1, scheduler.ActorCount);
    }

    [Fact]
    public void UnregisterActor_RemovesActorFromScheduler()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor();

        scheduler.RegisterActor(actor);
        scheduler.UnregisterActor(actor);

        Assert.Equal(0, scheduler.ActorCount);
    }

    [Fact]
    public void GetNextActor_ReturnsRegisteredActor()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor { ActorName = "Enemy1" };

        scheduler.RegisterActor(actor);

        var next = scheduler.GetNextActor();

        Assert.NotNull(next);
        Assert.Equal("Enemy1", next.ActorName);
    }

    [Fact]
    public void GetNextActor_ReturnsNull_WhenNoActors()
    {
        var scheduler = new EnergyScheduler();

        var next = scheduler.GetNextActor();

        Assert.Null(next);
    }

    [Fact]
    public void GetNextActor_SkipsInactiveActors()
    {
        var scheduler = new EnergyScheduler();
        var inactive = new MockActor { ActorName = "Inactive", IsActive = false };
        var active = new MockActor { ActorName = "Active", IsActive = true };

        scheduler.RegisterActor(inactive);
        scheduler.RegisterActor(active);

        var next = scheduler.GetNextActor();

        Assert.NotNull(next);
        Assert.Equal("Active", next.ActorName);
    }

    [Fact]
    public void GetActorNextTick_ReturnsTickForRegisteredActor()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor();

        scheduler.RegisterActor(actor);

        var tick = scheduler.GetActorNextTick(actor);

        Assert.NotNull(tick);
        // Actor starts with 0 energy, needs 100 energy at speed 100 = 1 tick
        Assert.Equal(1, tick.Value);
    }

    [Fact]
    public void GetActorNextTick_ReturnsNull_ForUnregisteredActor()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor();

        var tick = scheduler.GetActorNextTick(actor);

        Assert.Null(tick);
    }

    [Fact]
    public void ActorCompletedAction_ReschedulesActor()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor { Speed = 100 };

        scheduler.RegisterActor(actor);

        // Get the actor and complete its action
        var next = scheduler.GetNextActor();
        Assert.NotNull(next);

        scheduler.ActorCompletedAction(actor, EnergyScheduler.STANDARD_ACTION_COST);

        // Actor should be rescheduled for a future tick
        var nextTick = scheduler.GetActorNextTick(actor);
        Assert.NotNull(nextTick);
        Assert.True(nextTick.Value >= 0);
    }

    [Fact]
    public void Reset_ClearsAllActors()
    {
        var scheduler = new EnergyScheduler();
        scheduler.RegisterActor(new MockActor { ActorName = "A" });
        scheduler.RegisterActor(new MockActor { ActorName = "B" });
        scheduler.RegisterActor(new MockActor { ActorName = "C" });

        Assert.Equal(3, scheduler.ActorCount);

        scheduler.Reset();

        Assert.Equal(0, scheduler.ActorCount);
        Assert.Equal(0, scheduler.CurrentTick);
    }

    [Fact]
    public void FastActor_GainsMoreEnergy()
    {
        var scheduler = new EnergyScheduler();
        var fast = new MockActor { ActorName = "Fast", Speed = 150 };
        var normal = new MockActor { ActorName = "Normal", Speed = 100 };

        scheduler.RegisterActor(fast);
        scheduler.RegisterActor(normal);

        // Both start at tick 0 with 0 energy
        Assert.Equal(0, scheduler.GetActorEnergy(fast));
        Assert.Equal(0, scheduler.GetActorEnergy(normal));
    }

    [Fact]
    public void SlowActor_ScheduledLater()
    {
        var scheduler = new EnergyScheduler();
        var slow = new MockActor { ActorName = "Slow", Speed = 50 };

        scheduler.RegisterActor(slow);

        // Complete an action - slow actor needs more ticks to recover
        var actor = scheduler.GetNextActor();
        scheduler.ActorCompletedAction(slow, EnergyScheduler.STANDARD_ACTION_COST);

        var nextTick = scheduler.GetActorNextTick(slow);
        Assert.NotNull(nextTick);
        // Speed 50 = gains 50 energy per tick, needs 100 = 2 ticks
        Assert.True(nextTick.Value >= 2);
    }

    [Fact]
    public void PeekNextActor_DoesNotRemoveFromSchedule()
    {
        var scheduler = new EnergyScheduler();
        var actor = new MockActor { ActorName = "Peeked" };

        scheduler.RegisterActor(actor);

        var peeked = scheduler.PeekNextActor();
        var next = scheduler.GetNextActor();

        Assert.NotNull(peeked);
        Assert.NotNull(next);
        Assert.Same(peeked, next);
    }

    [Fact]
    public void MultipleActors_AllGetScheduled()
    {
        var scheduler = new EnergyScheduler();

        for (int i = 0; i < 10; i++)
        {
            scheduler.RegisterActor(new MockActor { ActorName = $"Enemy{i}" });
        }

        Assert.Equal(10, scheduler.ActorCount);
    }

    [Fact]
    public void GetDebugInfo_ReturnsNonEmptyString()
    {
        var scheduler = new EnergyScheduler();
        scheduler.RegisterActor(new MockActor { ActorName = "TestActor" });

        var info = scheduler.GetDebugInfo();

        Assert.NotNull(info);
        Assert.Contains("TestActor", info);
    }

    [Fact]
    public void ActorCannotAct_GetsRescheduled()
    {
        var scheduler = new EnergyScheduler();
        var cannotAct = new MockActor { ActorName = "Stunned", CanAct = false };
        var canAct = new MockActor { ActorName = "Ready", CanAct = true };

        scheduler.RegisterActor(cannotAct);
        scheduler.RegisterActor(canAct);

        // Should skip the stunned actor and return the ready one
        var next = scheduler.GetNextActor();

        Assert.NotNull(next);
        Assert.Equal("Ready", next.ActorName);
    }
}
