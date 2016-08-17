using System;
using System.Text.RegularExpressions;

namespace GraphQL.SchemaGenerator.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        ///     Convert to camel case. ExampleWord -> exampleWord.
        /// </summary>
        public static string ConvertToCamelCase(string name)
        {
            if (name == null || name.Length <= 1)
            {
                return name;
            }

            return Char.ToLower(name[0]) + name.Substring(1);
        }

        /// <summary>
        ///     Remove special characters. Example@!#$23 -> Example23
        /// </summary>
        public static string SafeString(string name)
        {
            if (name == null)
            {
                return null;
            }

            return Regex.Replace(name, "[^0-9a-zA-Z]+", "");
        }

        /// <summary>
        ///     Get the graph name for this string.
        /// </summary>
        /// <returns>Camel case, safe string.</returns>
        public static string GraphName(string name)
        {
            return SafeString(ConvertToCamelCase(name));
        }
    }
}
