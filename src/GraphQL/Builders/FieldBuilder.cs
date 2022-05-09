using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Builders
{
    /// <summary>
    /// Static methods to create field builders.
    /// </summary>
    public static class FieldBuilder
    {
        /// <summary>
        /// Returns a builder for a new field with a specified source type, return type and graph type.
        /// </summary>
        /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
        /// <typeparam name="TReturnType">The type of the return value of the resolver.</typeparam>
        /// <param name="type">The graph type of the field.</param>
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(Type? type = null)
            => FieldBuilder<TSourceType, TReturnType>.Create(type);

        /// <inheritdoc cref="Create{TSourceType, TReturnType}(Type)"/>
        public static FieldBuilder<TSourceType, TReturnType> Create<TSourceType, TReturnType>(IGraphType type)
            => FieldBuilder<TSourceType, TReturnType>.Create(type);
    }

    /// <summary>
    /// Builds a field for a graph with a specified source type and return type.
    /// </summary>
    /// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
    /// <typeparam name="TReturnType">The type of the return value of the resolver.</typeparam>
    public class FieldBuilder<TSourceType, TReturnType>
    {
        /// <summary>
        /// Returns the generated field.
        /// </summary>
        public FieldType FieldType { get; }

        /// <summary>
        /// Initializes a new instance for the specified <see cref="Types.FieldType"/>.
        /// </summary>
        protected FieldBuilder(FieldType fieldType)
        {
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns a builder for a new field.
        /// </summary>
        /// <param name="type">The graph type of the field.</param>
        /// <param name="name">The name of the field.</param>
        public static FieldBuilder<TSourceType, TReturnType> Create(IGraphType type, string name = "default")
        {
            var fieldType = new FieldType
            {
                Name = name,
                ResolvedType = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        /// <inheritdoc cref="Create(IGraphType, string)"/>
        public static FieldBuilder<TSourceType, TReturnType> Create(Type? type = null, string name = "default")
        {
            var fieldType = new FieldType
            {
                Name = name,
                Type = type,
                Arguments = new QueryArguments(),
            };
            return new FieldBuilder<TSourceType, TReturnType>(fieldType);
        }

        /// <summary>
        /// Sets the graph type of the field.
        /// </summary>
        /// <param name="type">The graph type of the field.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Type(IGraphType type)
        {
            FieldType.ResolvedType = type;
            return this;
        }

        /// <summary>
        /// Sets the name of the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Name(string name)
        {
            FieldType.Name = name;
            return this;
        }

        /// <summary>
        /// Sets the description of the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Description(string? description)
        {
            FieldType.Description = description;
            return this;
        }

        /// <summary>
        /// Sets the deprecation reason of the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> DeprecationReason(string? deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        /// <summary>
        /// Sets the default value of fields on input object graph types.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> DefaultValue(TReturnType? defaultValue = default)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        internal FieldBuilder<TSourceType, TReturnType> DefaultValue(object? defaultValue)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        /// <summary>
        /// Sets the resolver for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Resolve(IFieldResolver? resolver)
        {
            FieldType.Resolver = resolver;
            return this;
        }

        /// <inheritdoc cref="Resolve(IFieldResolver)"/>
        public virtual FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolve)
            => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));

        /// <inheritdoc cref="Resolve(IFieldResolver)"/>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolve)
            => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(context => new ValueTask<TReturnType?>(resolve(context))));

        /// <summary>
        /// Sets the return type of the field.
        /// </summary>
        /// <typeparam name="TNewReturnType">The type of the return value of the resolver.</typeparam>
        public virtual FieldBuilder<TSourceType, TNewReturnType> Returns<TNewReturnType>()
            => new FieldBuilder<TSourceType, TNewReturnType>(FieldType);

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, string? description, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <typeparam name="TArgumentType">The type of the argument value.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">The description of the argument.</param>
        /// <param name="defaultValue">The default value of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType, TArgumentType>(string name, string? description,
            TArgumentType? defaultValue = default, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        /// <summary>
        /// Adds an argument to the field.
        /// </summary>
        /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
        /// <param name="name">The name of the argument.</param>
        /// <param name="configure">A delegate to further configure the argument.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Argument<TArgumentGraphType>(string name, Action<QueryArgument>? configure = null)
            where TArgumentGraphType : IGraphType
        {
            var arg = new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
            };
            configure?.Invoke(arg);
            FieldType.Arguments ??= new();
            FieldType.Arguments.Add(arg);
            return this;
        }

        /// <summary>
        /// Runs a configuration delegate for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        /// <summary>
        /// Sets a source stream resolver for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveStream(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> sourceStreamResolver)
        {
            FieldType.StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(sourceStreamResolver);
            return this;
        }

        /// <summary>
        /// Sets a source stream resolver for the field.
        /// </summary>
        public virtual FieldBuilder<TSourceType, TReturnType> ResolveStreamAsync(Func<IResolveFieldContext<TSourceType>, Task<IObservable<TReturnType?>>> sourceStreamResolver)
        {
            FieldType.StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(context => new ValueTask<IObservable<TReturnType?>>(sourceStreamResolver(context)));
            return this;
        }

        /// <summary>
        /// Apply directive to field without specifying arguments. If the directive declaration has arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name)
        {
            FieldType.ApplyDirective(name);
            return this;
        }

        /// <summary>
        /// Apply directive to field specifying one argument. If the directive declaration has other arguments,
        /// then their default values (if any) will be used.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <param name="argumentValue">Argument value.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name, string argumentName, object? argumentValue)
        {
            FieldType.ApplyDirective(name, argumentName, argumentValue);
            return this;
        }

        /// <summary>
        /// Apply directive to field specifying configuration delegate.
        /// </summary>
        /// <param name="name">Directive name.</param>
        /// <param name="configure">Configuration delegate.</param>
        public virtual FieldBuilder<TSourceType, TReturnType> Directive(string name, Action<AppliedDirective> configure)
        {
            FieldType.ApplyDirective(name, configure);
            return this;
        }
    }
}
