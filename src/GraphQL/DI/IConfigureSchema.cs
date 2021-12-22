using System;
using GraphQL.Types;

namespace GraphQL.DI
{
    /// <summary>
    /// Allows configuration of a schema prior to its constructor.
    /// </summary>
    public interface IConfigureSchema
    {
        /// <summary>
        /// Configures a schema prior to the code in its constructor.
        /// Specifically, executes during the <see cref="Schema"/> constructor, which
        /// executes prior to any descendant classes' constructors.
        /// </summary>
        void Configure(ISchema schema, IServiceProvider serviceProvider);
    }

    internal class ConfigureSchema : IConfigureSchema
    {
        private readonly Action<ISchema, IServiceProvider> _action;

        public ConfigureSchema(Action<ISchema, IServiceProvider> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Configure(ISchema schema, IServiceProvider serviceProvider)
        {
            _action(schema, serviceProvider);
        }
    }
}
