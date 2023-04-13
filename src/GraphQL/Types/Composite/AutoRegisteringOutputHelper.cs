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
    /// any existing configuration within <see cref="ObjectFieldType.Arguments"/>, <see cref="ObjectFieldType.Resolver"/>
    /// and <see cref="SubscriptionRootFieldType.StreamResolver"/>.
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
        Action<ParameterInfo, QueryArgument> applyArgumentAttributesFunc)
    {
        if (fieldType == null)
            throw new ArgumentNullException(nameof(fieldType));

        if (memberInfo is PropertyInfo propertyInfo)
        {
            if (buildMemberInstanceExpressionFunc != null)
            {
                if (fieldType is not ObjectFieldType oft)
                    throw new InvalidOperationException($"The member '{propertyInfo.DeclaringType?.Name}.{propertyInfo.Name}' is a resolver, but the specified fieldType is not a ObjectFieldType.");

                oft.Resolver = new MemberResolver(propertyInfo, buildMemberInstanceExpressionFunc(memberInfo));
            }
        }
        else if (memberInfo is MethodInfo methodInfo)
        {
            List<LambdaExpression> expressions = new();
            QueryArguments queryArguments = new();
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                var getArgumentInfoMethod = getTypedArgumentInfoMethod(parameterInfo.ParameterType);
                var argumentInfo = getArgumentInfoMethod(fieldType, parameterInfo);
                var (queryArgument, expression) = argumentInfo.ConstructQueryArgument();
                if (queryArgument != null)
                {
                    applyArgumentAttributesFunc(parameterInfo, queryArgument);
                    queryArguments.Add(queryArgument);
                }
                expression ??= AutoRegisteringHelper.GetParameterExpression(
                    parameterInfo.ParameterType,
                    queryArgument ?? throw new InvalidOperationException("Invalid response from ConstructQueryArgument: queryArgument and expression cannot both be null"));
                expressions.Add(expression);
            }
            if (buildMemberInstanceExpressionFunc != null)
            {
                var memberInstanceExpression = buildMemberInstanceExpressionFunc(methodInfo);
                if (IsObservableOrAsyncEnumerable(methodInfo.ReturnType))
                {
                    if (fieldType is not SubscriptionRootFieldType srft)
                        throw new InvalidOperationException($"The member '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' is a stream resolver, but the specified fieldType is not a SubscriptionRootFieldType.");
                    var resolver = new SourceStreamMethodResolver(methodInfo, memberInstanceExpression, expressions);
                    srft.Resolver = resolver;
                    srft.StreamResolver = resolver;
                }
                else
                {
                    if (fieldType is not ObjectFieldType oft)
                        throw new InvalidOperationException($"The member '{methodInfo.DeclaringType?.Name}.{methodInfo.Name}' is a resolver, but the specified fieldType is not a ObjectFieldType.");

                    oft.Resolver = new MemberResolver(methodInfo, memberInstanceExpression, expressions);
                }
            }

            if (fieldType is IFieldTypeWithArguments ftwa)
                ftwa.Arguments = queryArguments;
        }
        else if (memberInfo is FieldInfo fieldInfo)
        {
            if (buildMemberInstanceExpressionFunc != null)
            {
                if (fieldType is not ObjectFieldType oft)
                    throw new InvalidOperationException($"The member '{fieldInfo.DeclaringType?.Name}.{fieldInfo.Name}' is a resolver, but the specified fieldType is not a ObjectFieldType.");

                oft.Resolver = new MemberResolver(fieldInfo, buildMemberInstanceExpressionFunc(memberInfo));
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
    internal static bool IsObservableOrAsyncEnumerable(Type type)
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
                types.SelectMany(type => type.GetProperties(flags)).Where(x => x.CanRead),
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
                typeof(TSourceType).GetProperties(flags).Where(x => x.CanRead),
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
        }
    }
}
