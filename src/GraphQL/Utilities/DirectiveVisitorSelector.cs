using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    public class DirectiveVisitorSelector : IVisitorSelector
    {
        private readonly IDictionary<string, Type> _directiveVisitors;
        private readonly Func<Type, SchemaDirectiveVisitor> _typeResolver;

        public DirectiveVisitorSelector(
            IDictionary<string, Type> directiveVisitors,
            Func<Type, SchemaDirectiveVisitor> typeResolver)
        {
            _directiveVisitors = directiveVisitors;
            _typeResolver = typeResolver;
        }

        public IEnumerable<ISchemaNodeVisitor> Select(object node)
        {
            if (node is IProvideMetadata meta && meta.GetAstType<IHasDirectivesNode>() is IHasDirectivesNode ast)
            {
                if (ast.Directives != null)
                {
                    foreach (var visitor in BuildVisitors(ast.Directives))
                    {
                        yield return visitor;
                    }
                }
            }
        }

        private IEnumerable<ISchemaNodeVisitor> BuildVisitors(IEnumerable<GraphQLDirective> directives)
        {
            foreach (var dir in directives.Where(x => _directiveVisitors.ContainsKey(x.Name.ValueString)))
            {
                var visitor = _typeResolver(_directiveVisitors[dir.Name.ValueString]);
                visitor.Name = dir.Name.ValueString;
                if (dir.Arguments != null)
                    visitor.Arguments = ToArguments(dir.Arguments);
                yield return visitor;
            }
        }

        private Dictionary<string, object> ToArguments(List<GraphQLArgument> arguments)
        {
            return arguments.ToDictionary(x => x.Name.ValueString, x => x.Value.ToValue());
        }
    }
}
