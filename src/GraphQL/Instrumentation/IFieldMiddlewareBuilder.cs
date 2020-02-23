using System;
using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Interface for connecting middlewares to a schema.
    /// </summary>
    public interface IFieldMiddlewareBuilder
    {
        /// <summary>
        /// Adds the specified delegate to the list of delegates that will be applied to the schema when invoking <see cref="ApplyTo(ISchema)"/>.
        /// <br/><br/>
        /// The delegate is used to unify the different ways of specifying middleware. See additional methods in <see cref="FieldMiddlewareBuilderExtensions"/>.
        /// </summary>
        /// <param name="middleware">Middleware delegate.</param>
        /// <returns>Reference to the same <see cref="IFieldMiddlewareBuilder"/>.</returns>
        IFieldMiddlewareBuilder Use(Func<ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware);

        /// <summary>
        /// Applies all delegates specified by the <see cref="Use(Func{ISchema, FieldMiddlewareDelegate, FieldMiddlewareDelegate})"/> method to the schema.
        /// <br/><br/>
        /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
        /// Therefore, as a rule, this method should be called only once during schema initialization. See <see cref="DocumentExecuter.ExecuteAsync(ExecutionOptions)"/>.
        /// </summary>
        /// <param name="schema">The schema to which you want to apply middlewares.</param>
        void ApplyTo(ISchema schema);
    }
}
