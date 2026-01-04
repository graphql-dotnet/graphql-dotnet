using System.Linq.Expressions;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Builders;

/// <summary>
/// Builds a field for a graph with a specified source type and return type.
/// </summary>
/// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
/// <typeparam name="TReturnType">The type of the return value of the resolver.</typeparam>
public class FieldBuilder<[NotAGraphType] TSourceType, [NotAGraphType] TReturnType> : IFieldMetadataWriter
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
    /// <param name="name">The name of the field.</param>
    /// <param name="type">The graph type of the field.</param>
    public static FieldBuilder<TSourceType, TReturnType> Create(string name, IGraphType type)
    {
        var fieldType = new FieldType
        {
            Name = name,
            ResolvedType = type,
        };
        return new FieldBuilder<TSourceType, TReturnType>(fieldType);
    }

    /// <inheritdoc cref="Create(string, IGraphType)"/>
    public static FieldBuilder<TSourceType, TReturnType> Create(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null)
    {
        var fieldType = new FieldType
        {
            Name = name,
            Type = type,
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
    /// Sets the input coercion for the field, replacing any existing coercion function.
    /// Runs before any validation defined via <see cref="Validate"/>.
    /// Null values are not passed to this function.
    /// Applies only to input fields.
    /// </summary>
    [AllowedOn<IInputObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ParseValue(Func<object, object> parseValue)
    {
        FieldType.Parser = parseValue;
        return this;
    }

    /// <summary>
    /// Adds validation to the field, appending it to any existing validation function.
    /// Runs after any coercion defined via <see cref="ParseValue"/>.
    /// Null values are not passed to this function.
    /// Applies only to input fields.
    /// </summary>
    [AllowedOn<IInputObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Validate(Action<object> validation)
    {
        FieldType.Validator += validation;
        return this;
    }

    /// <inheritdoc cref="ValidateArguments(Func{FieldArgumentsValidationContext, ValueTask})"/>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ValidateArguments(Action<FieldArgumentsValidationContext> validation)
    {
        FieldType.ValidateArguments = ctx => { validation(ctx); return default; };
        return this;
    }

    /// <summary>
    /// Sets argument validation to the field, replacing any existing validation function.
    /// Runs after all arguments have been coerced and validated as appropriate.
    /// Throw a <see cref="ValidationError"/> exception within the delegate if necessary to indicate
    /// a problem; they will be reported as a validation error. Other exceptions bubble
    /// up to the <see cref="DocumentExecuter"/> to be handled by the unhandled exception delegate.
    /// Applies only to output fields.
    /// </summary>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ValidateArguments(Func<FieldArgumentsValidationContext, ValueTask> validation)
    {
        FieldType.ValidateArguments = validation;
        return this;
    }

    /// <summary>
    /// Sets the resolver for the field.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Resolve(IFieldResolver? resolver)
    {
        FieldType.Resolver = resolver;
        return this;
    }

    /// <inheritdoc cref="Resolve(IFieldResolver)"/>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolve)
        => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));

    /// <inheritdoc cref="Resolve(IFieldResolver)"/>
    /// <remarks>Indicates that the <see cref="IResolveFieldContextAccessor"/> is not required for this resolver.</remarks>
    [AllowedOn<IObjectGraphType>]
    internal virtual FieldBuilder<TSourceType, TReturnType> ResolveNoAccessor(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolve)
        => Resolve(new FuncFieldResolverNoAccessor<TSourceType, TReturnType>(resolve));

    /// <inheritdoc cref="Resolve(IFieldResolver)"/>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolve)
        => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(context => new ValueTask<TReturnType?>(resolve(context))));

    /// <inheritdoc cref="Resolve(IFieldResolver)"/>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ResolveDelegate(Delegate? resolve)
    {
        IFieldResolver? resolver = null;

        if (resolve != null)
        {
            // create an instance expression that points to the instance represented by the delegate
            // for instance, if the delegate represents obj.MyMethod,
            // then the lambda would be: _ => obj
            var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var body = Expression.Constant(resolve.Target, resolve.Method.DeclaringType!);
            var lambda = Expression.Lambda(body, param);
            resolver = AutoRegisteringHelper.BuildFieldResolver(resolve.Method, null, null, lambda);
        }

        return Resolve(resolver);
    }

    /// <summary>
    /// Sets the return type of the field.
    /// </summary>
    /// <typeparam name="TNewReturnType">The type of the return value of the resolver.</typeparam>
    public virtual FieldBuilder<TSourceType, TNewReturnType> Returns<[NotAGraphType] TNewReturnType>()
        => new(FieldType);

    /// <summary>
    /// Adds an argument to the field.
    /// </summary>
    /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
    /// <param name="name">The name of the argument.</param>
    /// <param name="description">The description of the argument.</param>
    /// <param name="configure">A delegate to further configure the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType>(string name, string? description, Action<QueryArgument>? configure = null)
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
    /// <param name="name">The name of the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType>(string name)
        where TArgumentGraphType : IGraphType
        => Argument<TArgumentGraphType>(name, null);

    /// <summary>
    /// Adds an argument to the field.
    /// </summary>
    /// <typeparam name="TArgumentGraphType">The graph type of the argument.</typeparam>
    /// <param name="name">The name of the argument.</param>
    /// <param name="configure">A delegate to further configure the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TArgumentGraphType>(string name, Action<QueryArgument>? configure = null)
        where TArgumentGraphType : IGraphType
        => Argument(typeof(TArgumentGraphType), name, configure);

    /// <summary>
    /// Adds an argument to the field.
    /// </summary>
    /// <typeparam name="TArgumentClrType">The clr type of the argument.</typeparam>
    /// <param name="name">The name of the argument.</param>
    /// <param name="nullable">Indicates if the argument is optional or not. When <see langword="null"/>, nullable value types will be nullable fields.</param>
    /// <param name="configure">A delegate to further configure the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument<[NotAGraphType] TArgumentClrType>(string name, bool? nullable = null, Action<QueryArgument>? configure = null)
    {
        Type type;

        try
        {
            var isNullable = nullable ?? (Nullable.GetUnderlyingType(typeof(TArgumentClrType)) != null);
            type = typeof(TArgumentClrType).GetGraphTypeFromType(isNullable, TypeMappingMode.InputType);
        }
        catch (ArgumentOutOfRangeException exp)
        {
            throw new ArgumentException($"The GraphQL type for argument '{FieldType.Name}.{name}' could not be derived implicitly from type '{typeof(TArgumentClrType).Name}'. " + exp.Message, exp);
        }

        return Argument(type, name, configure);
    }

    /// <summary>
    /// Adds an argument to the field.
    /// </summary>
    /// <typeparam name="TArgumentClrType">The clr type of the argument.</typeparam>
    /// <param name="name">The name of the argument.</param>
    /// <param name="nullable">Indicates if the argument is optional or not. When <see langword="null"/>, nullable value types will be nullable fields.</param>
    /// <param name="description">The description of the argument.</param>
    /// <param name="configure">A delegate to further configure the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument<[NotAGraphType] TArgumentClrType>(string name, bool? nullable, string? description, Action<QueryArgument>? configure = null)
        => Argument<TArgumentClrType>(name, nullable, b =>
        {
            b.Description = description;
            configure?.Invoke(b);
        });

    /// <summary>
    /// Adds an argument to the field.
    /// </summary>
    /// <param name="type">The graph type of the argument.</param>
    /// <param name="name">The name of the argument.</param>
    /// <param name="configure">A delegate to further configure the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string name, Action<QueryArgument>? configure = null)
    {
        var arg = new QueryArgument(type)
        {
            Name = name,
        };
        configure?.Invoke(arg);
        FieldType.Arguments ??= [];
        FieldType.Arguments.Add(arg);
        return this;
    }

    /// <summary>
    /// Adds an argument to the field.
    /// </summary>
    /// <param name="type">The graph type of the argument.</param>
    /// <param name="name">The name of the argument.</param>
    /// <param name="configure">A delegate to further configure the argument.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Argument(IGraphType type, string name, Action<QueryArgument>? configure = null)
    {
        var arg = new QueryArgument(type)
        {
            Name = name,
        };
        configure?.Invoke(arg);
        FieldType.Arguments ??= [];
        FieldType.Arguments.Add(arg);
        return this;
    }

    /// <summary>
    /// Adds the specified collection of arguments to the field.
    /// </summary>
    /// <param name="arguments">Arguments to add.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Arguments(IEnumerable<QueryArgument> arguments)
    {
        if (arguments != null)
        {
            foreach (var arg in arguments)
            {
                FieldType.Arguments ??= [];
                FieldType.Arguments.Add(arg);
            }
        }
        return this;
    }

    /// <summary>
    /// Adds the specified collection of arguments to the field.
    /// </summary>
    /// <param name="arguments">Arguments to add.</param>
    [AllowedOn<IObjectGraphType, IInterfaceGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> Arguments(params QueryArgument[] arguments)
    {
        return Arguments((IEnumerable<QueryArgument>)arguments);
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
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ResolveStream(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> sourceStreamResolver)
    {
        FieldType.StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(sourceStreamResolver);
        FieldType.Resolver ??= SourceFieldResolver.Instance;
        return this;
    }

    /// <summary>
    /// Sets a source stream resolver for the field.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ResolveStreamAsync(Func<IResolveFieldContext<TSourceType>, Task<IObservable<TReturnType?>>> sourceStreamResolver)
    {
        FieldType.StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(context => new ValueTask<IObservable<TReturnType?>>(sourceStreamResolver(context)));
        FieldType.Resolver ??= SourceFieldResolver.Instance;
        return this;
    }

    // Allows metadata builder extension methods to read/write to the underlying field type without unnecessarily
    // exposing metadata methods directly on the field builder; users can always use the FieldType property
    // to access the underlying metadata directly.
    Dictionary<string, object?> IProvideMetadata.Metadata => FieldType.Metadata;
    IMetadataReader IMetadataWriter.MetadataReader => FieldType;
    TType IProvideMetadata.GetMetadata<TType>(string key, TType defaultValue) => FieldType.GetMetadata(key, defaultValue);
    TType IProvideMetadata.GetMetadata<TType>(string key, Func<TType> defaultValueFactory) => FieldType.GetMetadata(key, defaultValueFactory);
    bool IProvideMetadata.HasMetadata(string key) => FieldType.HasMetadata(key);

    /// <summary>
    /// Specifies that the field depends on <typeparamref name="TService"/> to be provided by the dependency injection provider.
    /// </summary>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> DependsOn<TService>()
        => this.DependsOn(typeof(TService));

    /// <inheritdoc cref="FieldExtensions.ApplyMiddleware(FieldType, IFieldMiddleware)"/>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ApplyMiddleware(IFieldMiddleware middleware)
    {
        FieldType.ApplyMiddleware(middleware);
        return this;
    }

    /// <inheritdoc cref="FieldExtensions.ApplyMiddleware{TMiddleware}(FieldType)"/>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ApplyMiddleware<TMiddleware>()
        where TMiddleware : IFieldMiddleware
    {
        FieldType.ApplyMiddleware<TMiddleware>();
        return this;
    }

    /// <inheritdoc cref="FieldExtensions.ApplyMiddleware(FieldType, Func{FieldMiddlewareDelegate, FieldMiddlewareDelegate})"/>/>
    [AllowedOn<IObjectGraphType>]
    public virtual FieldBuilder<TSourceType, TReturnType> ApplyMiddleware(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> middleware)
    {
        FieldType.ApplyMiddleware(middleware);
        return this;
    }
}
