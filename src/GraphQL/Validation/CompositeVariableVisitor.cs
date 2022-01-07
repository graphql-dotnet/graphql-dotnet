using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation
{
    internal sealed class CompositeVariableVisitor : IVariableVisitor
    {
        private readonly List<IVariableVisitor> _visitors;

        public CompositeVariableVisitor(List<IVariableVisitor> visitors)
        {
            _visitors = visitors;
        }

        public void VisitField(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
        {
            foreach (var visitor in _visitors)
                visitor.VisitField(context, variable, variableName, type, field, variableValue, parsedValue);
        }

        public void VisitList(ValidationContext context, VariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue)
        {
            foreach (var visitor in _visitors)
                visitor.VisitList(context, variable, variableName, type, variableValue, parsedValue);
        }

        public void VisitObject(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue)
        {
            foreach (var visitor in _visitors)
                visitor.VisitObject(context, variable, variableName, type, variableValue, parsedValue);
        }

        public void VisitScalar(ValidationContext context, VariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue)
        {
            foreach (var visitor in _visitors)
                visitor.VisitScalar(context, variable, variableName, type, variableValue, parsedValue);
        }
    }
}
