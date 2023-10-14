using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    internal sealed class CompositeVariableVisitor : IVariableVisitor
    {
        private readonly List<IVariableVisitor> _visitors;

        public CompositeVariableVisitor(List<IVariableVisitor> visitors)
        {
            _visitors = visitors;
        }

        public async ValueTask VisitFieldAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, FieldType field, object? variableValue, object? parsedValue)
        {
            foreach (var visitor in _visitors)
                await visitor.VisitFieldAsync(context, variable, variableName, type, field, variableValue, parsedValue).ConfigureAwait(false);
        }

        public async ValueTask VisitListAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ListGraphType type, object? variableValue, IList<object?>? parsedValue)
        {
            foreach (var visitor in _visitors)
                await visitor.VisitListAsync(context, variable, variableName, type, variableValue, parsedValue).ConfigureAwait(false);
        }

        public async ValueTask VisitObjectAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, IInputObjectGraphType type, object? variableValue, object? parsedValue)
        {
            foreach (var visitor in _visitors)
                await visitor.VisitObjectAsync(context, variable, variableName, type, variableValue, parsedValue).ConfigureAwait(false);
        }

        public async ValueTask VisitScalarAsync(ValidationContext context, GraphQLVariableDefinition variable, VariableName variableName, ScalarGraphType type, object? variableValue, object? parsedValue)
        {
            foreach (var visitor in _visitors)
                await visitor.VisitScalarAsync(context, variable, variableName, type, variableValue, parsedValue).ConfigureAwait(false);
        }
    }
}
