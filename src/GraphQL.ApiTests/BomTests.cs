using System.Runtime.CompilerServices;

namespace GraphQL.ApiTests;

public class BomTests
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pull/3477
    [Fact(Skip = "Does not work")]
    public void Files_Should_Not_Use_BOM()
    {
        string GetPath([CallerFilePath] string path = "") => path; // <GIT_ROOT>\src\GraphQL.ApiTests\BomTests.cs

        var gitRoot = new DirectoryInfo(GetPath()).Parent!.Parent!.Parent!;
        // Protection from situations when this test is copied to another repo or the folder structure changed, etc.
        if (!File.Exists(Path.Combine(gitRoot.FullName, "src", "GraphQL.sln")))
            throw new InvalidOperationException("Unable to find repository root");

        byte[] buffer = new byte[3];
        int counter = 0;
        List<string> files = new();

        string[] extensions = { ".cs", ".csproj", ".sln" };
        foreach (string file in Directory.EnumerateFiles(gitRoot.FullName, "*.*", SearchOption.AllDirectories))
        {
            ++counter;

            if (extensions.Any(file.EndsWith))
            {
                using var stream = File.OpenRead(file);

                // https://en.wikipedia.org/wiki/Byte_order_mark
                if (stream.Read(buffer, 0, 3) == 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF) // EFBBBF
                {
                    files.Add(file);
                }
            }
        }

        Console.WriteLine("Files checked: " + counter);

        if (files.Count > 0)
            throw new InvalidOperationException("Remove BOM from files. You can do this with any Hex Editor such as Notepad++. Files with BOM found:" + Environment.NewLine + string.Join(Environment.NewLine, files));
    }
}
