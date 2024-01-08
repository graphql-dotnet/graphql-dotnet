using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Attributes;

/// <summary>
/// Applies a directive to part of a schema.
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public class DirectiveAttribute : GraphQLAttribute
{
    /// <inheritdoc cref="DirectiveAttribute"/>
    public DirectiveAttribute(string name)
    {
        Name = name;
    }

    /// <inheritdoc cref="DirectiveAttribute"/>
    public DirectiveAttribute(string name, string argumentName, object argumentValue)
    {
        Name = name;
        Arguments.Add(argumentName, argumentValue);
    }

    /// <inheritdoc cref="DirectiveAttribute"/>
    public DirectiveAttribute(string name, string argumentName1, object argumentValue1, string argumentName2, object argumentValue2)
    {
        Name = name;
        Arguments.Add(argumentName1, argumentValue1);
        Arguments.Add(argumentName2, argumentValue2);
    }

    /// <inheritdoc cref="DirectiveAttribute"/>
    public DirectiveAttribute(string name, string argumentName1, object argumentValue1, string argumentName2, object argumentValue2, string argumentName3, object argumentValue3)
    {
        Name = name;
        Arguments.Add(argumentName1, argumentValue1);
        Arguments.Add(argumentName2, argumentValue2);
        Arguments.Add(argumentName3, argumentValue3);
    }

    /// <inheritdoc cref="DirectiveAttribute"/>
    /// <remarks>
    /// The <paramref name="argsAndValues"/> parameter must contain an even number of elements, where
    /// the first element of each pair is the argument name and the second element is the argument value.
    /// </remarks>
    public DirectiveAttribute(string name, params object[] argsAndValues)
    {
        Name = name;
        for (int i = 0; i < argsAndValues.Length; i += 2)
        {
            Arguments.Add((string)argsAndValues[i], argsAndValues[i + 1]);
        }
    }

    /// <summary>
    /// The name of the directive.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The arguments to the directive.
    /// </summary>
    public Dictionary<string, object?> Arguments = new();

    /// <inheritdoc/>
    public override void Modify(TypeConfig type) => type.ApplyDirective(Name, ApplyDirectives);

    /// <inheritdoc/>
    public override void Modify(FieldConfig field) => field.ApplyDirective(Name, ApplyDirectives);

    /// <inheritdoc/>
    public override void Modify(EnumValueDefinition enumValueDefinition) => enumValueDefinition.ApplyDirective(Name, ApplyDirectives);

    /// <inheritdoc/>
    public override void Modify(IGraphType graphType) => graphType.ApplyDirective(Name, ApplyDirectives);

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType) => fieldType.ApplyDirective(Name, ApplyDirectives);

    /// <inheritdoc/>
    public override void Modify(QueryArgument queryArgument) => queryArgument.ApplyDirective(Name, ApplyDirectives);

    private void ApplyDirectives(AppliedDirective directive)
    {
        foreach (var arg in Arguments)
        {
            directive.AddArgument(new DirectiveArgument(arg.Key) { Value = arg.Value });
        }
    }
}
