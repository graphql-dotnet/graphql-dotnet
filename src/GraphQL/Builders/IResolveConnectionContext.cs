using GraphQL.Types;

namespace GraphQL.Builders
{
    public interface IResolveConnectionContext : IResolveFieldContext
    {
        bool IsUnidirectional { get; }

        int? First { get; }

        int? Last { get; }

        string After { get; }

        string Before { get; }

        int? PageSize { get; }
    }

    public interface IResolveConnectionContext<out T> : IResolveFieldContext<T>, IResolveConnectionContext
    {
    }
}
