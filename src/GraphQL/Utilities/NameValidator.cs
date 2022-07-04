namespace GraphQL.Utilities
{
    /// <summary>
    /// Validator for GraphQL names.
    /// </summary>
    public static class NameValidator
    {
        /// <summary>
        /// Validates a specified name.
        /// </summary>
        /// <param name="name">GraphQL name.</param>
        /// <param name="type">Type of element: field, type, argument, enum.</param>
        public static void ValidateName(string name, NamedElement type) => GlobalSwitches.NameValidation(name, type);

        /// <summary>
        /// Validates a specified name during schema initialization.
        /// </summary>
        /// <param name="name">GraphQL name.</param>
        /// <param name="type">Type of element: field, type, argument, enum.</param>
        internal static void ValidateNameOnSchemaInitialize(string name, NamedElement type) => ValidateDefault(name, type);

        /// <summary>
        /// Validates a specified name according to the GraphQL <see href="https://spec.graphql.org/October2021/#sec-Names">specification</see>.
        /// </summary>
        /// <param name="name">GraphQL name.</param>
        /// <param name="type">Type of element: field, type, argument, enum or directive.</param>
        public static void ValidateDefault(string name, NamedElement type)
        {
            ValidateNameNotNull(name, type);

            if (name.Length > 1 && name[0] == '_' && name[1] == '_')
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A {type.ToString().ToLower()} name: '{name}' must not begin with __, which is reserved by GraphQL introspection.");
            }

            var c = name[0];
            if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '_')
                ThrowMatchError();

            for (int i = 1; i < name.Length; ++i)
            {
                c = name[i];
                if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_')
                    ThrowMatchError();
            }

            void ThrowMatchError()
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A {type.ToString().ToLower()} name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but '{name}' does not.");
            }
        }

        //TODO: maybe remove after
        internal static void ValidateNameNotNull(string name, NamedElement type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"A {type.ToString().ToLower()} name can not be null or empty.");
            }
        }
    }
}
