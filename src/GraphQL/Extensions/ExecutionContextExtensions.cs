using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL;

/// <summary>
/// Extension methods for <see cref="Execution.IExecutionContext"/>
/// </summary>
public static class ExecutionContextExtensions
{
    /// <summary>
    /// Gets the arguments for the specified field.
    /// Returns <see langword="null"/> if no arguments were defined for the field.
    /// </summary>
    public static IDictionary<string, ArgumentValue>? GetArguments(this IExecutionContext executionContext, FieldType fieldDefinition, GraphQLField astField)
        => executionContext.ArgumentValues?.TryGetValue(astField, out var args) == true ? args : fieldDefinition.DefaultArgumentValues;

    /// <summary>
    /// Gets the directives for the specified AST node.
    /// Returns <see langword="null"/> if no directives were supplied for the node.
    /// </summary>
    public static IDictionary<string, DirectiveInfo>? GetDirectives(this IExecutionContext executionContext, ASTNode astNode)
        => executionContext.DirectiveValues?.TryGetValue(astNode, out var directives) == true ? directives : null;
}
