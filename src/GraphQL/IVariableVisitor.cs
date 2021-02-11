using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL
{
    public interface IVariableVisitor
    {
        void VisitScalar(VariableDefinition variable, VariableName variableName, ScalarGraphType type, object variableValue, object parsedValue);

        void VisitList(VariableDefinition variable, VariableName variableName, ListGraphType type, object variableValue, object parsedValue);

        void VisitObject(VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object variableValue, object parsedValue);

        void VisitField(VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object variableValue, object parsedValue);
    }
}
