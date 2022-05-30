namespace GraphQL.Tests;

internal static class FileExtensions
{
    internal static string ReadGraphQLRequest(this string fileName) => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Files", "GraphQL", fileName + ".graphql"));

    internal static string ReadSDL(this string fileName) => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Files", "SDL", fileName + ".graphql"));

    internal static string ReadJsonResult(this string fileName) => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Files", "JSON", fileName + ".json"));
}
