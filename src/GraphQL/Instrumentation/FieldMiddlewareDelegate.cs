using System.Threading.Tasks;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Encapsulates a method that has a parameter of type <see cref="IResolveFieldContext"/> and
    /// asynchronously returns an object.
    /// </summary>
    public delegate Task<object> FieldMiddlewareDelegate(IResolveFieldContext context);
}
