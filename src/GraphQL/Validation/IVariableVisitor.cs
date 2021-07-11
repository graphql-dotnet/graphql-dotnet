#nullable enable

using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;

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
        void VisitScalar(ValidationContext context, VariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue);

        /// <summary>
        /// Visits parsed list value.
        /// </summary>
        void VisitList(ValidationContext context, VariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue);

        /// <summary>
        /// Visits parsed input object value.
        /// </summary>
        void VisitObject(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue);

        /// <summary>
        /// Visits parsed value of input object field.
        /// </summary>
        void VisitField(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue);
    }
}
