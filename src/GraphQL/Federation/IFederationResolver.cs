using GraphQL.Types;

namespace GraphQL.Federation;

internal interface IFederationResolver
{
    Type SourceType { get; }
    IInputObjectGraphType? SourceGraphType { get; set; }
    object Resolve(IResolveFieldContext context, object source);
}
