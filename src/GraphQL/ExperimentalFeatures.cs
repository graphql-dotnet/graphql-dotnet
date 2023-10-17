namespace GraphQL;

/// <summary>
/// Options for configuring experimental features.
/// </summary>
public class ExperimentalFeatures
{
    /// <summary>
    /// Enables ability to expose user-defined meta-information via introspection.
    /// See https://github.com/graphql/graphql-spec/issues/300 for more information.
    /// This property must be set before schema initialization.
    /// <br/><br/>
    /// This is completely experimental feature that are not in the official specification (yet).
    /// </summary>
    public bool AppliedDirectives { get; set; } = false;

    /// <summary>
    /// Enables ability to expose 'isRepeatable' field for directives via introspection.
    /// This property must be set before schema initialization.
    /// <br/><br/>
    /// This feature is from a working draft of the specification (not experimental, just not released yet).
    /// </summary>
    public bool RepeatableDirectives { get; set; } = false;

    /// <summary>
    /// Enables deprecation of input values - arguments on a field or input fields on an input type.
    /// This property must be set before schema initialization.
    /// <br/><br/>
    /// This feature is from a working draft of the specification (not experimental, just not released yet).
    /// </summary>
    public bool DeprecationOfInputValues { get; set; } = false;

    /// <summary>
    /// Specifies whether scalar or object variable types can be used where list types are defined.
    /// For instance, if this switch is enabled, a variable of type Int can be used for an [Int] argument.
    /// </summary>
    public bool AllowScalarVariablesForListTypes { get; set; }
}
