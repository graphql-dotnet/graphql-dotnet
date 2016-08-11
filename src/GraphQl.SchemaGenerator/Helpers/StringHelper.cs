using System;

namespace GraphQL.SchemaGenerator.Helpers
{
    public static class StringHelper
    {
        public static string ConvertToCamelCase(string name)
        {
            if (name == null || name.Length <= 1)
            {
                return name;
            }

            return Char.ToLower(name[0]) + name.Substring(1);
        }
    }
}
