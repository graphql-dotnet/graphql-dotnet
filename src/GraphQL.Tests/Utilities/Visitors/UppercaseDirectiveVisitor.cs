using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors
{
    /// <summary>
    /// Visitor for unit tests. Replace field resolver to return uppercase letters.
    /// Use <see cref="FuncFieldResolver{T}"/>.
    /// </summary>
    public class UppercaseDirectiveVisitor : SchemaDirectiveVisitor
    {
        public override string Name => "upper";

        public override void VisitFieldDefinition(FieldType field)
        {
            base.VisitFieldDefinition(field);

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

    /// <summary>
    /// Visitor for unit tests. Replace field resolver to return uppercase letters.
    /// Use <see cref="AsyncFieldResolver{T}"/>.
    /// </summary>
    public class AsyncUppercaseDirectiveVisitor : SchemaDirectiveVisitor
    {
        public override string Name => "upper";

        public override void VisitFieldDefinition(FieldType field)
        {
            base.VisitFieldDefinition(field);

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
