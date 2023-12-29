using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.Helpers;

public static class OptionsExtensions
{
    /// <summary>
    /// Gets a boolean option from the <see cref="AnalyzerOptions"/> with the specified name.
    /// </summary>
    /// <param name="analyzerOptions">The <see cref="AnalyzerOptions"/> instance.</param>
    /// <param name="name">The name of the option.</param>
    /// <param name="tree">The <see cref="SyntaxTree"/> associated with the analysis.</param>
    /// <param name="defaultValue">The default value if the option is not present or cannot be parsed.</param>
    /// <returns>
    /// The boolean option value. If the option is not present or cannot be parsed, returns the specified <paramref name="defaultValue"/>.
    /// </returns>
    public static bool GetBoolOption(this AnalyzerOptions analyzerOptions, string name, SyntaxTree tree, bool defaultValue = default)
    {
        var config = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

        if (config.TryGetValue(name, out string? configValue))
        {
            return bool.TryParse(configValue, out bool value) ? value : defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets a string option from the <see cref="AnalyzerOptions"/> with the specified name.
    /// </summary>
    /// <param name="analyzerOptions">The <see cref="AnalyzerOptions"/> instance.</param>
    /// <param name="name">The name of the option.</param>
    /// <param name="tree">The <see cref="SyntaxTree"/> associated with the analysis.</param>
    /// <param name="defaultValue">The default value if the option is not present or cannot be parsed.</param>
    /// <returns>
    /// The string option value. If the option is not present or cannot be parsed, returns the specified <paramref name="defaultValue"/>.
    /// </returns>
    public static string? GetStringOption(this AnalyzerOptions analyzerOptions, string name, SyntaxTree tree, string? defaultValue = default)
    {
        var config = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
        return config.TryGetValue(name, out string? configValue) ? configValue : defaultValue;
    }
}
