using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    //this class is a placeholder for future support of properties or methods on the base class
    public class DIObjectGraphBase<TSource>
    {
        //this would be an ideal spot to put public readonly fields for the resolving query, such as Schema, Metrics, Executor, and so on, rather than being inside the ResolveFieldContext instance.
        //this could only contain fields that are not unique to a resolving field (such as Source), so as to not break multithreading support
        //with DI, any objects necessary could be brought in via dependency injection (such as Schema), so they really don't need to be in here
    }

    public class DIObjectGraphBase : DIObjectGraphBase<object>
    {

    }
}
