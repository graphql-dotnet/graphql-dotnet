using GraphQL.Execution;
using GraphQL.Resolvers;

namespace GraphQL.Types;

/// <summary>
/// Represents a field of an object graph type.
/// </summary>
public class ObjectFieldType : FieldType, IFieldTypeWithArguments
{
    /// <inheritdoc/>
    public QueryArguments? Arguments { get; set; }

    /// <summary>
    /// This property contains the argument values supplied to the field resolver if no arguments
    /// to the field were supplied within the request. This property serves as an optimization in
    /// <see cref="ReadonlyResolveFieldContext.Arguments"/>. So basically, we are optimizing for
    /// the idea that much of the time there are no field arguments specified, and simply the
    /// default set needs to be returned.
    /// Note that this value is automatically initialized during schema initialization.
    /// </summary>
    internal IDictionary<string, ArgumentValue>? DefaultArgumentValues { get; set; }

    /// <summary>
    /// Gets or sets a field resolver for the field.
    /// </summary>
    public IFieldResolver? Resolver { get; set; }
}
