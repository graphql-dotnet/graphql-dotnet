using System.ComponentModel;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Tests;

public class AssemblyLevelAttributeTests
{
    [Fact]
    public void Assembly_DescriptionAttribute_HasCorrectValue()
    {
        typeof(ISchema).Assembly
            .GetCustomAttribute<DescriptionAttribute>()
            .ShouldNotBeNull()
            .Description.ShouldBe("__GraphQL_NET_Assembly__");
    }
}
