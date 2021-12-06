using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            string libDir = Path.Combine(baseDir, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..", asmName, "bin", "Debug");
            Debug.Assert(Directory.Exists(libDir), $"Directory '{libDir}' doesn't exist");
            string[] tfmDirs = Directory.GetDirectories(libDir);
            Debug.Assert(tfmDirs.Length > 0, $"Directory '{libDir}' doesn't contain subdirectories");

            (string tfm, string content)[] publicApi = tfmDirs.Select(tfmDir => (new DirectoryInfo(tfmDir).Name.Replace(".", ""), Assembly.LoadFile(Path.Combine(tfmDir, asmName + ".dll")).GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                //WhitelistedNamespacePrefixes = new[] { "Microsoft.Extensions.DependencyInjection" },
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute", "System.Diagnostics.CodeAnalysis.AllowNullAttribute" }
            }) + Environment.NewLine)).ToArray();

            // See: https://shouldly.readthedocs.io/en/latest/assertions/shouldMatchApproved.html
            // Note: If the AssemblyName.approved.txt file doesn't match the latest publicApi value,
            // this call will try to launch a diff tool to help you out but that can fail on
            // your machine if a diff tool isn't configured/setup.
            if (publicApi.DistinctBy(item => item.content).Count() == 1)
            {
                publicApi[0].content.ShouldMatchApproved(options => options.WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
            }
            else
            {
                var uniqueApi = publicApi.ToLookup(item => item.content);
                foreach (var item in uniqueApi)
                {
                    item.Key.ShouldMatchApproved(options => options.SubFolder(string.Join("+", item.Select(x => x.tfm))).WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
                }
            }
        }
    }
}
