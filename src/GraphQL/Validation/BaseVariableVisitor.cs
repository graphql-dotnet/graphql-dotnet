using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Base class implementing <see cref="IVariableVisitor"/>. Does nothing.
    /// Inherit from it if you need to override only some of the methods.
    /// </summary>
    public class BaseVariableVisitor : IVariableVisitor
    {
        /// <inheritdoc/>
        public virtual void VisitField(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
        {
        }

        /// <inheritdoc/>
        public virtual void VisitList(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue)
        {
        }

        /// <inheritdoc/>
        public virtual void VisitObject(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue)
        {
        }

        /// <inheritdoc/>
        public virtual void VisitScalar(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue)
        {
        }
    }
}
