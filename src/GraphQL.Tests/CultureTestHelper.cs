using System;
using System.Collections.Generic;
using System.Globalization;

namespace GraphQL.Tests
{
    public class CultureTestHelper
    {
        public static IEnumerable<CultureInfo> Cultures => new[]
        {
            new CultureInfo("fi-FI"),
            new CultureInfo("en-US"),
            CultureInfo.InvariantCulture,
            new CultureInfo(CultureInfo.CurrentUICulture.Name),
            new CultureInfo(CultureInfo.CurrentCulture.Name)
        };

        public static void UseCultures(Action scope)
        {
            foreach (var culture in Cultures)
            {
                UseCulture(culture, scope);
            }
        }

        public static void UseCulture(CultureInfo culture, Action scope)
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
}
