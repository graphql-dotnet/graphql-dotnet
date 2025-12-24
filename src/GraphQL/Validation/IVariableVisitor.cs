using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation;

/// <summary>
/// Visitor whose methods are called when parsing the inputs into variables in <see cref="ValidationContext.GetVariablesValuesAsync(IVariableVisitor?)"/>.
/// </summary>
public interface IVariableVisitor
{
    /// <summary>
    /// Visits parsed scalar value.
    /// </summary>
    public ValueTask VisitScalarAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue);

    /// <summary>
    /// Visits parsed list value.
    /// </summary>
    public ValueTask VisitListAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue);

    /// <summary>
    /// Visits parsed input object value.
    /// </summary>
    public ValueTask VisitObjectAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue);

    /// <summary>
    /// Visits parsed value of input object field.
    /// </summary>
    public ValueTask VisitFieldAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue);
}
