using GraphQL.Types;

namespace GraphQL.Introspection;

/// <summary>
/// The <see cref="__Field"/> introspection type represents each field in an Object or Interface type.
/// </summary>
public class __Field : ObjectGraphType<IFieldType>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="__Field"/> introspection type.
    /// </summary>
    /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
    public __Field(bool allowAppliedDirectives = false)
        : this(allowAppliedDirectives, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="__Field"/> introspection type.
    /// </summary>
    /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
    /// <param name="deprecationOfInputValues">
    /// Allows deprecation of input values - arguments on a field or input fields on an input type.
    /// This feature is from a working draft of the specification.
    /// </param>
    public __Field(bool allowAppliedDirectives = false, bool deprecationOfInputValues = false)
    {
        SetName(nameof(__Field), validate: false);

        Description =
            "Object and Interface types are described by a list of Fields, each of " +
            "which has a name, potentially a list of arguments, and a return type.";

        Field<NonNullGraphType<StringGraphType>>("name").Resolve(context => context.Source.Name);

        Field<StringGraphType>("description").Resolve(context => context.Source.Description);

        var argsField = Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args")
            .ResolveAsync(async context =>
            {
                var source = context.Source;
                if (source.Arguments?.Count > 0)
                {
                    var arguments = context.ArrayPool.Rent<QueryArgument>(source.Arguments.Count);

                    bool includeDeprecated = context.GetArgument<bool>("includeDeprecated");

                    int index = 0;
                    foreach (var argument in source.Arguments.List!)
                    {
                        if ((includeDeprecated || string.IsNullOrWhiteSpace(argument.DeprecationReason)) && await context.Schema.Filter.AllowArgument(source, argument).ConfigureAwait(false))
                            arguments[index++] = argument;
                    }

                    var comparer = context.Schema.Comparer.ArgumentComparer(source);
                    if (comparer != null)
                        Array.Sort(arguments, 0, index, comparer);

                    return arguments.Constrained(index);
                }

                return Array.Empty<QueryArgument>();
            });
        if (deprecationOfInputValues)
            argsField.Argument<BooleanGraphType>("includeDeprecated", arg => arg.DefaultValue = BoolBox.False);

        Field<NonNullGraphType<__Type>>("type").Resolve(context => context.Source.ResolvedType);

        Field<NonNullGraphType<BooleanGraphType>>("isDeprecated").Resolve(context => (!string.IsNullOrWhiteSpace(context.Source.DeprecationReason)).Boxed());

        Field<StringGraphType>("deprecationReason").Resolve(context => context.Source.DeprecationReason);

        if (allowAppliedDirectives)
            this.AddAppliedDirectivesField("field");
    }
}
