using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

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

        int? NumberOfSkippedEntries { get; }

        int? TotalCount { get; }

        bool IsPartial { get; }
    }

    public interface IResolveConnectionContext<out T> : IResolveFieldContext<T>, IResolveConnectionContext
    {

    }
}
