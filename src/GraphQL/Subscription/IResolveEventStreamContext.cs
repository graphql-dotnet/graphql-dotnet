using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Subscription
{
    public interface IResolveEventStreamContext : IResolveFieldContext
    {

    }

    public interface IResolveEventStreamContext<out TSource> : IResolveFieldContext<TSource>
    {

    }
}
