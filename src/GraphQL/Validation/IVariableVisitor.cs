using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation
{
    public interface IVariableVisitor
    {
        void VisitScalar(ValidationContext context, VariableDefinition variable, VariableName variableName, ScalarGraphType type, object variableValue, object parsedValue);

        void VisitList(ValidationContext context, VariableDefinition variable, VariableName variableName, ListGraphType type, object variableValue, object parsedValue);

        void VisitObject(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object variableValue, object parsedValue);

        void VisitField(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object variableValue, object parsedValue);
    }

    public class VariableVisitorBase : IVariableVisitor
    {
        public virtual void VisitField(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object variableValue, object parsedValue)
        {
        }

        public virtual void VisitList(ValidationContext context, VariableDefinition variable, VariableName variableName, ListGraphType type, object variableValue, object parsedValue)
        {
        }

        public virtual void VisitObject(ValidationContext context, VariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object variableValue, object parsedValue)
        {
        }

        public virtual void VisitScalar(ValidationContext context, VariableDefinition variable, VariableName variableName, ScalarGraphType type, object variableValue, object parsedValue)
        {
        }
    }
}
