using GraphQL.Types;

namespace GraphQL.Conversion
{
    /// <summary>
    /// A name converter which does not modify the names passed to it.
    /// </summary>
    public class DefaultNameConverter : INameConverter
    {
        /// <summary>
        /// Static instance of <see cref="DefaultNameConverter"/> that can be reused instead of creating new.
        /// </summary>
        public static readonly DefaultNameConverter Instance = new();

        /// <summary>
        /// Returns the field name without modification
        /// </summary>
        public string NameForField(string fieldName, IComplexGraphType parentGraphType) => fieldName;

        /// <summary>
        /// Returns the argument name without modification
        /// </summary>
        public string NameForArgument(string argumentName, IComplexGraphType parentGraphType, FieldType field) => argumentName;
    }
}
