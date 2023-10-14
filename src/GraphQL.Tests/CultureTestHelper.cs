using System.Globalization;

namespace GraphQL.Tests;

public static class CultureTestHelper
{
    private static IEnumerable<CultureInfo> Cultures => new[]
    {
        new CultureInfo("ru-RU"),
        new CultureInfo("fi-FI"),
        new CultureInfo("en-US"),
        CultureInfo.InvariantCulture,
        new CultureInfo(CultureInfo.CurrentUICulture.Name),
        new CultureInfo(CultureInfo.CurrentCulture.Name)
    };

    /// <summary>
    /// Executes the specified delegate with a variety of cultures.
    /// Be sure to mark the test class with <c>[Collection("StaticTests")]</c>
    /// to avoid the interference of static variables.
    /// </summary>
    public static void UseCultures(Action scope)
    {
        foreach (var culture in Cultures)
        {
            UseCulture(culture, scope);
        }
    }

    private static void UseCulture(CultureInfo culture, Action scope)
    {
        var before = CultureInfo.CurrentCulture;
        var beforeUi = CultureInfo.CurrentUICulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        try
        {
            scope();
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = before;
            CultureInfo.DefaultThreadCurrentUICulture = beforeUi;
        }
    }
}
