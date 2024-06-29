using System.Diagnostics;

namespace GraphQL.Federation.Tests;

public abstract class BaseTest
{
    protected static void WaitForDebugger()
    {
        while (!Debugger.IsAttached)
        {
            Thread.Sleep(100);
        }
    }
}
