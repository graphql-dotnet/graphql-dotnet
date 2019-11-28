using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Builders
{
    public interface IResolveConnectionContext : IResolveFieldContext
    {

        public bool IsUnidirectional { get; }

        public int? First { get; }

        public int? Last { get; }

        public string After { get; }

        public string Before { get; }

        public int? PageSize { get; }
    }

    public interface IResolveConnectionContext<out T> : IResolveFieldContext<T>, IResolveConnectionContext
    {

    }
}
