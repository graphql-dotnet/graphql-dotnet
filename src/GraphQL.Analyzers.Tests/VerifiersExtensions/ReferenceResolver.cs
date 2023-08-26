using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using NuGet.Frameworks;

namespace GraphQL.Analyzers.Tests.VerifiersExtensions;

public class ReferenceResolver
{
    public static ReferenceAssemblies ResolveReferenceAssemblies()
    {
        string assemblyLocation = Assembly
            .GetCallingAssembly()
            .GetAssemblyLocation();

        string dir = Path.GetFileName(Path.GetDirectoryName(assemblyLocation));
        var targetFramework = NuGetFramework.Parse(dir);

        return CreateReferenceAssemblies(targetFramework)
            .AddPackages(ImmutableArray.Create(
                new PackageIdentity("GraphQL", "7.3.0"),
                new PackageIdentity("GraphQL.MicrosoftDI", "7.3.0")));
    }

    public static ReferenceAssemblies CreateReferenceAssemblies(NuGetFramework targetFramework)
    {
        if (!targetFramework.IsPackageBased)
        {
            return ReferenceAssemblies.Default;
        }

        return new ReferenceAssemblies(
            targetFramework.ToString(),
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                targetFramework.Version.ToString(3)),
            Path.Combine("ref", targetFramework.ToString()));
    }
}
