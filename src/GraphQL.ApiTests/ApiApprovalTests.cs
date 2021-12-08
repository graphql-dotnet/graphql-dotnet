using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
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
            string projectName = type.Assembly.GetName().Name;
            string projectDir = Path.Combine(baseDir, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..");
            string buildDir = Path.Combine(projectDir, projectName, "bin", "Debug");
            Debug.Assert(Directory.Exists(buildDir), $"Directory '{buildDir}' doesn't exist");
            string csProject = Path.Combine(projectDir, projectName, projectName + ".csproj");
            var project = XDocument.Load(csProject);
            string[] tfms = project.Descendants("TargetFrameworks").Union(project.Descendants("TargetFramework")).First().Value.Split(";", StringSplitOptions.RemoveEmptyEntries);

            // There may be old stuff from earlier builds like net45, netcoreapp3.0, etc. so filter it out
            string[] actualTfmDirs = Directory.GetDirectories(buildDir).Where(dir => tfms.Any(tfm => dir.EndsWith(tfm))).ToArray();
            Debug.Assert(actualTfmDirs.Length > 0, $"Directory '{buildDir}' doesn't contain subdirectories matching {string.Join(";", tfms)}");

            (string tfm, string content)[] publicApi = actualTfmDirs.Select(tfmDir => (new DirectoryInfo(tfmDir).Name.Replace(".", ""), Assembly.LoadFile(Path.Combine(tfmDir, projectName + ".dll")).GeneratePublicApi(new ApiGeneratorOptions
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
                publicApi[0].content.ShouldMatchApproved(options => options.NoDiff().WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
            }
            else
            {
                var uniqueApi = publicApi.ToLookup(item => item.content);
                foreach (var item in uniqueApi)
                {
                    item.Key.ShouldMatchApproved(options => options.SubFolder(string.Join("+", item.Select(x => x.tfm).OrderBy(x => x))).NoDiff().WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
                }
            }
        }
    }
}
