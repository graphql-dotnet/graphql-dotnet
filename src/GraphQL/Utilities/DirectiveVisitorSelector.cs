using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    public class DirectiveVisitorSelector : IVisitorSelector
    {
        private IDictionary<string, Type> _directiveVisitors;
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
            if (node is IProvideMetadata meta)
            {
                var ast = meta.GetAstType<IHasDirectivesNode>();
                if (ast == null) yield break;
                foreach (var visitor in BuildVisitors(ast.Directives))
                {
                    yield return visitor;
                }
            }
        }

        private IEnumerable<ISchemaNodeVisitor> BuildVisitors(IEnumerable<GraphQLDirective> directives)
        {
            var filtered = directives.Where(x => _directiveVisitors.ContainsKey(x.Name.Value)).ToList();
            foreach(var dir in filtered)
            {
                var visitor = _typeResolver(_directiveVisitors[dir.Name.Value]);
                visitor.Name = dir.Name.Value;
                visitor.Arguments = ToArguments(dir.Arguments);
                yield return visitor;
            }
        }

        private Dictionary<string, object> ToArguments(IEnumerable<GraphQLArgument> arguments)
        {
            return arguments.ToDictionary(x => x.Name.Value, x => x.Value.ToValue());
        }
    }
}
