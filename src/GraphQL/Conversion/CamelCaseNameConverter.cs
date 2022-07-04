using GraphQL.Types;

namespace GraphQL.Conversion
{
    /// <summary>
    /// Camel case name converter; set as the default <see cref="INameConverter"/> within <see cref="Schema.NameConverter"/>.
    /// Always used by all introspection fields regardless of the selected <see cref="INameConverter"/>.
    /// </summary>
    public class CamelCaseNameConverter : INameConverter
    {
        /// <summary>
        /// Static instance of <see cref="CamelCaseNameConverter"/> that can be reused instead of creating new.
        /// </summary>
        public static readonly CamelCaseNameConverter Instance = new();

        /// <summary>
        /// Returns the field name converted to camelCase.
        /// </summary>
        public string NameForField(string fieldName, IComplexGraphType parentGraphType) => fieldName.ToCamelCase();

        /// <summary>
        /// Returns the argument name converted to camelCase.
        /// </summary>
        public string NameForArgument(string argumentName, IComplexGraphType parentGraphType, FieldType field) => argumentName.ToCamelCase();
    }
}
