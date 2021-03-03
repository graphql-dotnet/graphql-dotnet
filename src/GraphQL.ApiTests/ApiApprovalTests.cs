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
        [InlineData(typeof(SubscriptionDocumentExecuter))]
        [InlineData(typeof(SystemTextJson.DocumentWriter))]
        [InlineData(typeof(NewtonsoftJson.DocumentWriter))]
        [InlineData(typeof(MicrosoftDI.ScopedFieldBuilderExtensions))]
        [InlineData(typeof(Caching.MemoryDocumentCache))]
        [InlineData(typeof(DataLoader.DataLoaderContext))]
        public void PublicApi(Type type)
        {
            string publicApi = type.Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                //WhitelistedNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" }
            });

            // See: https://shouldly.readthedocs.io/en/latest/assertions/shouldMatchApproved.html
            // Note: If the AssemblyName.approved.txt file doesn't match the latest publicApi value,
            // this call will try to launch a diff tool to help you out but that can fail on
            // your machine if a diff tool isn't configured/setup.
            publicApi.ShouldMatchApproved(options => options.WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
        }
    }
}
