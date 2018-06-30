using System;
using System.Text.RegularExpressions;

namespace GraphQL.Utilities
{
    public class FieldValidator
    {
        private static readonly string RESERVED_PREFIX = "__";
        private static readonly string NAME_RX = @"^[_A-Za-z][_0-9A-Za-z]*$";

        public static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A field name can not be null or empty.");
            }

            if (name.Length > 1 && name.StartsWith(RESERVED_PREFIX))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A field name: {name} must not begin with \"__\", which is reserved by GraphQL introspection.");
            }
            if (!Regex.IsMatch(name, NAME_RX))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A field name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but {name} does not.");
            }
        }
    }
}
