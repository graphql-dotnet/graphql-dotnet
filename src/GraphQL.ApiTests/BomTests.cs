namespace GraphQL.ApiTests;

public class BomTests
{
    // https://github.com/graphql-dotnet/graphql-dotnet/pull/3477
    [Fact]
    public void Files_Should_Not_Use_BOM()
    {
        var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent!.Parent!.Parent!.Parent!.Parent!;

        byte[] buffer = new byte[3];
        int counter = 0;
        List<string> files = new();

        foreach (string file in Directory.EnumerateFiles(baseDir.FullName, "*.*", SearchOption.AllDirectories))
        {
            ++counter;

            if (file.EndsWith(".cs") || file.EndsWith(".csproj"))
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
            throw new InvalidOperationException("Remove BOM from files. Files with BOM found:" + Environment.NewLine + string.Join(Environment.NewLine, files));
    }
}
