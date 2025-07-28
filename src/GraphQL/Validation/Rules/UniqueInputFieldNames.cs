using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Unique input field names:
///
/// A GraphQL input object value is only valid if all supplied fields are
/// uniquely named.
/// 
/// <para>
/// Also validates that literals for OneOf Input Objects contain only one field.
/// </para>
/// </summary>
public sealed class UniqueInputFieldNames : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
    public static readonly UniqueInputFieldNames Instance = new();
    private UniqueInputFieldNames() { }

    /// <inheritdoc/>
    /// <exception cref="UniqueInputFieldNamesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new NodeVisitors(
            new MatchingNodeVisitor<GraphQLObjectValue>(
                enter: (objVal, context) =>
                {
                    var knownNameStack = context.TypeInfo.UniqueInputFieldNames_KnownNameStack ??= new();

                    knownNameStack.Push(context.TypeInfo.UniqueInputFieldNames_KnownNames!);
                    context.TypeInfo.UniqueInputFieldNames_KnownNames = null;
                },
                leave: (objVal, context) =>
                {
                    if (context.TypeInfo.GetInputType() is IInputObjectGraphType { IsOneOf: true })
                    {
                        var fieldCount = context.TypeInfo.UniqueInputFieldNames_KnownNames?.Count ?? 0;
                        if (fieldCount != 1)
                        {
                            context.ReportError(new OneOfInputValuesError(context, objVal));
                        }
                    }
                    context.TypeInfo.UniqueInputFieldNames_KnownNames = context.TypeInfo.UniqueInputFieldNames_KnownNameStack!.Pop();
                }),

            new MatchingNodeVisitor<GraphQLObjectField>(
                leave: (objField, context) =>
                {
                    var knownNames = context.TypeInfo.UniqueInputFieldNames_KnownNames ??= new();

                    if (knownNames.TryGetValue(objField.Name, out var value))
                    {
                        context.ReportError(new UniqueInputFieldNamesError(context, value, objField));
                    }
                    else
                    {
                        knownNames[objField.Name] = objField.Value;
                    }

                    if (objField.Value is GraphQLNullValue && context.TypeInfo.GetInputType(1) is IInputObjectGraphType { IsOneOf: true })
                    {
                        context.ReportError(new OneOfInputValuesError(context, objField));
                    }
                })
        );
}
