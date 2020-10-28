using System;
using System.Text.RegularExpressions;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Validator for GraphQL names.
    /// </summary>
    public static class NameValidator
    {
        private const string RESERVED_PREFIX = "__";
        private const string NAME_RX = @"^[_A-Za-z][_0-9A-Za-z]*$";

        /// <summary>
        /// Gets or sets current validation delegate. By default this delegate validates all names according
        /// to the GraphQL <see href="http://spec.graphql.org/June2018/#sec-Names">specification</see>.
        /// <br/>
        /// Setting this delegate allows you to use names not conforming to the specification, for example
        /// 'enum-member'. Only use it when absolutely necessary.
        /// </summary>
        public static Action<string, string> Validation = ValidateDefault;

        /// <summary>
        /// Validates a specified name.
        /// </summary>
        /// <param name="name">GraphQL name.</param>
        /// <param name="type">Type of element: field, type, argument, enum.</param>
        public static void ValidateName(string name, string type = "field") => Validation(name, type);

        /// <summary>
        /// Validates a specified name according to the GraphQL <see href="http://spec.graphql.org/June2018/#sec-Names">specification</see>.
        /// </summary>
        /// <param name="name">GraphQL name.</param>
        /// <param name="type">Type of element: field, type, argument, enum.</param>
        public static void ValidateDefault(string name, string type)
        {
            ValidateNameNotNull(name, type);

            if (name.Length > 1 && name.StartsWith(RESERVED_PREFIX, StringComparison.InvariantCulture))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A {type} name: {name} must not begin with \"__\", which is reserved by GraphQL introspection.");
            }
            if (!Regex.IsMatch(name, NAME_RX))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A {type} name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but {name} does not.");
            }
        }

        //TODO: maybe remove after
        internal static void ValidateNameNotNull(string name, string type = "field")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A {type} name can not be null or empty.");
            }
        }
    }
}
