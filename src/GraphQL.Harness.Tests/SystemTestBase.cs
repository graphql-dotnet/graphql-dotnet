using Alba;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Harness.Tests;

public class SystemTestBase<T> where T : class
{
    protected async Task<IScenarioResult> run(Action<Scenario> configuration, Action<AlbaHost> systemConfigure = null)
    {
        using var system = AlbaHost.ForStartup<T>(b => b.ConfigureServices((_, services) => services.AddMvcCore()));
        // system.Environment.EnvironmentName = "Testing";
        systemConfigure?.Invoke(system);
        return await system.Scenario(configuration).ConfigureAwait(false);
    }
}
