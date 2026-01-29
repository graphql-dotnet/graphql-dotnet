using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Resolvers;
using GraphQL.Utilities;

namespace GraphQL.Types;

/// <summary>
/// Helper methods for <see cref="AutoRegisteringObjectGraphType{TSourceType}"/> and <see cref="AutoRegisteringInterfaceGraphType{TSourceType}"/>.
/// </summary>
internal static class AutoRegisteringOutputHelper
{
    /// <summary>
    /// Configures query arguments and a field resolver for the specified <see cref="FieldType"/>, overwriting
    /// any existing configuration within <see cref="FieldType.Arguments"/>, <see cref="FieldType.Resolver"/>
    /// and <see cref="FieldType.StreamResolver"/>.
    /// <br/><br/>
    /// For fields and properties, no query arguments are added and the field resolver simply pulls the appropriate
    /// member from <see cref="IResolveFieldContext.Source"/>.
    /// <br/><br/>
    /// For methods, method arguments are iterated and processed by
    /// <see cref="AutoRegisteringObjectGraphType{TSourceType}.GetArgumentInformation(FieldType, ParameterInfo)">GetArgumentInformation</see>, building
    /// a list of query arguments and expressions as necessary. Then a field resolver is built around the method.
    /// </summary>
    [RequiresDynamicCode("This code calls a generic method and compiles a lambda at runtime.")]
    [RequiresUnreferencedCode("This code calls a generic method and compiles a lambda at runtime.")]
    public static void BuildFieldType(
        MemberInfo memberInfo,
        FieldType fieldType,
        Func<MemberInfo, LambdaExpression>? buildMemberInstanceExpressionFunc,
        Func<FieldType, ParameterInfo, ArgumentInformation> getArgumentInfoMethod,
        Action<ParameterInfo, QueryArgument> applyArgumentAttributesFunc,
        Func<Type, Func<ArgumentInformation, LambdaExpression?>> getTypedParameterResolverMethod)
    {
        // Note: If buildMemberInstanceExpressionFunc is null, then it is assumed this is for
        // an interface graph type and the field resolver will be set to always throw an exception.

        if (fieldType == null)
            throw new ArgumentNullException(nameof(fieldType));

        if (memberInfo is PropertyInfo propertyInfo)
        {
            fieldType.Arguments = null;
            if (buildMemberInstanceExpressionFunc != null || (propertyInfo.GetMethod?.IsStatic ?? false))
            {
                var resolver = new MemberResolver(propertyInfo, buildMemberInstanceExpressionFunc?.Invoke(memberInfo));
                fieldType.Resolver = resolver;
                fieldType.StreamResolver = null;
            }
        }
        else if (memberInfo is MethodInfo methodInfo)
        {
            List<LambdaExpression> expressions = [];
            QueryArguments queryArguments = [];
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                LambdaExpression? expression = null;

                // Create ArgumentInformation for the parameter
                var argumentInfo = getArgumentInfoMethod(fieldType, parameterInfo);

                // Try to get a resolver from overridden method
                var getParameterResolverMethod = getTypedParameterResolverMethod(parameterInfo.ParameterType);
                if (getParameterResolverMethod != null)
                {
                    expression = getParameterResolverMethod(argumentInfo);
                }

                // If we have an expression, use it
                if (expression != null)
                {
                    expressions.Add(expression);
                    continue;
                }

                // Create query argument
                var queryArgument = argumentInfo.ConstructQueryArgument();
                applyArgumentAttributesFunc(parameterInfo, queryArgument);
                queryArguments.Add(queryArgument);

                expression = AutoRegisteringHelper.GetParameterExpression(parameterInfo.ParameterType, queryArgument);
                expressions.Add(expression);
            }
            if (buildMemberInstanceExpressionFunc != null || methodInfo.IsStatic)
            {
                var memberInstanceExpression = buildMemberInstanceExpressionFunc?.Invoke(methodInfo);
                if (IsObservableOrAsyncEnumerable(methodInfo.ReturnType) && memberInstanceExpression != null) // static support for interfaces is only for federation resolvers, not needed for subscriptions
                {
                    var resolver = new SourceStreamMethodResolver(methodInfo, memberInstanceExpression, expressions);
                    fieldType.Resolver = resolver;
                    fieldType.StreamResolver = resolver;
                }
                else
                {
                    var resolver = new MemberResolver(methodInfo, memberInstanceExpression, expressions);
                    fieldType.Resolver = resolver;
                    fieldType.StreamResolver = null;
                }
            }
            fieldType.Arguments = queryArguments;
        }
        else if (memberInfo is FieldInfo fieldInfo)
        {
            fieldType.Arguments = null;
            if (buildMemberInstanceExpressionFunc != null || fieldInfo.IsStatic)
            {
                var resolver = new MemberResolver(fieldInfo, buildMemberInstanceExpressionFunc?.Invoke(memberInfo));
                fieldType.Resolver = resolver;
                fieldType.StreamResolver = null;
            }
        }
        else if (memberInfo == null)
        {
            throw new ArgumentNullException(nameof(memberInfo));
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(memberInfo), "Member must be a field, property or method.");
        }
    }

    /// <summary>
    /// Determines if the type is an <see cref="IObservable{T}"/> or task that returns an <see cref="IObservable{T}"/>.
    /// Also checks for <see cref="IAsyncEnumerable{T}"/> and task that returns an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    private static bool IsObservableOrAsyncEnumerable(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var g = type.GetGenericTypeDefinition();
        if (g == typeof(IObservable<>) || g == typeof(IAsyncEnumerable<>))
            return true;
        if (g == typeof(Task<>) || g == typeof(ValueTask<>))
            return IsObservableOrAsyncEnumerable(type.GetGenericArguments()[0]);
        return false;
    }

    /// <summary>
    /// Returns a list of properties, methods or fields that should have fields created for them.
    /// <br/><br/>
    /// Unless overridden, returns a list of public instance readable properties and public instance methods
    /// that do not return <see langword="void"/> or <see cref="Task"/>
    /// including properties and methods declared on inherited classes.
    /// <br/><br/>
    /// The <see cref="MemberScanAttribute"/> can be applied to <typeparamref name="TSourceType"/> to control
    /// which member types are scanned. By default, only properties and methods are scanned (not fields).
    /// </summary>
    [RequiresUnreferencedCode("This method scans the specified type for public properties and methods including on base types.")]
    public static IEnumerable<MemberInfo> GetRegisteredMembers<TSourceType>(Expression<Func<TSourceType, object?>>[]? excludedProperties)
    {
        // Check for MemberScanAttribute to determine which member types to scan
        // Default for output types is Properties and Methods (not fields)
        var memberScanAttr = typeof(TSourceType).GetCustomAttribute<MemberScanAttribute>(inherit: true);
        var memberTypes = memberScanAttr?.MemberTypes ?? (ScanMemberTypes.Properties | ScanMemberTypes.Methods);

        var scanProperties = memberTypes.HasFlag(ScanMemberTypes.Properties);
        var scanFields = memberTypes.HasFlag(ScanMemberTypes.Fields);
        var scanMethods = memberTypes.HasFlag(ScanMemberTypes.Methods);

        if (typeof(TSourceType).IsInterface)
        {
            var types = typeof(TSourceType).GetInterfaces().Append(typeof(TSourceType));

            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            var properties = scanProperties
                ? AutoRegisteringHelper.ExcludeProperties(
                    types.SelectMany(type => type.GetProperties(flags)).Where(x => x.GetMethod?.IsPublic ?? false),
                    excludedProperties)
                : Enumerable.Empty<PropertyInfo>();

            var fields = scanFields
                ? types.SelectMany(type => type.GetFields(flags))
                : Enumerable.Empty<FieldInfo>();

            var methods = scanMethods
                ? types.SelectMany(type => type.GetMethods(flags))
                    .Where(x =>
                        !x.ContainsGenericParameters &&     // exclude methods with open generics
                        !x.IsSpecialName &&                 // exclude methods generated for properties
                        x.ReturnType != typeof(void) &&     // exclude methods which do not return a value
                        x.ReturnType != typeof(Task))
                : Enumerable.Empty<MethodInfo>();

            return properties.Concat<MemberInfo>(fields).Concat(methods);
        }
        else
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            var properties = scanProperties
                ? AutoRegisteringHelper.ExcludeProperties(
                    typeof(TSourceType).GetProperties(flags).Where(x => x.GetMethod?.IsPublic ?? false),
                    excludedProperties)
                : Enumerable.Empty<PropertyInfo>();

            var fields = scanFields
                ? typeof(TSourceType).GetFields(flags)
                : Enumerable.Empty<FieldInfo>();

            var methods = scanMethods
                ? typeof(TSourceType).GetMethods(flags)
                    .Where(x =>
                        !x.ContainsGenericParameters &&                          // exclude methods with open generics
                        !x.IsSpecialName &&                                      // exclude methods generated for properties
                        x.ReturnType != typeof(void) &&                          // exclude methods which do not return a value
                        x.ReturnType != typeof(Task) &&                          // exclude methods which do not return a value
                        x.GetBaseDefinition().DeclaringType != typeof(object) && // exclude methods inherited from object (e.g. GetHashCode)
                        !IsRecordEqualsMethod<TSourceType>(x) &&                 // exclude methods generated for record types: public virtual/override bool Equals(RECORD_TYPE)
                        x.Name != "<Clone>$")                                    // exclude methods generated for record types: public [new] virtual RECORD_TYPE <Clone>$()
                : Enumerable.Empty<MethodInfo>();

            return properties.Concat<MemberInfo>(fields).Concat(methods);
        }
    }

    private static bool IsRecordEqualsMethod<TSourceType>(MethodInfo method) =>
        method.Name == "Equals"
        && !method.IsStatic
        && method.GetParameters().Length == 1
        && IsTypeSourceOrAncestor(typeof(TSourceType), method.GetParameters()[0].ParameterType)
        && method.ReturnType == typeof(bool);

    private static bool IsTypeSourceOrAncestor(Type sourceType, Type type) =>
        sourceType == type || sourceType.BaseType != typeof(object) && sourceType.BaseType is not null && IsTypeSourceOrAncestor(sourceType.BaseType, type);

    /// <summary>
    /// Gets all required properties and fields for a type, including those from base classes.
    /// </summary>
    private static IEnumerable<MemberInfo> GetRequiredMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
    {
        // Get all properties and fields with required modifier (including from base classes)

        // Note that the compiler enforces that required properties/fields are settable with
        // visiblity same as the containing class. So no need to check that properties are
        // writable here, and no need to check for visibility; but we must include non-public
        // properties in case the containing class is 'internal'.

        IEnumerable<MemberInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null);

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null);

        return properties.Concat(fields);
    }

    /// <summary>
    /// Analyzes a method parameter and returns an instance of <see cref="ArgumentInformation"/>
    /// containing information necessary to build a <see cref="QueryArgument"/> and <see cref="IFieldResolver"/>.
    /// Also applies any <see cref="GraphQLAttribute"/> attributes defined on the <see cref="ParameterInfo"/>
    /// to the returned <see cref="ArgumentInformation"/> instance.
    /// </summary>
    public static ArgumentInformation GetArgumentInformation(Type sourceType, TypeInformation typeInformation, FieldType fieldType, ParameterInfo parameterInfo)
    {
        var argumentInfo = new ArgumentInformation(parameterInfo, sourceType, fieldType, typeInformation);
        argumentInfo.ApplyAttributes();
        return argumentInfo;
    }

    /// <summary>
    /// Applies <see cref="GraphQLAttribute"/> attributes defined on the supplied <see cref="ParameterInfo"/>
    /// to the specified <see cref="QueryArgument"/>.
    /// Also scans the parameter's owning module and assembly for globally-applied attributes.
    /// </summary>
    public static void ApplyArgumentAttributes(ParameterInfo parameterInfo, QueryArgument queryArgument)
    {
        // Apply derivatives of GraphQLAttribute
        var attributes = parameterInfo.GetGraphQLAttributes();
        foreach (var attr in attributes)
        {
            attr.Modify(queryArgument);
            attr.Modify(queryArgument, parameterInfo);
        }
    }

    /// <summary>
    /// Builds a lambda expression that determines how to obtain an instance of <typeparamref name="TSourceType"/>
    /// based on the <see cref="InstanceSourceAttribute"/> applied to the type.
    /// </summary>
    [RequiresUnreferencedCode("Types using InstanceSource.NewInstance or InstanceSource.GetServiceOrCreateInstance require public constructors for dependency injection.")]
    [RequiresDynamicCode("Types using InstanceSource.NewInstance or InstanceSource.GetServiceOrCreateInstance require dynamic instance creation.")]
    public static Expression<Func<IResolveFieldContext, TSourceType>> BuildSourceExpression<TSourceType>()
    {
        var instanceSourceAttr = typeof(TSourceType).GetCustomAttribute<InstanceSourceAttribute>();
        var instanceSource = instanceSourceAttr?.InstanceSource ?? InstanceSource.ContextSource;
        return BuildSourceExpression<TSourceType>(instanceSource);
    }

    /// <summary>
    /// Builds a lambda expression that determines how to obtain an instance of <typeparamref name="TSourceType"/>
    /// based on the specified <paramref name="instanceSource"/>.
    /// </summary>
    [RequiresUnreferencedCode("Types using InstanceSource.NewInstance or InstanceSource.GetServiceOrCreateInstance require public constructors for dependency injection.")]
    [RequiresDynamicCode("Types using InstanceSource.NewInstance or InstanceSource.GetServiceOrCreateInstance require dynamic instance creation.")]
    public static Expression<Func<IResolveFieldContext, TSourceType>> BuildSourceExpression<TSourceType>(InstanceSource instanceSource)
    {
        return instanceSource switch
        {
            InstanceSource.ContextSource => BuildGetContextSourceExpression<TSourceType>(),
            InstanceSource.GetServiceOrCreateInstance => BuildGetServiceOrCreateInstanceExpression<TSourceType>(true),
            InstanceSource.GetRequiredService => BuildGetRequiredServiceExpression<TSourceType>(),
            InstanceSource.NewInstance => BuildGetServiceOrCreateInstanceExpression<TSourceType>(false),
            _ => throw new InvalidOperationException($"Unknown instance source: {instanceSource}")
        };
    }

    // builds: context => (TSourceType)(context.Source ?? throw new InvalidOperationException("IResolveFieldContext.Source is null..."));
    private static Expression<Func<IResolveFieldContext, TSourceType>> BuildGetContextSourceExpression<TSourceType>()
        => context => (TSourceType)(context.Source ?? ThrowSourceNullException());
    internal static object ThrowSourceNullException()
        => throw new InvalidOperationException("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");

    private static Expression<Func<IResolveFieldContext, TSourceType>> BuildGetRequiredServiceExpression<TSourceType>()
        => context => context.RequestServicesOrThrow().GetRequiredService<TSourceType>();

    /* BuildGetServiceOrCreateInstanceExpression builds something like this:
     * 
     * context => {
     *   var serviceProvider = context.RequestServices ?? throw new MissingRequestServicesException();
     *   if (tryGetServiceFirst) {
     *     var service = serviceProvider.GetService(typeof(TSourceType));
     *     if (service != null)
     *       return service;
     *   }
     *   return new TSourceType(
     *     serviceProvider.GetService(typeof(Dependency1Type)) as Dependency1Type,
     *     serviceProvider.GetService(typeof(Dependency2Type)) as Dependency2Type,
     *     context, // if IResolveFieldContext
     *   ) {
     *     // set required properties/fields
     *     Dependency3Property = serviceProvider.GetService(typeof(Dependency3Type)) as Dependency3Type,
     *     ContextField = context, // if IResolveFieldContext
     *   };
     * }
     * 
     * - IServiceProvider and IResolveFieldContext constructor parameters or required properties/fields are passed directly without DI lookup.
     * - There is no handling for CancellationToken, as that is not typical for constructor injection; it is available from the context if needed.
     * - There is no handling for IResolveFieldContext<T>.
     * - There is no support for optional parameters; all constructor parameters must be resolvable.
     * - There is no support for multiple constructors; the type must have exactly one public constructor.
     * - Struct types are supported, in which case there is no heap allocation for the temporary instance of TSourceType.
     * 
     * The final expression can be compiled at runtime for an optimized field resolver.
     * Equivalent code can be constructed via source generation for AOT use.
     */
    [RequiresUnreferencedCode("Types using InstanceSource.GetServiceOrCreateInstance require public constructors for dependency injection.")]
    [RequiresDynamicCode("Types using InstanceSource.GetServiceOrCreateInstance require dynamic instance creation.")]
    private static Expression<Func<IResolveFieldContext, TSourceType>> BuildGetServiceOrCreateInstanceExpression<TSourceType>(bool tryGetServiceFirst)
    {
        var contextParam = Expression.Parameter(typeof(IResolveFieldContext), "context");
        var serviceProviderVar = Expression.Variable(typeof(IServiceProvider), "serviceProvider");

        var getServiceMethod = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;

        // Tracks whether DI is actually needed; used to optimize RequestServices validation below
        bool needsServiceProviderForDI = false;

        // Find the constructor (requires exactly one public constructor, or value type with default ctor)
        var constructors = typeof(TSourceType).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = constructors.Length == 1 ? constructors[0] : null;

        // Build fallback: create new instance with constructor + required member injection
        var fallbackExpression = BuildNewInstanceExpression(constructor);

        // Build service lookup expression: serviceProvider.GetService(typeof(TSourceType))
        var getServiceMethod2 = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;
        var getServiceForType = Expression.Call(serviceProviderVar, getServiceMethod2, Expression.Constant(typeof(TSourceType)));

        // Coalesce lookup with fallback (value types need conditional since TypeAs doesn't work)
        Expression coalesceExpression;
        if (typeof(TSourceType).IsValueType)
        {
            var nullCheck = Expression.Equal(getServiceForType, Expression.Constant(null, typeof(object)));
            var convertToTSourceType = Expression.Convert(getServiceForType, typeof(TSourceType));
            coalesceExpression = Expression.Condition(nullCheck, fallbackExpression, convertToTSourceType);
        }
        else
        {
            var convertToTSourceType = Expression.TypeAs(getServiceForType, typeof(TSourceType));
            coalesceExpression = Expression.Coalesce(convertToTSourceType, fallbackExpression);
        }

        // Final assembly: assign serviceProvider (with validation only if needed), then return result
        // - tryGetServiceFirst=true: use coalesceExpression (lookup, fallback if null)
        // - tryGetServiceFirst=false: use fallbackExpression directly (always create new)
        var blockExpression = Expression.Block(
            new[] { serviceProviderVar },
            BuildRequestServicesAssignment(tryGetServiceFirst || needsServiceProviderForDI),
            tryGetServiceFirst ? coalesceExpression : fallbackExpression);

        return Expression.Lambda<Func<IResolveFieldContext, TSourceType>>(blockExpression, contextParam);

        // Builds new instance expression with constructor and required member injection
        Expression BuildNewInstanceExpression(ConstructorInfo? constructor)
        {
            // No public constructor and not a value type - will throw at runtime if service not found
            if (constructor == null && !typeof(TSourceType).IsValueType)
            {
                return Expression.Throw(
                    Expression.New(
                        typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                        Expression.Constant($"Unable to create instance of type {typeof(TSourceType).Name}; no single public constructor found and type is not registered in service provider.")),
                    typeof(TSourceType));
            }

            // Build constructor call expression
            NewExpression newExpression;
            if (constructor == null)
            {
                // Value type with default constructor
                newExpression = Expression.New(typeof(TSourceType));
            }
            else
            {
                // Inject constructor parameters from DI
                var parameters = constructor.GetParameters();

                if (parameters.Length == 0)
                {
                    newExpression = Expression.New(constructor);
                }
                else
                {
                    var parameterExpressions = new Expression[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        // Create expression to resolve parameter from DI, or pass through IResolveFieldContext / IServiceProvider
                        parameterExpressions[i] = CreateDependencyExpression(parameters[i].ParameterType);
                    }
                    newExpression = Expression.New(constructor, parameterExpressions);
                }
            }

            // Inject C# 11 'required' members after construction
            var requiredMembers = GetRequiredMembers(typeof(TSourceType)).ToList();
            if (requiredMembers.Count > 0)
            {
                var bindings = new List<MemberBinding>(requiredMembers.Count);
                foreach (var member in requiredMembers)
                {
                    var memberType = member switch
                    {
                        PropertyInfo pi => pi.PropertyType,
                        FieldInfo fi => fi.FieldType,
                        _ => throw new InvalidOperationException($"Unexpected member type: {member.GetType().Name}")
                    };

                    // Create expression to resolve required member from DI, or pass through IResolveFieldContext / IServiceProvider
                    var valueExpression = CreateDependencyExpression(memberType);
                    bindings.Add(Expression.Bind(member, valueExpression));
                }

                return Expression.MemberInit(newExpression, bindings);
            }

            return newExpression;
        }

        // Build expression that assigns RequestServices to a variable, throwing if null
        Expression BuildRequestServicesAssignment(bool throwIfMissing)
        {
            var requestServicesProperty = Expression.Property(contextParam, nameof(IResolveFieldContext.RequestServices));

            if (!throwIfMissing)
            {
                return Expression.Assign(serviceProviderVar, requestServicesProperty);
            }

            var throwMissingServices = Expression.Throw(
                Expression.New(typeof(MissingRequestServicesException)),
                typeof(IServiceProvider));

            return Expression.Assign(
                serviceProviderVar,
                Expression.Coalesce(requestServicesProperty, throwMissingServices));
        }

        // Builds an expression to resolve a dependency from DI or pass through special types
        Expression CreateDependencyExpression(Type type)
        {
            // IServiceProvider and IResolveFieldContext are passed directly without DI lookup
            if (type == typeof(IServiceProvider))
            {
                return serviceProviderVar;
            }
            else if (type == typeof(IResolveFieldContext))
            {
                return contextParam;
            }
            else
            {
                // Track that we need service provider validation since we're doing actual DI
                needsServiceProviderForDI = true;
                var getServiceCall = Expression.Call(serviceProviderVar, getServiceMethod, Expression.Constant(type));
                return Expression.Convert(getServiceCall, type);
            }
        }
    }
}
