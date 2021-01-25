using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    public class DirectiveVisitorSelector : IVisitorSelector
    {
        private readonly Dictionary<string, Type> _directiveVisitors;
        private readonly Func<Type, SchemaDirectiveVisitor> _typeResolver;

        public DirectiveVisitorSelector(
            Dictionary<string, Type> directiveVisitors,
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

        private IEnumerable<ISchemaNodeVisitor> BuildVisitors(List<GraphQLDirective> directives)
        {
            foreach (var dir in directives)
            {
                var name = (string)dir.Name.Value;
                if (_directiveVisitors.TryGetValue(name, out var type))
                {
                    var visitor = _typeResolver(type);
                    visitor.Name = name;
                    if (dir.Arguments != null)
                        visitor.Arguments = dir.Arguments.ToDictionary(x => (string)x.Name.Value, x => x.Value.ToValue());
                    yield return visitor;
                }
            }
        }
    }
}
