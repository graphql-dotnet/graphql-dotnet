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
    [RequiresDynamicCode("This code calls a generic method and compiles a lambda at runtime.")]
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
}
