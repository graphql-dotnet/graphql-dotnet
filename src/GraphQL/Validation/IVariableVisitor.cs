using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Visitor which methods are called when parsing the inputs into variables in <see cref="ValidationContext.GetVariableValues"/>.
    /// </summary>
    public interface IVariableVisitor
    {
        /// <summary>
        /// Visits parsed scalar value.
        /// </summary>
        void VisitScalar(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue);

        /// <summary>
        /// Visits parsed list value.
        /// </summary>
        void VisitList(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue);

        /// <summary>
        /// Visits parsed input object value.
        /// </summary>
        void VisitObject(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue);

        /// <summary>
        /// Visits parsed value of input object field.
        /// </summary>
        void VisitField(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue);
    }
}
