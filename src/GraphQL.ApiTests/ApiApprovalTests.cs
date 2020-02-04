using System;
using GraphQL.Types;
using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace GraphQL.ApiTests
{
    /// <see href="https://github.com/JakeGinnivan/ApiApprover"/>
    public class ApiApprovalTests
    {
        [Theory]
        [InlineData(typeof(IGraphType))]
        [InlineData(typeof(SystemTextJson.DocumentWriter))]
        [InlineData(typeof(NewtonsoftJson.DocumentWriter))]
        public void PublicApi(Type type)
        {
            string publicApi = type.Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                //WhitelistedNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" }
            });

            publicApi.ShouldMatchApproved(options => options.WithDiscriminator(type.Assembly.GetName().Name));
        }
    }
}
