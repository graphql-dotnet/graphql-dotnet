using GraphQL.Types;

namespace GraphQL.Conversion
{
    /// <summary>
    /// Sanitizes graph field and argument names to a particular case convention, such as camelСase or PascalCase.<br/>
    /// <br/>
    /// Set <see cref="Schema.NameConverter"/> to an instance of a derived class to select a converter to use.
    /// The default converter is <see cref="CamelCaseNameConverter"/>.<br/>
    /// <br/>
    /// Introspection fields always use <see cref="CamelCaseNameConverter"/> regardless of the selected <see cref="INameConverter"/>.
    /// </summary>
    public interface INameConverter
    {
        /// <summary>
        /// Sanitizes a field name for a specified parent graph type; returns the updated field name
        /// </summary>
        string NameForField(string fieldName, IComplexGraphType parentGraphType);

        /// <summary>
        /// Sanitizes an argument name for a specified parent graph type and field definition; returns the updated field name
        /// </summary>
        string NameForArgument(string argumentName, IComplexGraphType parentGraphType, FieldType field);
    }
}
