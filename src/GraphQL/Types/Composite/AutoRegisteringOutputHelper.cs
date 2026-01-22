using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Resolvers;

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
    /// <see cref="AutoRegisteringObjectGraphType{TSourceType}.GetArgumentInformation{TParameterType}(FieldType, ParameterInfo)">GetArgumentInformation</see>, building
    /// a list of query arguments and expressions as necessary. Then a field resolver is built around the method.
    /// </summary>
    public static void BuildFieldType(
        MemberInfo memberInfo,
        FieldType fieldType,
        Func<MemberInfo, LambdaExpression>? buildMemberInstanceExpressionFunc,
        Func<Type, Func<FieldType, ParameterInfo, ArgumentInformation>> getTypedArgumentInfoMethod,
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
                var getArgumentInfoMethod = getTypedArgumentInfoMethod(parameterInfo.ParameterType);
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
    /// </summary>
    public static IEnumerable<MemberInfo> GetRegisteredMembers<TSourceType>(Expression<Func<TSourceType, object?>>[]? excludedProperties)
    {
        if (typeof(TSourceType).IsInterface)
        {
            var types = typeof(TSourceType).GetInterfaces().Append(typeof(TSourceType));

            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var properties = AutoRegisteringHelper.ExcludeProperties(
                types.SelectMany(type => type.GetProperties(flags)).Where(x => x.GetMethod?.IsPublic ?? false),
                excludedProperties);
            var methods = types.SelectMany(type => type.GetMethods(flags))
                .Where(x =>
                    !x.ContainsGenericParameters &&     // exclude methods with open generics
                    !x.IsSpecialName &&                 // exclude methods generated for properties
                    x.ReturnType != typeof(void) &&     // exclude methods which do not return a value
                    x.ReturnType != typeof(Task));
            return properties.Concat<MemberInfo>(methods);
        }
        else
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var properties = AutoRegisteringHelper.ExcludeProperties(
                typeof(TSourceType).GetProperties(flags).Where(x => x.GetMethod?.IsPublic ?? false),
                excludedProperties);
            var methods = typeof(TSourceType).GetMethods(flags)
                .Where(x =>
                    !x.ContainsGenericParameters &&                          // exclude methods with open generics
                    !x.IsSpecialName &&                                      // exclude methods generated for properties
                    x.ReturnType != typeof(void) &&                          // exclude methods which do not return a value
                    x.ReturnType != typeof(Task) &&                          // exclude methods which do not return a value
                    x.GetBaseDefinition().DeclaringType != typeof(object) && // exclude methods inherited from object (e.g. GetHashCode)
                    !IsRecordEqualsMethod<TSourceType>(x) &&                 // exclude methods generated for record types: public virtual/override bool Equals(RECORD_TYPE)
                    x.Name != "<Clone>$");                                   // exclude methods generated for record types: public [new] virtual RECORD_TYPE <Clone>$()
            return properties.Concat<MemberInfo>(methods);
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
    private static IEnumerable<MemberInfo> GetRequiredMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
    {
        // Get all properties with required modifier (including from base classes)
        IEnumerable<MemberInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null);

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null);

        return properties.Concat(fields);
    }

    /// <summary>
    /// Analyzes a method parameter and returns an instance of <see cref="ArgumentInformation"/>
    /// containing information necessary to build a <see cref="QueryArgument"/> and <see cref="IFieldResolver"/>.
    /// Also applies any <see cref="GraphQLAttribute"/> attributes defined on the <see cref="ParameterInfo"/>
    /// to the returned <see cref="ArgumentInformation"/> instance.
    /// </summary>
    public static ArgumentInformation GetArgumentInformation<TSourceType>(TypeInformation typeInformation, FieldType fieldType, ParameterInfo parameterInfo)
    {
        var argumentInfo = new ArgumentInformation(parameterInfo, typeof(TSourceType), fieldType, typeInformation);
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

    private static Expression<Func<IResolveFieldContext, TSourceType>> BuildGetContextSourceExpression<TSourceType>()
        => context => (TSourceType)(context.Source ?? ThrowSourceNullException());

    private static object ThrowSourceNullException()
    {
        throw new InvalidOperationException("IResolveFieldContext.Source is null; please use static methods when using an AutoRegisteringObjectGraphType as a root graph type or provide a root value.");
    }

    [RequiresUnreferencedCode("Types using InstanceSource.GetServiceOrCreateInstance require public constructors for dependency injection.")]
    [RequiresDynamicCode("Types using InstanceSource.GetServiceOrCreateInstance require dynamic instance creation.")]
    private static Expression<Func<IResolveFieldContext, TSourceType>> BuildGetServiceOrCreateInstanceExpression<TSourceType>(bool tryGetServiceFirst)
    {
        var contextParam = Expression.Parameter(typeof(IResolveFieldContext), "context");
        var serviceProviderVar = Expression.Variable(typeof(IServiceProvider), "serviceProvider");

        // 1. Check RequestServices and store in variable, throwing if null
        var requestServicesProperty = Expression.Property(contextParam, nameof(IResolveFieldContext.RequestServices));

        var throwMissingServices = Expression.Throw(
            Expression.New(typeof(MissingRequestServicesException)),
            typeof(IServiceProvider));

        var assignServiceProvider = Expression.Assign(
            serviceProviderVar,
            Expression.Coalesce(requestServicesProperty, throwMissingServices));

        var getServiceMethod = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;
        bool needsServiceProviderForDI = false;

        // Local method to create dependency injection expression for a type
        Expression CreateDependencyExpression(Type type)
        {
            // Special handling for IServiceProvider and IResolveFieldContext
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
                // Mark that we need service provider for actual dependency resolution
                needsServiceProviderForDI = true;
                var getServiceCall = Expression.Call(serviceProviderVar, getServiceMethod, Expression.Constant(type));
                return Expression.Convert(getServiceCall, type);
            }
        }

        // 2. Look for a single public constructor; if multiple or none assume null
        var constructors = typeof(TSourceType).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = constructors.Length == 1 ? constructors[0] : null;

        // 3. Generate fallback expression
        Expression fallbackExpression;
        if (constructor == null && !typeof(TSourceType).IsValueType)
        {
            // If no single constructor found, throw
            fallbackExpression = Expression.Throw(
                Expression.New(
                    typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                    Expression.Constant($"Unable to create instance of type {typeof(TSourceType).Name}; no single public constructor found and type is not registered in service provider.")),
                typeof(TSourceType));
        }
        else
        {
            NewExpression newExpression;
            if (constructor == null)
            {
                newExpression = Expression.New(typeof(TSourceType));
            }
            else
            {
                // 3a. For each constructor parameter, handle special types or call GetService
                var parameters = constructor.GetParameters();

                if (parameters.Length == 0)
                {
                    // Parameterless constructor
                    newExpression = Expression.New(constructor);
                }
                else
                {
                    var parameterExpressions = new Expression[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameterExpressions[i] = CreateDependencyExpression(parameters[i].ParameterType);
                    }
                    newExpression = Expression.New(constructor, parameterExpressions);
                }
            }

            // 3b. Handle required properties and fields
            var requiredMembers = GetRequiredMembers(typeof(TSourceType)).ToList();
            if (requiredMembers.Count > 0)
            {
                var bindings = new List<MemberBinding>();
                foreach (var member in requiredMembers)
                {
                    var memberType = member switch
                    {
                        PropertyInfo pi => pi.PropertyType,
                        FieldInfo fi => fi.FieldType,
                        _ => throw new InvalidOperationException($"Unexpected member type: {member.GetType().Name}")
                    };

                    var valueExpression = CreateDependencyExpression(memberType);
                    bindings.Add(Expression.Bind(member, valueExpression));
                }

                fallbackExpression = Expression.MemberInit(newExpression, bindings);
            }
            else
            {
                fallbackExpression = newExpression;
            }
        }

        // 4. Build main expression using the stored service provider
        var getServiceMethod2 = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;
        var getServiceForType = Expression.Call(serviceProviderVar, getServiceMethod2, Expression.Constant(typeof(TSourceType)));

        // 4b. Coalesce with fallback expression (handle value types differently)
        Expression coalesceExpression;
        if (typeof(TSourceType).IsValueType)
        {
            // For value types, check if GetService returns null, then convert or use fallback
            var nullCheck = Expression.Equal(getServiceForType, Expression.Constant(null, typeof(object)));
            var convertToTSourceType = Expression.Convert(getServiceForType, typeof(TSourceType));
            coalesceExpression = Expression.Condition(nullCheck, fallbackExpression, convertToTSourceType);
        }
        else
        {
            // For reference types, use TypeAs and Coalesce
            var convertToTSourceType = Expression.TypeAs(getServiceForType, typeof(TSourceType));
            coalesceExpression = Expression.Coalesce(convertToTSourceType, fallbackExpression);
        }

        // 5. Create block expression with variable and statements
        // For GetServiceOrCreateInstance (tryGetServiceFirst=true), always validate RequestServices
        // For NewInstance (tryGetServiceFirst=false), only validate when actually needed for DI
        var assignServiceProviderWithoutCheck = Expression.Assign(serviceProviderVar, requestServicesProperty);
        var blockExpression = Expression.Block(
            new[] { serviceProviderVar },
            (tryGetServiceFirst || needsServiceProviderForDI) ? assignServiceProvider : assignServiceProviderWithoutCheck,
            tryGetServiceFirst ? coalesceExpression : fallbackExpression);

        return Expression.Lambda<Func<IResolveFieldContext, TSourceType>>(blockExpression, contextParam);
    }

    private static readonly PropertyInfo _requestServicesProperty = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.RequestServices))!;
    private static Expression<Func<IResolveFieldContext, TSourceType>> BuildGetRequiredServiceExpression<TSourceType>()
    {
        // Build: context => (TSourceType)context.RequestServices.GetService(typeof(TSourceType)) ?? throw new InvalidOperationException(...)
        var contextParam = Expression.Parameter(typeof(IResolveFieldContext), "context");
        var requestServicesProperty = Expression.Property(contextParam, _requestServicesProperty);

        var throwMissingServices = Expression.Throw(
            Expression.New(typeof(MissingRequestServicesException)),
            typeof(IServiceProvider));

        var serviceProviderExpression = Expression.Coalesce(requestServicesProperty, throwMissingServices);

        var getServiceMethod = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;
        var getServiceCall = Expression.Call(serviceProviderExpression, getServiceMethod, Expression.Constant(typeof(TSourceType)));

        var exceptionMessage = $"Required service of type {typeof(TSourceType).Name} is not registered in the service provider.";
        var throwExpression = Expression.Throw(
            Expression.New(typeof(InvalidOperationException).GetConstructor([typeof(string)])!, Expression.Constant(exceptionMessage)),
            typeof(object));

        var coalesceExpression = Expression.Coalesce(getServiceCall, throwExpression);
        var convertExpression = Expression.Convert(coalesceExpression, typeof(TSourceType));

        return Expression.Lambda<Func<IResolveFieldContext, TSourceType>>(convertExpression, contextParam);
    }
}
