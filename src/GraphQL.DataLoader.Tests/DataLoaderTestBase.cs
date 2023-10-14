using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests;

public abstract class DataLoaderTestBase
{
    protected Fake Fake { get; } = new Fake();
}
