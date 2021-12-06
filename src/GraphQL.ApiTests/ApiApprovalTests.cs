using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string asmName = type.Assembly.GetName().Name;
            string tfmsDir = Path.Combine(baseDir, "..\\..\\..\\..", asmName, "bin", "Debug");
            Debug.Assert(Directory.Exists(tfmsDir));
            string[] tfms = Directory.GetDirectories(tfmsDir);
            Debug.Assert(tfms.Length > 0);

            foreach (string tfmDir in tfms)
            {
                var asm = Assembly.LoadFile(Path.Combine(tfmDir, asmName + ".dll"));
                string publicApi = asm.GeneratePublicApi(new ApiGeneratorOptions
                {
                    IncludeAssemblyAttributes = false,
                    //WhitelistedNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
                    ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute" }
                }) + Environment.NewLine;

                // See: https://shouldly.readthedocs.io/en/latest/assertions/shouldMatchApproved.html
                // Note: If the AssemblyName.approved.txt file doesn't match the latest publicApi value,
                // this call will try to launch a diff tool to help you out but that can fail on
                // your machine if a diff tool isn't configured/setup.
                string tfm = new DirectoryInfo(tfmDir).Name.Replace(".", "");
                publicApi.ShouldMatchApproved(options => options.SubFolder(tfm).WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
            }
        }
    }
}
