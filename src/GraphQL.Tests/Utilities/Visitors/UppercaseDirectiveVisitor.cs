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
        public override void VisitField(FieldType field)
        {
            base.VisitField(field);

            var inner = field.Resolver ?? new NameFieldResolver();
            field.Resolver = new FuncFieldResolver<object>(context =>
            {
                var result = inner.Resolve(context);

                if (result is string str)
                {
                    return str.ToUpperInvariant();
                }

                return result;
            });
        }
    }

    /// <summary>
    /// Visitor for unit tests. Replace field resolver to return uppercase letters.
    /// Use <see cref="AsyncFieldResolver{T}"/>.
    /// </summary>
    public class AsyncUppercaseDirectiveVisitor : SchemaDirectiveVisitor
    {
        public override void VisitField(FieldType field)
        {
            base.VisitField(field);

            var inner = WrapResolver(field.Resolver);
            field.Resolver = new AsyncFieldResolver<object>(async context =>
            {
                var result = await inner.ResolveAsync(context);

                if (result is string str)
                {
                    return str.ToUpperInvariant();
                }

                return result;
            });
        }
    }
}
