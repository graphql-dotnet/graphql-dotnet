using System.Runtime.CompilerServices;

namespace GraphQL.Tests;

public class StackSlimTests
{
    [Fact]
    public void DefaultStackIsEmpty()
    {
        using StackSlim<object> stack = default;

        stack.Count.ShouldBe(0);
        stack.TryPop(out var value).ShouldBeFalse();
        value.ShouldBeNull();
        stack.Count.ShouldBe(0);
        stack.Clear();
        stack.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(130)]
    public void PushAndTryPopPreserveOrderAcrossInlineAndPooledStorage(int count)
    {
        using var stack = new StackSlim<int>();

        for (int i = 0; i < count; ++i)
        {
            stack.Push(i);
            stack.Count.ShouldBe(i + 1);
        }

        for (int i = count - 1; i >= 0; --i)
        {
            stack.TryPop(out var value).ShouldBeTrue();
            value.ShouldBe(i);
            stack.Count.ShouldBe(i);
        }

        stack.TryPop(out var emptyValue).ShouldBeFalse();
        emptyValue.ShouldBe(0);
        stack.Push(42);
        stack.TryPop(out var reusedValue).ShouldBeTrue();
        reusedValue.ShouldBe(42);
        stack.Count.ShouldBe(0);
    }

    [Fact]
    public void SupportsNullsAndTuplesContainingReferences()
    {
        using var stack = new StackSlim<(string? Value, int Index)>();
        stack.Push((null, 1));
        stack.Push(("inline", 2));
        stack.Push((null, 3));
        stack.Push(("pooled", 4));

        stack.TryPop(out var value).ShouldBeTrue();
        value.ShouldBe(("pooled", 4));
        stack.TryPop(out value).ShouldBeTrue();
        value.ShouldBe(((string?)null, 3));
        stack.TryPop(out value).ShouldBeTrue();
        value.ShouldBe(("inline", 2));
        stack.TryPop(out value).ShouldBeTrue();
        value.ShouldBe(((string?)null, 1));
        stack.TryPop(out value).ShouldBeFalse();
        value.ShouldBe(default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(130)]
    public void ClearAllowsReuse(int count)
    {
        using var stack = new StackSlim<int>();
        for (int i = 0; i < count; ++i)
            stack.Push(i);

        stack.Clear();
        stack.Count.ShouldBe(0);
        stack.TryPop(out _).ShouldBeFalse();
        stack.Clear();

        for (int i = 0; i < 35; ++i)
            stack.Push(i);
        for (int i = 34; i >= 0; --i)
        {
            stack.TryPop(out var value).ShouldBeTrue();
            value.ShouldBe(i);
        }
        stack.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(35)]
    public void DisposeIsRepeatableAndAllowsReuse(int count)
    {
        var stack = new StackSlim<int>();
        try
        {
            for (int i = 0; i < count; ++i)
                stack.Push(i);

            stack.Dispose();
            stack.Count.ShouldBe(0);
            stack.TryPop(out _).ShouldBeFalse();
            stack.Dispose();

            for (int i = 0; i < 35; ++i)
                stack.Push(i);
            for (int i = 34; i >= 0; --i)
            {
                stack.TryPop(out var value).ShouldBeTrue();
                value.ShouldBe(i);
            }
            stack.Count.ShouldBe(0);
        }
        finally
        {
            stack.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(18)]
    public void TryPopReleasesReferences(int retainedCount)
    {
        var stack = new StackSlim<(object Value, int Index)>();
        try
        {
            for (int i = 0; i < retainedCount; ++i)
                stack.Push((new object(), i));
            var reference = PushReference(ref stack);

            TryPopReference(ref stack);
            AssertReferencesCollected(ref stack, [reference], retainedCount);

            for (int i = retainedCount - 1; i >= 0; --i)
            {
                stack.TryPop(out var entry).ShouldBeTrue();
                entry.Value.ShouldNotBeNull();
                entry.Index.ShouldBe(i);
            }
        }
        finally
        {
            stack.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearAndDisposeReleaseInlineAndPooledReferences(bool dispose)
    {
        var stack = new StackSlim<(object Value, int Index)>();
        try
        {
            var references = new WeakReference[35];
            for (int i = 0; i < references.Length; ++i)
                references[i] = PushReference(ref stack);

            if (dispose)
                stack.Dispose();
            else
                stack.Clear();

            AssertReferencesCollected(ref stack, references, 0);
            stack.Push((new object(), 42));
            stack.TryPop(out var entry).ShouldBeTrue();
            entry.Index.ShouldBe(42);
        }
        finally
        {
            stack.Dispose();
        }
    }

    // Keep temporary strong references out of the method that triggers collection.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference PushReference(ref StackSlim<(object Value, int Index)> stack)
    {
        var value = new object();
        stack.Push((value, stack.Count));
        return new WeakReference(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TryPopReference(ref StackSlim<(object Value, int Index)> stack)
    {
        stack.TryPop(out _).ShouldBeTrue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertReferencesCollected(ref StackSlim<(object Value, int Index)> stack, WeakReference[] references, int expectedCount)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (var reference in references)
            reference.IsAlive.ShouldBeFalse();

        // Keep the stack live across collection so uncleared entries remain observable.
        stack.Count.ShouldBe(expectedCount);
    }
}
