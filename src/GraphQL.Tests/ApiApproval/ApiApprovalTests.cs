using GraphQL.Types;
using PublicApiGenerator;
using Shouldly;
using System;
using Xunit;

namespace GraphQL.Tests.ApiApproval
{
    /// <see href="https://github.com/JakeGinnivan/ApiApprover"/>
    public class ApiApprovalTests
    {
        [Theory]
        [InlineData(typeof(IGraphType))]
        public void PublicApi(Type type)
        {
            string publicApi = ApiGenerator.GeneratePublicApi(
                type.Assembly,
                shouldIncludeAssemblyAttributes: false,
                //whitelistedNamespacePrefixes: new[] { "Microsoft.Extensions.DependencyInjection" },
                excludeAttributes: new[] { "System.Diagnostics.DebuggerDisplayAttribute" });

            publicApi.ShouldMatchApproved(builder => builder.WithDescriminator(type.Assembly.GetName().Name));
        }
    }
}
