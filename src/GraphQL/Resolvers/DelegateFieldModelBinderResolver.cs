using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using GraphQL.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// An implementation of <see cref="IFieldResolver"/> that executes a specified delegate by late-binding
    /// method arguments such as <see cref="IResolveFieldContext"/>, the source, the user context, or field arguments.
    /// </summary>
    public class DelegateFieldModelBinderResolver : IFieldResolver
    {
        private readonly Delegate _resolver;
        private readonly ParameterInfo[] _parameters;

        /// <summary>
        /// Initializes a new instance with the specified delegate.
        /// </summary>
        public DelegateFieldModelBinderResolver(Delegate resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
            _parameters = _resolver.GetMethodInfo().GetParameters();
        }

        /// <inheritdoc/>
        public object? Resolve(IResolveFieldContext context)
        {
            var arguments = ReflectionHelper.BuildArguments(_parameters, context);
            try
            {
                return _resolver.DynamicInvoke(arguments);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                return null; // never executed, necessary only for intellisense
            }
        }
    }
}
