using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors
{
    /// <summary>
    /// Visitor for unit tests. Wraps field resolver and returns UPPERCASED result if it is string.
    /// </summary>
    public class UppercaseDirectiveVisitor : BaseSchemaNodeVisitor
    {
        public override void VisitFieldDefinition(FieldType field, ISchema schema)
        {
            if (field.HasAppliedDirectives() && field.GetAppliedDirectives().Find("upper") != null)
            {
                var inner = field.Resolver ?? NameFieldResolver.Instance;
                field.Resolver = new FuncFieldResolver<object>(context =>
                {
                    object result = inner.Resolve(context);

                    return result is string str
                        ? str.ToUpperInvariant()
                        : result;
                });
            }
        }
    }

    /// <summary>
    /// Visitor for unit tests. Wraps field resolver and returns UPPERCASED result if it is string.
    /// </summary>
    public class AsyncUppercaseDirectiveVisitor : BaseSchemaNodeVisitor
    {
        public override void VisitFieldDefinition(FieldType field, ISchema schema)
        {
            if (field.HasAppliedDirectives() && field.GetAppliedDirectives().Find("upper") != null)
            {
                var inner = field.Resolver ?? NameFieldResolver.Instance;
                field.Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    object result = await inner.ResolveAsync(context);

                    return result is string str
                        ? str.ToUpperInvariant()
                        : result;
                });
            }
        }
    }
}
